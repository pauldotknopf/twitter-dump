using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using CommandLine;
using Newtonsoft.Json;
using Serilog;
using Formatting = Newtonsoft.Json.Formatting;

namespace YouTubeDump
{
    partial class Program
    {
        [Verb("get-captions")]
        class GetCaptionsOptions : BaseIndexOptions
        {
            
        }

        private static async Task<int> GetCaptions(GetCaptionsOptions options)
        {
            options.Init();
            var indexPath = Path.Combine(options.IndexDirectory, "index.json");

            if (!File.Exists(indexPath))
            {
                Log.Logger.Error("The index.json file doesn't exist. Run \"index\" first.");
                return 1;
            }

            var index = JsonConvert.DeserializeObject<YouTubeDumpChannelIndex>(File.ReadAllText(indexPath));

            var captionDirectory = Path.Combine(options.IndexDirectory, "captions");
            if (!Directory.Exists(captionDirectory))
            {
                Directory.CreateDirectory(captionDirectory);
            }
            
            foreach (var video in index.Videos)
            {
                Log.Logger.Information("Downloading captions for {videoId}...", video.Id);
                
                var captionPath = Path.Combine(captionDirectory, $"{video.Id}.json");
                if (File.Exists(captionPath))
                {
                    Log.Logger.Information("Already downloaded, skipping...");
                    continue;
                }
                
                try
                {
                    var getVideoResponse =
                        GetRequestBody($"https://www.youtube.com/get_video_info?html5=1&video_id={video.Id}");

                    var keys = getVideoResponse.Split("&").Select(x =>
                    {
                        var split = x.Split("=");
                        return new Tuple<string, string>(split[0], HttpUtility.UrlDecode(split[1]));
                    }).ToDictionary(x => x.Item1, x => x.Item2);

                    var playerResponse = JsonConvert.DeserializeObject<GetVideoPlayerObject>(keys["player_response"]);

                    var caption = playerResponse.Captions?.PlayerCaptionsTracklistRenderer?.CaptionTracks
                        .FirstOrDefault();

                    if (caption == null)
                    {
                        Log.Logger.Warning("No caption present.");
                        continue;
                    }

                    var captionXml = GetRequestBody(caption.BaseUrl);

                    var xmlDoc = new XmlDocument();
                    xmlDoc.LoadXml(captionXml);

                    var captions = new List<YouTubeDumpCaption>();
                    foreach (XmlElement item in xmlDoc.GetElementsByTagName("text"))
                    {
                        var innerText = item.InnerText;
                        if (!string.IsNullOrEmpty(innerText))
                        {
                            innerText = HttpUtility.HtmlDecode(innerText);
                        }

                        innerText = Regex.Replace(innerText, @"<[^>]*>", "");
                        
                        captions.Add(new YouTubeDumpCaption
                        {
                            Start = double.Parse(item.GetAttribute("start")),
                            Duration = double.Parse(item.GetAttribute("dur")),
                            Value = innerText
                        });
                    }
                    
                    File.WriteAllText(captionPath, JsonConvert.SerializeObject(captions, Formatting.Indented));
                    
                    Log.Logger.Information("Saved!");
                }
                catch (Exception ex)
                {
                    Log.Logger.Error(ex, "Couldn't get timed text for {videoId}. " + ex.Message, video.Id);
                }
            }

            return 0;
        }
    }
}