using MacroscopRtspUrlGenerator.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MacroscopRtspUrlGenerator
{
    // rtsp://polyakov2.ent.macroscop.com:554/rtsp?channelid=3438760c-ea39-4301-9a36-f90871e95c6d
    // rtsp://polyakov2.ent.macroscop.com:554/rtsp?channelid=3438760c-ea39-4301-9a36-f90871e95c6d&streamtype=alternative
    // rtsp://polyakov2.ent.macroscop.com:554/rtsp?channelid=3438760c-ea39-4301-9a36-f90871e95c6d&streamtype=secondalternative
    // rtsp://polyakov2.ent.macroscop.com:554/rtsp?channelid=3438760c-ea39-4301-9a36-f90871e95c6d&streamtype=thirdalternative

    class Program
    {
        static readonly string[] ParamsDescription = new string[10]
        {
                "-s", "--server",
                "-l", "--login",
                "-p", "--password",
                "-n", "--streams",
                "--all",
                "-?"
        };

        static string ServerAddress = "127.0.0.1:8080";
        static string ServerLogin = "root";
        static string ServerPassword = "";
        
        static StreamsDepth StreamNumber = StreamsDepth.All;

        static void Main(string[] args)
        {
            var configPaths = ParseArgs(args);

            if (configPaths.Count > 0)
            {
                var fInfo = new FileInfo(configPaths.First());
                var conf = File.ReadAllText(fInfo.FullName);

                var some = new Configuration(conf);

                Console.WriteLine(JsonConvert.SerializeObject(some));
            }
        }

        private static HashSet<string> ParseArgs(string[] args)
        {
            if (args.Length == 0) return ShowHelp() as HashSet<string>;

            HashSet<string> paths = new();

            for (int i = 0; i < args.Length; i++)
            {
                if ((args[i].Length == 2 && !args[i].Contains(":")) || args[i].StartsWith("--"))
                {
                    var argument = args[i][1..];

                    _ = argument.ToLower() switch
                    {
                        "s"         => (ServerAddress = args[i + 1], i++),                                                     // ParamsDescription[0]
                        "-server"   => (ServerAddress = args[i + 1], i++),                                                     // ParamsDescription[1]
                        "l"         => (ServerLogin = args[i + 1], i++),                                                       // ParamsDescription[2]
                        "-login"    => (ServerLogin = args[i + 1], i++),                                                       // ParamsDescription[3]
                        "p"         => (ServerPassword = args[i + 1], i++),                                                    // ParamsDescription[4]
                        "-password" => (ServerPassword = args[i + 1], i++),                                                    // ParamsDescription[5]
                        "n"         => (StreamNumber = (StreamsDepth)Enum.Parse(typeof(StreamsDepth), args[i + 1]), i++),      // ParamsDescription[6]
                        "-streams"  => (StreamNumber = (StreamsDepth)Enum.Parse(typeof(StreamsDepth), args[i + 1]), i++),      // ParamsDescription[7]
                        "-all"      => (StreamNumber = StreamsDepth.All),                                                      // ParamsDescription[8]
                        "?"         => (ShowHelp()),                                                                           // ParamsDescription[9]
                        _ => throw new NotImplementedException(message: $"Invalid input parameter: \"{args[i]}\""),
                    };
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
            Console.WriteLine(String.Format(
                "\n {0,-24}{1}\n {2,-24}{3}\n {4,-24}{5}\n {6,-24}{7}\n {8,-24}{9}\n {10,-24}{11}\n",
                $"{ParamsDescription[0]}, {ParamsDescription[1]} <address>", "Server address",
                $"{ParamsDescription[2]}, {ParamsDescription[3]} <string>", "Login",
                $"{ParamsDescription[4]}, {ParamsDescription[5]} <string>", "Password",
                $"{ParamsDescription[6]}, {ParamsDescription[7]} <number>", "Streams number. The minimum value is 0 (first stream). The maximum value is 4 (all streams).",
                $"{ParamsDescription[8]}", "Generate all existing links by channel",
                $"{ParamsDescription[9]}", "Show this message and exit"));

            Environment.Exit(0);
            return null;
        }
    }
}
