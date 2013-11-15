using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace TwitchAPIHandler.Objects
{
    public class TopGame
    {
        public int channels { get; set; }
        public Game game { get; set; }

        public static async Task<List<TopGame>> GetTopGames()
        {
            Uri top_games_path = new Uri(PathStrings.TOP_GAMES_PATH);
            var request = HttpWebRequest.Create(top_games_path);
            request.Method = "GET";
            var response = await HttpRequest(request);
            return JsonToTopGames(response);
        }

        private static List<TopGame> JsonToTopGames(string json)
        {
            List<TopGame> topGames = new List<TopGame>();
            JToken o = JObject.Parse(json);
            JArray featured = JArray.Parse(o.SelectToken("top").ToString());

            foreach (var arrayValue in featured)
            {
                topGames.Add(new TopGame()
                {
                    channels = int.Parse(arrayValue.SelectToken("channels").ToString()),
                    game = new Game
                    {
                        name = arrayValue.SelectToken("game").SelectToken("name").ToString(),
                        box = new Box
                        {
                            medium = new Uri(arrayValue.SelectToken("game").SelectToken("box").SelectToken("medium").ToString())
                        }
                    },
                });
            }

            return topGames;
        }

        private static async Task<string> HttpRequest(WebRequest request)
        {
            string received = "";

            using (var response = (HttpWebResponse)(await Task<WebResponse>.Factory.FromAsync(request.BeginGetResponse, request.EndGetResponse, null)))
            {
                using (var responseStream = response.GetResponseStream())
                {
                    using (var sr = new StreamReader(responseStream))
                    {
                        received = await sr.ReadToEndAsync();
                    }
                }
            }


            return received;
        }
    }

    public class Game
    {
        public string name { get; set; }
        public Box box { get; set; }
    }

    public class Box
    {
        public Uri medium { get; set; }
    }
}
