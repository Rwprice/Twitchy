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
        public static string GET_ALL_LIVE_FOLLOWED_STREAMS = PathStrings.GET_ALL_LIVE_FOLLOWED_STREAMS;
        public static string FRONT_PAGE_STREAMS_PATH = PathStrings.FRONT_PAGE_STREAMS_PATH;

        public static async Task<Stream> GetStream(string streamName)
        {
            Uri path = new Uri(string.Format(PathStrings.GET_STREAM, streamName));
            var request = HttpWebRequest.Create(path);
            request.Method = "GET";
            try
            {
                var response = await HttpRequest(request);
                JToken o = JObject.Parse(response);
                JToken stream = o.SelectToken("stream");

                var channel = new Channel();

                var viewers = int.Parse(stream.SelectToken("viewers").ToString());
                var display_name = stream.SelectToken("channel").SelectToken("display_name").ToString();
                var name = stream.SelectToken("channel").SelectToken("name").ToString();
                var game = stream.SelectToken("channel").SelectToken("game").ToString();
                var status = "";
                var logo = stream.SelectToken("channel").SelectToken("logo").ToString();

                try
                {
                    status = stream.SelectToken("channel").SelectToken("status").ToString();
                }

                catch
                {
                    if (status == "")
                    {
                        status = display_name;
                    }
                }

                channel = new Channel
                {
                    display_name = display_name,
                    name = name,
                    status = status,
                    logoUri = logo,
                    game = game
                };

                var streamToReturn = new Stream()
                {
                    channel = channel,
                    viewers = viewers
                };

                return streamToReturn;
            }

            catch
            {
                return new Stream();
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

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (null != handler)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public override bool Equals(object obj)
        {
            var item = obj as Stream;

            if (item == null)
            {
                return false;
            }

            return this.channel.Equals(item.channel);
        }

        public override int GetHashCode()
        {
            return this.channel.GetHashCode();
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
        public string logoUri { get; set; }
        public string game { get; set; }

        public override bool Equals(object obj)
        {
            var item = obj as Channel;

            if (item == null)
            {
                return false;
            }

            return this.name.Equals(item.name) && this.display_name.Equals(item.display_name);
        }

        public override int GetHashCode()
        {
            return this.name.GetHashCode();
        }
    }

    public class Notification
    {
        public string display_name { get; set; }
        public string name { get; set; }
        public bool notify { get; set; }
        public bool live { get; set; }

        public override bool Equals(object obj)
        {
            var item = obj as Notification;

            if (item == null)
            {
                return false;
            }

            return this.name.Equals(item.name) && this.display_name.Equals(item.display_name);
        }

        public override int GetHashCode()
        {
            return this.name.GetHashCode();
        }
    }
}
