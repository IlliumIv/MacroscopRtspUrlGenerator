using MacroscopRtspUrlGenerator.ConfigurationEntities;
using MacroscopRtspUrlGenerator.Enums;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;

namespace MacroscopRtspUrlGenerator
{
    // rtsp://polyakov2.ent.macroscop.com:554/rtsp?channelid=3438760c-ea39-4301-9a36-f90871e95c6d
    // rtsp://polyakov2.ent.macroscop.com:554/rtsp?channelid=3438760c-ea39-4301-9a36-f90871e95c6d&streamtype=alternative
    // rtsp://polyakov2.ent.macroscop.com:554/rtsp?channelid=3438760c-ea39-4301-9a36-f90871e95c6d&streamtype=secondalternative
    // rtsp://polyakov2.ent.macroscop.com:554/rtsp?channelid=3438760c-ea39-4301-9a36-f90871e95c6d&streamtype=thirdalternative
    // rtsp://<ip адрес сервера macroscop и порт rtsp>/rtsp?channelid=<id канала>&login=<имя пользователя>&password=<хэш-строка MD5 пароля>[&sound=on][&streamtype=alternative]

    // Ссылки к архиву?

    class Program
    {
        static string ServerAddress;
        static ushort ServerPort = 8080;
        static string ServerLogin;
        static string ServerPassword = "";
        static bool IsLinksSoundRequired = false;
        static bool IsAllStreamsPerChannelRequired = false;
        static bool UseSecondaryAddress = false;
        static bool UseProxying = false;
        static bool IncludeDisabled = false;
        static FileInfo OutputFile;
        static StreamType? SpecificStream;

        static void Main(string[] args)
        {
            HashSet<ChannelLinks> links = new();

            var configPaths = ParseArgs(args);

            if (configPaths.Count > 0)
                foreach (var path in configPaths)
                    links.UnionWith(CreateLinks(new Configuration(File.ReadAllText(new FileInfo(path).FullName))));

            if (ServerAddress != null && ServerLogin != null)
            {
                try
                {
                    HttpClient macroscopClient = new();
                    macroscopClient.BaseAddress = new Uri($"http://{ServerAddress}:{ServerPort}");
                    HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Get, "configex?responsetype=json");
                    string authString = $"{ServerLogin}:{CreateMD5(ServerPassword)}";
                    message.Headers.Add("Authorization", $"Basic {Convert.ToBase64String(Encoding.ASCII.GetBytes(authString))}");

                    var response = macroscopClient.Send(message);
                    string config = response.Content.ReadAsStringAsync().Result;

                    if (response.StatusCode == System.Net.HttpStatusCode.OK) links.UnionWith(CreateLinks(new Configuration(config)));
                    else Console.WriteLine(config);
                }
                catch (HttpRequestException e) when (e.InnerException is IOException)
                {
                    Console.WriteLine($"{e.Message} {e.InnerException.Message}");
                }
            }

            if (OutputFile != null)
            {
                string table = "Channel Name,Main,Alternative,SecondAlternative,ThirdAlternative\n";
                foreach (var channel in links) table += $"{channel.Name},{string.Join(",", channel.LinksArray)}\n";
                try { File.WriteAllText(OutputFile.FullName, table, Encoding.UTF8); }
                catch (Exception e) { Console.WriteLine(e.Message); }
            }
            else if (links.Count > 0)
            {
                string output = JsonConvert.SerializeObject(links, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                Console.WriteLine(output);
            }
        }

