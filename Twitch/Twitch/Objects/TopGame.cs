using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;
using System.ComponentModel;

namespace TwitchAPIHandler.Objects
{
    public class TopGame : INotifyPropertyChanged
    {
        private int _channels;
        public int channels
        {
            get
            {
                return _channels;
            }
            set
            {
                if (value != _channels)
                {
                    _channels = value;
                    NotifyPropertyChanged("channels");
                }
            }
        }

        private Game _game;
        public Game game
        {
            get
            {
                return _game;
            }
            set
            {
                if (value != _game)
                {
                    _game = value;
                    NotifyPropertyChanged("game");
                }
            }
        }

        public static async Task<ObservableCollection<TopGame>> GetTopGames()
        {
            Uri top_games_path = new Uri(string.Format(PathStrings.TOP_GAMES_PATH, 0));
            var request = HttpWebRequest.Create(top_games_path);
            request.Method = "GET";
            try
            {
                var response = await HttpRequest(request);
                var games = JsonToTopGames(response);
                var gamesToReturn = new ObservableCollection<TopGame>();
                foreach (var game in games)
                    gamesToReturn.Add(game);
                return gamesToReturn;
            }

            catch
            {
                return new ObservableCollection<TopGame>();
            }
        }

        public static async Task<ObservableCollection<TopGame>> SearchGames(string query)
        {
            Uri top_games_path = new Uri(string.Format(PathStrings.SEARCH_GAME_PATH, query));
            var request = HttpWebRequest.Create(top_games_path);
            request.Method = "GET";
            try
            {
                var response = await HttpRequest(request);
                var games = JsonToSearchResults(response);
                var gamesToReturn = new ObservableCollection<TopGame>();
                foreach (var game in games)
                    gamesToReturn.Add(game);
                return gamesToReturn;
            }

            catch
            {
                return new ObservableCollection<TopGame>();
            }
        }

        private static List<TopGame> JsonToSearchResults(string json)
        {
            List<TopGame> searchResults = new List<TopGame>();
            JToken o = JObject.Parse(json);
            JArray games = JArray.Parse(o.SelectToken("games").ToString());

            foreach (var arrayValue in games)
            {
                searchResults.Add(new TopGame()
                {
                    game = new Game
                    {
                        name = arrayValue.SelectToken("name").ToString(),
                        box = new Box
                        {
                            medium = new BitmapImage(new Uri(arrayValue.SelectToken("box").SelectToken("medium").ToString()))
                        }
                    },
                });
            }

            return searchResults;
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
                            medium = new BitmapImage(new Uri(arrayValue.SelectToken("game").SelectToken("box").SelectToken("medium").ToString()))
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

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (null != handler)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

    public class Game
    {
        public string name { get; set; }
        public Box box { get; set; }
    }

    public class Box
    {
        public BitmapImage medium { get; set; }
    }
}
