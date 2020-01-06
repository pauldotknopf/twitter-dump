using System;
using Newtonsoft.Json;

namespace YouTubeDump
{
    public class YouTubeDumpVideo
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        
        [JsonProperty("title")]
        public string Title { get; set; }
        
        [JsonProperty("uploaded-on")]
        public DateTimeOffset? UploadedOn { get; set; }
    }
}