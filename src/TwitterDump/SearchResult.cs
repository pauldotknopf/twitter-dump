using System.Collections.Generic;
using Newtonsoft.Json;

namespace TwitterDump
{
    public class SearchResult
    {
        public SearchResult()
        {
            Tweets = new List<Tweet>();
            Users = new List<User>();
        }
        
        [JsonProperty("query")]
        public string Query { get; set; }
        
        [JsonProperty("tweets")]
        public List<Tweet> Tweets { get; }
        
        [JsonProperty("users")]
        public List<User> Users { get; }
    }
}