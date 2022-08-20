using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Serilog;

namespace TwitterDump
{
    public static class Twitter
    {
        public static SearchResult SearchTweets(string query, Dictionary<string, string> headers, int? pageSize, int? maxResults)
        {
            if (pageSize == null)
            {
                pageSize = 100;
            }
            pageSize = Math.Max(pageSize.Value, 20);
            pageSize = Math.Min(pageSize.Value, 100);
            
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
                    {"count", pageSize.ToString() },
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
                Log.Logger.Information("Next {cursor}...", cursor);
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
                
                Log.Logger.Information("Returned {returned} tweets ({total} total}", tmpResult.Tweets.Count, result.Tweets.Count);

                if (maxResults.HasValue && result.Tweets.Count >= maxResults.Value)
                {
                    break;
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

            HttpClientHandler handler = new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.All
            };

            using (var http = new HttpClient(handler))
            {
                foreach (var header in headers)
                {
                    http.DefaultRequestHeaders.Add(header.Key, header.Value);
                }

                var responseContent = RetryGet(http, url);
                
                var dateTimeConverter = new IsoDateTimeConverter { DateTimeFormat = "ddd MMM dd HH:mm:ss zzzz yyyy", Culture = System.Globalization.CultureInfo.InvariantCulture };
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

                                string tweetId = null;
                                if (entry.content.item.content.tombstone != null)
                                {
                                    tweetId = (string)entry.content.item.content.tombstone.tweet.id;
                                }else if (entry.content.item.content.tweet != null)
                                {
                                    tweetId = (string) entry.content.item.content.tweet.id;
                                }
                                else
                                {
                                    continue;
                                }
                                
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
                    
                    nextCursor = cursor;
                }
                
                return result;
            }
        }

        private static string RetryGet(HttpClient client, string url)
        {
            string MakeRequest()
            {
                try
                {
                    var response = client.GetAsync(url).GetAwaiter()
                        .GetResult();
                    var responseContent = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    if (!response.IsSuccessStatusCode)
                    {
                        // {"errors":[{"message":"Over capacity","code":130}]}
                        if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
                        {
                            // This may be a result of "Over capacity, if it is, return NULL.
                            var json = JsonConvert.DeserializeObject<List<dynamic>>(responseContent);
                            if (json.Count == 1 && json[0].message != null & json[0].message == "Over capacity")
                            {
                                Log.Logger.Warning("Over capacity, retrying...");
                                return null;
                            }
                        }

                        Log.Logger.Warning("Request exception {statusCode} {body}, retrying...", response.StatusCode, responseContent);
                        return null;
                    }

                    return responseContent;
                }
                catch(Exception e)
                {
                    Log.Logger.Warning("Exception {message}, retrying...", e.Message);
                    return null;
                }
            }

            var count = 0;
            while (count < 10)
            {
                var content = MakeRequest();

                if (content == null)
                {
                    Thread.Sleep(1000);
                }
                else
                {
                    return content;
                }
                
                count++;
            }
            
            Log.Logger.Error("Too many retries, failed connection!");
            Environment.Exit(1);
            
            return null;
        }
    }
}