using System.Collections.Generic;

namespace TwitterDump
{
    public class SearchResult
    {
        public SearchResult()
        {
            Tweets = new List<Tweet>();
            Users = new List<User>();
        }
        
        public List<Tweet> Tweets { get; }
        
        public List<User> Users { get; }
    }
}