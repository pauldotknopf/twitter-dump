using Newtonsoft.Json;

namespace YouTubeDump
{
    public class YouTubeDumpCaption
    {
        [JsonProperty("start")]
        public double Start { get; set; }
            
        [JsonProperty("duraction")]
        public double Duration { get; set; }
            
        [JsonProperty("value")]
        public string Value { get; set; }
    }
}