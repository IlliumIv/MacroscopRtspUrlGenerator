using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MacroscopRtspUrlGenerator
{
    public class Configuration
    {
        protected static dynamic JsonBody { get; private set; }
        public string SenderId { get; } = JsonBody.SenderId;
        public bool? IsRtspServerEnabled { get; } = JsonBody.RtspServerInfo.IsEnabled;
        public ushort? RtspServerPort { get; } = JsonBody.RtspServerInfo.TcpPort;
        public JArray Channels { get; } = JsonBody.Channels;
        public JArray Servers { get; } = JsonBody.Servers;

        public Configuration(string jsonString)
        {
            TryParse(jsonString, JsonBody);
        }

        public bool TryParse(string jsonString, out dynamic result)
        {
            result = null;
            JsonBody = JsonConvert.DeserializeObject<dynamic>(jsonString);
            result = JsonBody;
            return true;
        }
    }
}
