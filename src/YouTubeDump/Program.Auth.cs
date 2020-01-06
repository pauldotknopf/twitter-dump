using System;
using System.IO;
using CommandLine;
using Google.Apis.Auth.OAuth2;
using Newtonsoft.Json;
using Serilog;

namespace YouTubeDump
{
    partial class Program
    {
        [Verb("auth")]
        class AuthOptions
        {
            
        }

        private static int Auth(AuthOptions options)
        {
            var clientId = ReadLine.Read("Client id:");

            if (string.IsNullOrEmpty(clientId))
            {
                Log.Logger.Error("You must provide a client id.");
                return 1;
            }
            
            var clientSecret = ReadLine.Read("Client secret:");

            if (string.IsNullOrEmpty(clientSecret))
            {
                Log.Logger.Error("You must provide a client secret.");
                return 1;
            }
            
            var youtubeAuthFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), ".youtube-dump-auth.json");

            if (File.Exists(youtubeAuthFile))
            {
                File.Delete(youtubeAuthFile);
            }
            
            File.WriteAllText(youtubeAuthFile, JsonConvert.SerializeObject(new ClientSecrets
            {
                ClientId = clientId,
                ClientSecret = clientSecret
            }, Formatting.Indented));
            
            Console.WriteLine("Saved!");
            
            return 0;
        }
    }
}