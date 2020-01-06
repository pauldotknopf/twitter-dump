using System.Collections.Generic;
using Newtonsoft.Json;

namespace YouTubeDump
{
    public class YouTubeDumpChannelIndex
    {
        [JsonProperty("channel")]
        public YouTubeDumpChannel Channel { get; set; }
        
        [JsonProperty("videos")]
        public List<YouTubeDumpVideo> Videos { get; set; }
    }
}