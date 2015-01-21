using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace TwitchAPIHandler.Objects
{
    public class Stream : INotifyPropertyChanged
    {
        private int _viewers;
        public int viewers
        {
            get
            {
                return _viewers;
            }
            set
            {
                if (value != _viewers)
                {
                    _viewers = value;
                    NotifyPropertyChanged("viewers");
                }
            }
        }

        private Preview _preview;
        public Preview preview
        {
            get
            {
                return _preview;
            }
            set
            {
                if (value != _preview)
                {
                    _preview = value;
                    NotifyPropertyChanged("preview");
                }
            }
        }

        private Channel _channel;
        public Channel channel
        {
            get
            {
                return _channel;
            }
            set
            {
                if (value != _channel)
                {
                    _channel = value;
                    NotifyPropertyChanged("channel");
                }
            }
        }

        public static string TOP_STREAMS_FOR_GAME_PATH = PathStrings.TOP_STREAMS_FOR_GAME_PATH;
        public static string SEARCH_GAME_PATH = PathStrings.SEARCH_GAME_PATH;
        public static string SEARCH_STREAM_PATH = PathStrings.SEARCH_STREAM_PATH;
        public static string TOP_STREAMS_PATH = PathStrings.TOP_STREAMS_PATH;
        public static string GET_FOLLOWED_STREAMS = PathStrings.GET_FOLLOWED_STREAMS;
        public static string GET_ALL_FOLLOWED_STREAMS = PathStrings.GET_ALL_FOLLOWED_STREAMS;

        public static async Task<ObservableCollection<Stream>> GetFeaturedStreams()
        {
            Uri front_page_streams_path = new Uri(PathStrings.FRONT_PAGE_STREAMS_PATH);
            var request = HttpWebRequest.Create(front_page_streams_path);
            request.Method = "GET";
            try
            {
                var response = await HttpRequest(request);
                var streams = JsonToFeaturedStreamsList(response);
                var streamsToReturn = new ObservableCollection<Stream>();
                foreach (var stream in streams)
                    streamsToReturn.Add(stream);
                return streamsToReturn;
            }

            catch
            {
                return new ObservableCollection<Stream>();
            }
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

        public static List<Stream> JsonToFeaturedStreamsList(string json)
        {
            List<Stream> featuredStreams = new List<Stream>();
            JToken o = JObject.Parse(json);
            JArray featured = JArray.Parse(o.SelectToken("featured").ToString());

            foreach (var arrayValue in featured)
            {
                JToken stream = arrayValue.SelectToken("stream");
                featuredStreams.Add(new Stream()
                {
                    viewers = int.Parse(stream.SelectToken("viewers").ToString()),
                    preview = new Preview
                    {
                        small = new BitmapImage(new Uri(stream.SelectToken("preview").SelectToken("small").ToString())),
                        medium = new BitmapImage(new Uri(stream.SelectToken("preview").SelectToken("medium").ToString()))
                    },
                    channel = new Channel
                    {
                        display_name = stream.SelectToken("channel").SelectToken("display_name").ToString(),
                        name = stream.SelectToken("channel").SelectToken("name").ToString(),
                        status = stream.SelectToken("channel").SelectToken("status").ToString()
                    }
                });
            }

            return featuredStreams;
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

    public class Preview
    {
        public BitmapImage medium { get; set; }
        public BitmapImage small { get; set; }
    }

    public class Channel
    {
        public string status { get; set; }
        public string display_name { get; set; }
        public string name { get; set; }
    }
}
