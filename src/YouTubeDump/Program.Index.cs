using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Newtonsoft.Json;
using Serilog;

namespace YouTubeDump
{
    partial class Program
    {
        [Verb("index")]
        class IndexOptions : BaseIndexOptions
        {
            [Value(0, MetaName = "channel-id", Required = true)]
            public string ChannelId { get; set; }
        }

        private static async Task<int> Index(IndexOptions options)
        {
            options.Init();

            var youtubeService = await GetYouTubeService();

            Log.Logger.Information("Getting channel info for {channelId}...", options.ChannelId);
            var channel = await GetChannel(options.ChannelId, youtubeService);

            Log.Logger.Information("Getting uploaded videos for channel {channel}...", channel.Title);
            var videos = await GetVideos(channel, youtubeService);

            Log.Logger.Information("Saving index.json...");
            var destinationFile = Path.Combine(options.IndexDirectory, "index.json");

            if (File.Exists(destinationFile))
            {
                File.Delete(destinationFile);
            }
            
            File.WriteAllText(destinationFile, JsonConvert.SerializeObject(new YouTubeDumpChannelIndex
            {
                Channel = channel,
                Videos = videos
            }, Formatting.Indented));
            
            Log.Logger.Information("Indexed!");
            
            return 0;
        }
        
        private static async Task<YouTubeDumpChannel> GetChannel(string channelId, YouTubeService youTubeService)
        {
            var channelRequest = youTubeService.Channels.List("contentDetails,topicDetails,snippet");
            channelRequest.Id = channelId;

            var channelResponse = (await channelRequest.ExecuteAsync());

            if (channelResponse.Items == null || channelResponse.Items.Count != 1)
            {
                Log.Logger.Error("Couldn't get channel by the given id.");
                Environment.Exit(1);
            }

            var channel = channelResponse.Items.Single();

            return new YouTubeDumpChannel
            {
                Id = channel.Id,
                Title = channel.Snippet.Title,
                UploadPlaylistId = channel.ContentDetails.RelatedPlaylists.Uploads
            };
        }

        private static async Task<List<YouTubeDumpVideo>> GetVideos(YouTubeDumpChannel channel, YouTubeService youTubeService)
        {
            var videosRequest = youTubeService.PlaylistItems.List("snippet,contentDetails");
            videosRequest.PlaylistId = channel.UploadPlaylistId;
            videosRequest.MaxResults = 50;

            var videos = new List<PlaylistItem>();
            var videosResponse = await videosRequest.ExecuteAsync();

            while (videosResponse.Items.Count > 0)
            {
                videos.AddRange(videosResponse.Items);

                if (!string.IsNullOrEmpty(videosResponse.NextPageToken))
                {
                    videosRequest.PageToken = videosResponse.NextPageToken;
                    videosResponse = await videosRequest.ExecuteAsync();
                }
                else
                {
                    videosResponse.Items.Clear();
                }
            }

            return videos.Select(x => new YouTubeDumpVideo
            {
                Id = x.ContentDetails.VideoId,
                Title = x.Snippet.Title,
                UploadedOn = x.Snippet.PublishedAt.HasValue ? new DateTimeOffset(x.Snippet.PublishedAt.Value) : (DateTimeOffset?)null
            }).ToList();
        }
           
    }
}