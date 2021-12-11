using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace MacroscopRtspUrlGenerator.ConfigurationEntities
{
    public class Configuration
    {
        private static dynamic JsonBody { get; set; }
        private JArray RawChannels { get { return JsonBody.Channels; } }
        private JArray RawServers { get { return JsonBody.Servers; } }
        public string SenderId { get { return JsonBody.SenderId; } }
        public bool IsRtspServerEnabled { get { return JsonBody.RtspServerInfo.IsEnabled; } }
        public ushort RtspServerPort { get { return JsonBody.RtspServerInfo.TcpPort; } }
        public HashSet<Channel> Channels { get; } = new HashSet<Channel>();
        public HashSet<Server> Servers { get; } = new HashSet<Server>();

        public Configuration(string jsonString)
        {
            JsonBody = JsonConvert.DeserializeObject<dynamic>(jsonString);
            foreach (var rawChannel in RawChannels) { Channels.Add(new Channel(rawChannel)); };
            foreach (var rawServer in RawServers) { Servers.Add(new Server(rawServer)); };
        }
    }
}
