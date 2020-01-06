using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CommandLine;
using Newtonsoft.Json;
using Serilog;

namespace YouTubeDump
{
    partial class Program
    {
        [Verb("search-captions")]
        class SearchCaptionsOptions : BaseIndexOptions
        {
            [Option('q', "query", Required = true)]
            public string Query { get; set; }
        }

        private static async Task<int> SearchCaptions(SearchCaptionsOptions options)
        {
            if (string.IsNullOrEmpty(options.Query))
            {
                Log.Logger.Error("You must provide a query.");
                Environment.Exit(1);
            }
            
            options.Init();

            var index = options.GetIndex();

            foreach (var video in index.Videos)
            {
                var captionPath = Path.Combine(options.IndexDirectory, "captions", $"{video.Id}.json");
                if (!File.Exists(captionPath))
                {
                    continue;
                }

                var captions = JsonConvert.DeserializeObject<List<YouTubeDumpCaption>>(File.ReadAllText(captionPath));

                for (var x = 0; x < captions.Count; x++)
                {
                    var current = captions[x];

                    if (!(current.Value ?? "").ToLower().Contains(options.Query))
                    {
                        continue;
                    }
                    
                    YouTubeDumpCaption previous = null, next = null;
                    if (x > 0)
                    {
                        previous = captions[x - 1];
                    }

                    if (x < captions.Count - 1)
                    {
                        next = captions[x + 1];
                    }
                    
                    Console.WriteLine("----------");
                    Console.WriteLine($"Id: {video.Id}");
                    Console.WriteLine($"Title: {video.Title}");
                    Console.WriteLine("Content:");
                    if (previous != null)
                    {
                        Console.WriteLine($"\t{previous.Value}");
                    }
                    Console.WriteLine($"\t{current.Value}");
                    if (next != null)
                    {
                        Console.WriteLine($"\t{next.Value}");
                    }
                    Console.WriteLine(current.Start);
                    Console.WriteLine($"Url: https://www.youtube.com/watch?v={video.Id}&t={Math.Max(Math.Floor(current.Start), 0)}s");
                }
            }

            return 0;
        }
    }
}