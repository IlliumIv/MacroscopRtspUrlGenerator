using Newtonsoft.Json.Linq;

namespace MacroscopRtspUrlGenerator.ConfigurationEntities
{
    public class Server
    {
        private JToken jTokenBody { get; set; }
        public string Id { get { return (string)jTokenBody["Id"]; } }
        public string Name { get { return (string)jTokenBody["Name"]; } }
        public string Url { get { return (string)jTokenBody["Url"]; } }
        public string PrimaryIp { get { return (string)jTokenBody["PrimaryIp"]; } }
        public string SecondaryIp { get { return (string)jTokenBody["SecondaryIp"]; } }
        public Server(JToken jToken) { jTokenBody = jToken; }
    }
}
