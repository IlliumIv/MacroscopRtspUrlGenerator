using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MacroscopRtspUrlGenerator
{
    public class ChannelLinks
    {
        public string Name { get; }
        public IEnumerable<string> Links => LinksArray.Where(x => x != null);
        [JsonIgnore] public string[] LinksArray { get; } = new string[4];
        public ChannelLinks(string name) { Name = name; }
    }
}
