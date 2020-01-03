using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CommandLine;
using Jint;
using Newtonsoft.Json;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

namespace TwitterDump
{
    class Program
    {
        static int Main(string[] args)
        {
            var headers = new Dictionary<string, string>();
            var r = ParseCurlCommand(
                "curl 'https://api.twitter.com/2/search/adaptive.json?include_profile_interstitial_type=1&include_blocking=1&include_blocked_by=1&include_followed_by=1&include_want_retweets=1&include_mute_edge=1&include_can_dm=1&include_can_media_tag=1&skip_status=1&cards_platform=Web-12&include_cards=1&include_composer_source=true&include_ext_alt_text=true&include_reply_count=1&tweet_mode=extended&include_entities=true&include_user_entities=true&include_ext_media_color=true&include_ext_media_availability=true&send_error_codes=true&simple_quoted_tweets=true&q=test&count=20&query_source=typed_query&pc=1&spelling_corrections=1&ext=mediaStats%2ChighlightedLabel%2CcameraMoment' -H 'authority: api.twitter.com' -H 'origin: https://twitter.com' -H 'x-twitter-client-language: en' -H 'x-csrf-token: b9e42043eec4f9eb8d2a68930d944c95' -H 'authorization: Bearer AAAAAAAAAAAAAAAAAAAAANRILgAAAAAAnNwIzUejRCOuH5E6I8xnZz4puTs%3D1Zv7ttfk8LF81IUq16cHjhLTvJu4FA33AGWWjCpTnA' -H 'user-agent: Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/79.0.3945.88 Safari/537.36' -H 'x-twitter-auth-type: OAuth2Session' -H 'x-twitter-active-user: yes' -H 'accept: */*' -H 'sec-fetch-site: same-site' -H 'sec-fetch-mode: cors' -H 'referer: https://twitter.com/search?q=test&src=typed_query' -H 'accept-encoding: gzip, deflate, br' -H 'accept-language: en-US,en;q=0.9,la;q=0.8' -H 'cookie: dnt=1; kdt=D2ZuwxUO5O3mHbg1sHx2K2Ix7wYM1JH9jK4MDlZR; remember_checked_on=1; csrf_same_site_set=1; rweb_optin=side_no_out; csrf_same_site=1; _ga=GA1.2.995291469.1572878948; tfw_exp=0; _gid=GA1.2.493100779.1577499190; lang=en; ct0=b9e42043eec4f9eb8d2a68930d944c95; ads_prefs=\"HBISAAA=\"; auth_multi=\"1173455037642792961:145038920a28dfd6c5d08004df3eb8af7d2846aa\"; auth_token=7367a55d9acc160329d62a77ad84470168502abb; personalization_id=\"v1_zhle+/wWZ+flbE7GE5hfSA==\"; guest_id=v1%3A157791343781956906; twid=u%3D46621029; _gat=1' --compressed",
                out headers);
            
            
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console(standardErrorFromLevel: LogEventLevel.Verbose)
                .CreateLogger();
            
            return Parser.Default.ParseArguments<SearchOptions, AuthOptions>(args)
                .MapResult(
                    (SearchOptions opts) => Search(opts),
                    (AuthOptions opts) => Auth(opts),
                    errs => 1);
        }
        
        [Verb("search")]
        class SearchOptions
        {
            [Option('o', "output", Required = true, Default = "stdout")]
            public string Output { get; set; }
            
            [Option('q', "query", Required = true)]
            public string Query { get; set; }
            
            [Option('p', "page-size", Default = 100)]
            public int? PageSize { get; set; }
            
            [Option('m', "max-results")]
            public int? MaxResults { get; set; }
        }

        private static int Search(SearchOptions options)
        {
            var twitterDumpAuthFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), ".twitter-dump-auth.json");
            if (!File.Exists(twitterDumpAuthFile))
            {
                Log.Logger.Error("You must run \"auth\" first.");
                return 1;
            }

            var headers = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(twitterDumpAuthFile));
            headers["accept-encoding"] = "UTF-8";
            
            var tweets = Twitter.SearchTweets(options.Query, headers, options.PageSize, options.MaxResults);
            var tweetsWithUrl = new SearchResult();
            tweetsWithUrl.Query = tweets.Query;
            tweetsWithUrl.Tweets.AddRange(tweets.Tweets.Select(x => new TweetWithUrl(x, tweets)));
            tweetsWithUrl.Users.AddRange(tweets.Users);
            var tweetsJson = JsonConvert.SerializeObject(tweetsWithUrl, Formatting.Indented);
            
            if (options.Output == "stdout")
            {
                Console.WriteLine(tweetsJson);
            }
            else
            {
                if (File.Exists(options.Output))
                {
                    File.Delete(options.Output);
                }
                File.WriteAllText(options.Output, tweetsJson);
            }
            
            return 0;
        }

        [Verb("auth", HelpText = "Add file contents to the index.")]
        class AuthOptions
        {
            
        }

        private static int Auth(AuthOptions options)
        {
            var steps = new StringBuilder();
            steps.AppendLine("Steps to authenticate:");

            steps.AppendLine("Step 1: With Chrome, authenticate with Twitter and then navigate to: https://twitter.com/search");
            steps.AppendLine("Step 2: Open Chrome developer tools");
            steps.AppendLine("Step 3: Open the Network tab on the developer tools");
            steps.AppendLine("Step 4: Filter requests for \"adaptive.json\"");
            steps.AppendLine("Step 5: Search for anything (doesn't matter)");
            steps.AppendLine("Step 6: Scroll down until a network request for \"adapative.json\" is made");
            steps.AppendLine("Step 7: Right click the request and click \"Copy -> Copy as cURL\"");
            steps.Append("Step 8: Paste the contents of your clipboard below");
            Console.WriteLine(steps);
            
            Console.Out.Write("Result: ");
            var curlCommand = Console.In.ReadLine();

            Dictionary<string, string> headers;
            while (!ParseCurlCommand(curlCommand, out headers))
            {
                Console.WriteLine();
                Console.Write("Invalid fetch command, try again:");
                curlCommand = Console.In.ReadLine();
            }

            var twitterDumpAuthFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), ".twitter-dump-auth.json");

            if (File.Exists(twitterDumpAuthFile))
            {
                File.Delete(twitterDumpAuthFile);
            }
            
            File.WriteAllText(twitterDumpAuthFile, JsonConvert.SerializeObject(headers, Formatting.Indented));
            
            Console.WriteLine("Saved!");
            
            return 0;
        }

        private static bool ParseCurlCommand(string curlCommand, out Dictionary<string, string> headers)
        {
            if (string.IsNullOrEmpty(curlCommand))
            {
                headers = null;
                return false;
            }
            
            headers = new Dictionary<string, string>();
            var currentIndex = curlCommand.IndexOf("-H", StringComparison.Ordinal);

            while (currentIndex != -1)
            {
                var openingIndex = curlCommand.IndexOf("'", currentIndex, StringComparison.Ordinal);
                var closingIndex = curlCommand.IndexOf("'", openingIndex + 1, StringComparison.Ordinal);

                var header = curlCommand.Substring(openingIndex + 1, closingIndex - openingIndex - 1);
                
                headers.Add(header.Substring(0, header.IndexOf(":", StringComparison.Ordinal)).Trim(), header.Substring(header.IndexOf(":", StringComparison.Ordinal) + 1).Trim());

                currentIndex = curlCommand.IndexOf("-H", closingIndex, StringComparison.Ordinal);
            }

            return true;
        }
    }
}
