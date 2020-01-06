using System.Collections.Generic;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace YouTubeDump
{
    public class GetVideoPlayerObject
    {
        public class StreamingDataObject
        {
            [JsonProperty("formats")]
            public List<StreamingDataFormatObject> Formats { get; set; }
        }

        public class StreamingDataFormatObject
        {
            [JsonProperty("url")]
            public string Url { get; set; }
            
            [JsonProperty("mimeType")]
            public string MimeType { get; set;}
            
            [JsonProperty("bitrate")]
            public int Bitrate { get; set; }
        
            [JsonProperty("width")]
            public int Width { get; set; }
        
            [JsonProperty("height")]
            public int Height { get; set; }
        
            [JsonProperty("quality")]
            public string Quality { get; set; }
        }
        
        [JsonProperty("streamingData")]
        public StreamingDataObject StreamingData { get; set; }

        [JsonProperty("captions")]
        public CaptionsData Captions { get; set;}

        public class CaptionsData
        {
            [JsonProperty("playerCaptionsTracklistRenderer")]
            public PlayerCaptionsTracklistRendererData PlayerCaptionsTracklistRenderer { get; set;}
        }

        public class PlayerCaptionsTracklistRendererData
        {
            [JsonProperty("captionTracks")]
            public List<PlayerCaptionsTracklistRendererDataCaptionTrack> CaptionTracks { get; set;}
        }

        public class PlayerCaptionsTracklistRendererDataCaptionTrack
        {
            [JsonPropertyName("baseUrl")]
            public string BaseUrl { get; set; }
        }
    }
}