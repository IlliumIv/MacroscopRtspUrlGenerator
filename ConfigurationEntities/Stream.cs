using MacroscopRtspUrlGenerator.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;

namespace MacroscopRtspUrlGenerator.ConfigurationEntities
{
    public class Stream
    {
        private JToken jTokenBody { get; set; }
        [JsonConverter(typeof(StringEnumConverter))] public StreamType StreamType { get { return (StreamType)Enum.Parse(typeof(StreamType), (string)jTokenBody["StreamType"]); } }
        public Stream(JToken jToken) { jTokenBody = jToken; }
    }
}