        private static HashSet<ChannelLinks> CreateLinks(Configuration config)
        {
            HashSet<ChannelLinks> links = new();

            foreach (var channel in config.Channels)
            {
                Server server = (ServerAddress, UseProxying) switch
                {
                    (not null, _) => config.Servers.First(x => x.Id == config.SenderId),
                    (null, false) => config.Servers.First(x => x.Id == channel.AttachedToServer),
                    _ => config.Servers.First(x => x.Id == config.SenderId),
                };

                string serverAddress = UseSecondaryAddress && server.SecondaryIp != string.Empty ? server.SecondaryIp : server.PrimaryIp;

                if (channel.Streams.Count > 0)
                {
                    ChannelLinks channelLinks = new(channel.Name);

                    if (!IncludeDisabled && channel.IsDisabled) continue;

                    if (SpecificStream != null)
                        channelLinks.LinksArray[(int)SpecificStream] = ConcatLink(serverAddress, config.RtspServerPort, channel.Id, SpecificStream.ToString(), channel.IsSoundOn);
                    else
                    {
                        switch (IsAllStreamsPerChannelRequired)
                        {
                            case true:
                                foreach (var stream in Enum.GetNames(typeof(StreamType)))
                                    channelLinks.LinksArray[(int)Enum.Parse(typeof(StreamType), stream)] = ConcatLink(serverAddress, config.RtspServerPort, channel.Id, stream, channel.IsSoundOn);
                                break;
                            case false:
                                foreach (var stream in channel.Streams)
                                    channelLinks.LinksArray[(int)stream.StreamType] = ConcatLink(serverAddress, config.RtspServerPort, channel.Id, stream.StreamType.ToString(), channel.IsSoundOn);
                                break;
                        };
                    }

                    links.Add(channelLinks);
                }
            }

            return links;
        }

        private static string ConcatLink(string address, ushort port, string channelId, string streamType, bool isSoundOn)
        {
            string link = $"rtsp://{address}:{port}/rtsp?channelid={channelId}&";
            if (IsLinksSoundRequired && isSoundOn) link += $"sound=on&";
            link += $"streamtype={streamType.ToLower()}";
            if (ServerLogin != null) link += $"&login={ServerLogin}";
            if (ServerPassword != string.Empty) link += $"&password={CreateMD5(ServerPassword).ToLower()}";

            return link;
        }

        static readonly string[] ParamsDescription = new string[12]
        {
            "--server",     // ParamsDescription[0]
            "--port",       // ParamsDescription[1]
            "--login",      // ParamsDescription[2]
            "--password",   // ParamsDescription[3]
            "--stream",     // ParamsDescription[4]
            "--all",        // ParamsDescription[5]
            "-?",           // ParamsDescription[6]
            "--sound",      // ParamsDescription[7]
            "--secondary",  // ParamsDescription[8]
            "--proxy",      // ParamsDescription[9]
            "--disabled",   // ParamsDescription[10]
            "--output"      // ParamsDescription[11]
        };

        private static HashSet<string> ParseArgs(string[] args)
        {
            if (args.Length == 0) return ShowHelp() as HashSet<string>;
            HashSet<string> paths = new();

            for (int i = 0; i < args.Length; i++)
            {
                if ((args[i].Length == 2 && !args[i].Contains(":")) || args[i].StartsWith("--"))
                {
                    var argument = args[i][1..];

                    try
                    {
                        _ = argument.ToLower() switch
                        {
                            "-server" => (ServerAddress = args[i + 1], i++),                                               // ParamsDescription[0]
                            "-port" => (ServerPort = ushort.Parse(args[i + 1]), i++),                                      // ParamsDescription[1]
                            "-login" => (ServerLogin = args[i + 1], i++),                                                  // ParamsDescription[2]
                            "-password" => (ServerPassword = args[i + 1], i++),                                            // ParamsDescription[3]
                            "-stream" => (SpecificStream = (StreamType)Enum.Parse(typeof(StreamType), args[i + 1]), i++),  // ParamsDescription[4]
                            "-all" => (IsAllStreamsPerChannelRequired = true),                                             // ParamsDescription[5]
                            "?" => (ShowHelp()),                                                                           // ParamsDescription[6]
                            "-sound" => (IsLinksSoundRequired = true),                                                     // ParamsDescription[7]
                            "-secondary" => (UseSecondaryAddress = true),                                                  // ParamsDescription[8]
                            "-proxy" => (UseProxying = true),                                                              // ParamsDescription[9]
                            "-disabled" => (IncludeDisabled = true),                                                       // ParamsDescription[10]
                            "-output" => (OutputFile = new FileInfo(args[i + 1]), i++),                                    // ParamsDescription[11]
                            _ => throw new InvalidOperationException(message: $"Invalid input parameter: \"{args[i]}\""),
                        };

                        if (SpecificStream != null && (int)SpecificStream.Value > 3)
                        {
                            throw new InvalidOperationException(message: $"Invalid value for parameter: {ParamsDescription[3]} {SpecificStream}.");
                        }
                    }
                    catch (InvalidOperationException e)
                    {
                        Console.WriteLine(e.Message);
                        Environment.Exit(1);
                    }
                    catch (IndexOutOfRangeException)
                    {
                        Console.WriteLine($"Invalid value for parameter: {argument}.");
                        Environment.Exit(1);
                    }
                }
                else
                {
                    var attrs = File.GetAttributes(args[i]);
                    if (attrs.HasFlag(FileAttributes.Directory)) paths = paths.Concat(Directory.GetFiles(args[i])).ToHashSet();
                    else paths.Add(args[i]);
                }
            }

            return paths;
        }

