using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommandLine;
using Newtonsoft.Json;
using Serilog;

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
                
                var captionPath = Path.Combine(captionDirectory, $"{video.Id}.xml");
                if (File.Exists(captionPath))
                {
                    Log.Logger.Information("Already downloaded, skipping...");
                    continue;
                }
                
                try
                {
                    var videoUrl = $"https://www.youtube.com/watch?v={video.Id}";
                    var videoHtml = GetRequestBody(videoUrl);

                    var timedTextUrl = Regex.Match(videoHtml,
                        @"captionTracks\\"":\[\{\\\""baseUrl\\\""\:\\\""((.*))\\\"",\\\""name\\\""");

                    if (!timedTextUrl.Success)
                    {
                        Log.Logger.Warning("Couldn't find timed text url.");
                        continue;
                    }

                    var timedTextUrlValue = timedTextUrl.Groups[2].Value;
                    
                    timedTextUrlValue = timedTextUrlValue.Replace("\\/", "/")
                        .Replace("\\\\u0026", "&");

                    var timedTextXml = GetRequestBody(timedTextUrlValue);

                    File.WriteAllText(captionPath, timedTextXml);
                    
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