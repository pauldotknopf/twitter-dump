using System;
using System.Linq;
using Newtonsoft.Json;

namespace TwitterDump
{
    public class Tweet : IEquatable<Tweet>
    {
        [JsonProperty("id")]
        public ulong Id { get; set; }

        [JsonProperty("created_at")]
        public DateTimeOffset CreatedAt { get; set; }
        
        [JsonProperty("full_text")]
        public string FullText { get; set; }
        
        [JsonProperty("user_id")]
        public ulong UserId { get; set; }
        
        public bool Equals(Tweet other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return GetHashCode() == other.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((User) obj);
        }

        public override int GetHashCode()
        {
            // ReSharper disable once NonReadonlyMemberInGetHashCode
            return Id.GetHashCode();
        }
    }

    public class TweetWithUrl : Tweet
    {
        private readonly SearchResult _result;

        public TweetWithUrl(Tweet existing, SearchResult result)
        {
            Id = existing.Id;
            CreatedAt = existing.CreatedAt;
            FullText = existing.FullText;
            UserId = existing.UserId;
            _result = result;
        }

        [JsonProperty("url")]
        public string Url
        {
            get
            {
                var user = _result.Users.FirstOrDefault(x => x.Id == UserId);
                if (user == null)
                {
                    return null;
                }

                return $"https://twitter.com/{user.ScreenName}/status/{Id}";
            }
        }
    }
}