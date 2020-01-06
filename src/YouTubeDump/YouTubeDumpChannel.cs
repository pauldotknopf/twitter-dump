using Newtonsoft.Json;

namespace YouTubeDump
{
    public class YouTubeDumpChannel
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        
        [JsonProperty("title")]
        public string Title { get; set; }
        
        [JsonProperty("upload-playlist-id")]
        public string UploadPlaylistId { get; set; }
    }
}