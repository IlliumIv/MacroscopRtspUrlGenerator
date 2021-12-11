using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace MacroscopRtspUrlGenerator.ConfigurationEntities
{
    public class Channel
    {
        private JToken jTokenBody { get; set; }
        private JArray RawStreams { get { return (JArray)jTokenBody["Streams"]; } }
        public string Id { get { return (string)jTokenBody["Id"]; } }
        public string Name { get { return (string)jTokenBody["Name"]; } }
        public string AttachedToServer { get { return (string)jTokenBody["AttachedToServer"]; } }
        public bool IsDisabled { get { return (bool)jTokenBody["IsDisabled"]; } }
        public bool IsSoundOn { get { return (bool)jTokenBody["IsSoundOn"]; } }
        public bool IsArchivingEnabled { get { return (bool)jTokenBody["IsArchivingEnabled"]; } }
        public bool IsSoundArchivingEnabled { get { return (bool)jTokenBody["IsSoundArchivingEnabled"]; } }
        public bool AllowedRealtime { get { return (bool)jTokenBody["AllowedRealtime"]; } }
        public bool AllowedArchive { get { return (bool)jTokenBody["AllowedArchive"]; } }
        public HashSet<Stream> Streams { get; } = new HashSet<Stream>();
        public Channel(JToken jToken)
        {
            jTokenBody = jToken;
            foreach (var rawStream in RawStreams) { Streams.Add(new Stream(rawStream)); };
        }
    }
}
