using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Serilog;

namespace TwitterDump
{
    public static class Twitter
    {
        public static SearchResult SearchTweets(string query, Dictionary<string, string> headers)
        {
            var uri = QueryHelpers.AddQueryString("https://api.twitter.com/2/search/adaptive.json",
                new Dictionary<string, string>
                {
                    {"include_profile_interstitial_type", "1"},
                    {"include_blocking", "1"},
                    {"include_blocked_by", "1"},
                    {"include_followed_by", "1"},
                    {"include_want_retweets", "1"},
                    {"include_mute_edge", "1"},
                    {"include_can_dm", "1"},
                    {"include_can_media_tag", "1"},
                    {"skip_status", "1"},
                    {"cards_platform", "Web-12"},
                    {"include_cards", "1"},
                    {"include_composer_source", "true"},
                    {"include_ext_alt_text", "true"},
                    {"include_reply_count", "1"},
                    {"tweet_mode", "extended"},
                    {"include_entities", "true"},
                    {"include_user_entities", "true"},
                    {"include_ext_media_color", "true"},
                    {"include_ext_media_availability", "true"},
                    {"send_error_codes", "true"},
                    {"simple_quoted_tweets", "true"},
                    {"tweet_search_mode", "live"},
                    {"count", "20"},
                    {"query_source", "typed_query"},
                    {"pc", "1"},
                    {"spelling_corrections", "1"},
                    {"ext", "mediaStats,highlightedLabel,cameraMoment"}
                });

            var result = GetTweets(
                QueryHelpers.AddQueryString(uri, new Dictionary<string, string>
                {
                    { "q", query }
                }),
                headers,
                out var cursor);

            while (!string.IsNullOrEmpty(cursor))
            {
                Thread.Sleep(TimeSpan.FromSeconds(1));
                var tmpResult = GetTweets(QueryHelpers.AddQueryString(uri, new Dictionary<string, string>
                    {
                        { "q", query },
                        { "cursor", cursor }
                    }),
                    headers,
                    out cursor);
                foreach (var tweet in tmpResult.Tweets)
                {
                    if (result.Tweets.Contains(tweet))
                    {
                        Log.Logger.Warning("Duplicate tweet returned from API {tweetId}, skipping...", tweet.Id);
                        continue;
                    }
                    result.Tweets.Add(tweet);
                }

                foreach (var user in tmpResult.Users)
                {
                    if (!result.Users.Contains(user))
                    {
                        result.Users.Add(user);
                    }
                }
            }

            result.Query = query;
            return result;
        }

        private static SearchResult GetTweets(string url, Dictionary<string, string> headers, out string nextCursor)
        {
            Log.Logger.Information("Requesting {url}..", url);
            
            var result = new SearchResult();
            nextCursor = null;
            string cursor = null;
            var hasEntries = false;
            
            
            using (var http = new HttpClient())
            {
                foreach (var header in headers)
                {
                    http.DefaultRequestHeaders.Add(header.Key, header.Value);
                }
  
                var response = http.GetAsync(url).GetAwaiter()
                    .GetResult();
                var responseContent = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception(responseContent);
                }

                var dateTimeConverter = new IsoDateTimeConverter { DateTimeFormat = "ddd MMM dd HH:mm:ss zzzz yyyy" };
                var deserializeSettings = new JsonSerializerSettings();
                deserializeSettings.Converters.Add(dateTimeConverter);
                var json = JsonConvert.DeserializeObject<dynamic>(responseContent);

                foreach (JProperty user in json.globalObjects.users)
                {
                    var userId = ulong.Parse(user.Name);
                    var value = (JObject)user.Value;
                    if (result.Users.All(x => x.Id != userId))
                    {
                        var userObject = value.ToObject<User>();
                        Log.Logger.Information("Adding {user}...", userObject.ScreenName);
                        result.Users.Add(value.ToObject<User>());
                    }
                }
                
                foreach (var instruction in json.timeline.instructions)
                {
                    if (instruction.addEntries != null)
                    {
                        foreach (var entry in instruction.addEntries.entries)
                        {
                            if (((string) entry.entryId).StartsWith("sq-I-t"))
                            {
                                hasEntries = true;
                                var tweetId = (string) entry.content.item.content.tweet.id;
                                var tweetData = (JObject)json.globalObjects.tweets[tweetId];
                                if (tweetData == null)
                                {
                                    // Deleted? Suspended account?
                                    Log.Logger.Warning("Deleted tweet {tweetId}, skipping...", tweetId);
                                    continue;
                                }
                                result.Tweets.Add(tweetData.ToObject<Tweet>(JsonSerializer.Create(deserializeSettings)));
                            }
                            else if (((string) entry.entryId).StartsWith("sq-cursor-bottom"))
                            {
                                cursor = (string)entry.content.operation.cursor.value;
                            }
                        }
                    }
                    else if (instruction.replaceEntry != null)
                    {
                        if (((string) instruction.replaceEntry.entryIdToReplace).StartsWith("sq-cursor-bottom"))
                        {
                            cursor = (string)instruction.replaceEntry.entry.content.operation.cursor.value;
                        }
                    }
                }

                if (hasEntries)
                {
                    Log.Logger.Information("Next {cursor}...", cursor);
                    nextCursor = cursor;
                }
                
                Log.Logger.Information("Returned {tweetCount} tweets.", result.Tweets.Count);
                
                return result;
            }
        }
    }
}