        private static object ShowHelp()
        {
            var splittedString = new List<string>();

            var serverDescription = $"{ParamsDescription[0]} <address>";
            var portDescription = $"{ParamsDescription[1]} <port>";
            var loginDescription = $"{ParamsDescription[2]} <string>";
            var passwordDescription = $"{ParamsDescription[3]} <string>";
            var streamDescription = $"{ParamsDescription[4]} <number>";
            var outputDescription = $"{ParamsDescription[11]} <string>";

            var description = $"Usage: MacroscopRtspUrlGenerator [{serverDescription}] [{portDescription}] [{outputDescription}]";
            Console.WriteLine(description);

            splittedString.Add($"[{loginDescription}] [{passwordDescription}] ");
            splittedString.Add($"[{ParamsDescription[5]}] [{ParamsDescription[7]}] [{ParamsDescription[8]}] [{ParamsDescription[9]}]");
            splittedString.Add($"[{ParamsDescription[10]}] [{streamDescription}] [configex]");

            foreach (var str in splittedString) Console.WriteLine("{0,-33}{1}", "", str);

            Console.WriteLine(String.Format(
                "\n {0,-24}{1}\n {2,-24}{3}\n {4,-24}{5}\n {6,-24}{7}\n {8,-24}{9}\n {10,-24}{11}\n {12,-24}{13}\n {14,-24}{15}\n {16,-24}{17}\n {18,-24}{19}\n {20,-24}{21}\n {22,-24}{23}\n",
                $"{serverDescription}", "Server address.",
                $"{portDescription}", "Server port.",
                $"{loginDescription}", "Login.",
                $"{passwordDescription}", "Password.",
                $"{outputDescription}", "Specify to generate .csv table.",
                $"{streamDescription}", "Generate links only for specific stream. The minimum value is 0 - Main (first) stream. The maximum value is 3 - ThirdAlternative (fourth) stream.",
                $"{ParamsDescription[5]}", $"Generate all links per channel. If specified, generator will ignore {ParamsDescription[4]} parameter.",
                $"{ParamsDescription[7]}", "Add sound parameter, if sound is enabled on channel.",
                $"{ParamsDescription[8]}", "Use secondary address for server if it set.",
                $"{ParamsDescription[9]}", "Use the specified address for all channels regardless of which server the channel is attached to.",
                $"{ParamsDescription[10]}", "Generate links for disabled channels too.",
                $"{ParamsDescription[6]}", "Show this message and exit."));

            Environment.Exit(0);
            return null;
        }

        private static string CreateMD5(string input)
        {
            using System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] inputBytes = Encoding.ASCII.GetBytes(input);
            byte[] hashBytes = md5.ComputeHash(inputBytes);

            StringBuilder sb = new();
            for (int i = 0; i < hashBytes.Length; i++)
                sb.Append(hashBytes[i].ToString("X2", CultureInfo.CurrentCulture));

            return sb.ToString();
        }
    }
}
