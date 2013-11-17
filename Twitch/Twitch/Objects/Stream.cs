﻿using Newtonsoft.Json.Linq;
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

        public static async Task<ObservableCollection<Stream>> GetFeaturedStreams()
        {
            Uri front_page_streams_path = new Uri(PathStrings.FRONT_PAGE_STREAMS_PATH);
            var request = HttpWebRequest.Create(front_page_streams_path);
            request.Method = "GET";
            var response = await HttpRequest(request);
            var streams = JsonToFeaturedStreamsList(response);
            var streamsToReturn = new ObservableCollection<Stream>();
            foreach (var stream in streams)
                streamsToReturn.Add(stream);
            return streamsToReturn;
        }

        public static async Task<ObservableCollection<Stream>> GetTopStreams()
        {
            Uri top_streams_path = new Uri(string.Format(PathStrings.TOP_STREAMS_PATH, 0));
            var request = HttpWebRequest.Create(top_streams_path);
            request.Method = "GET";
            var response = await HttpRequest(request);
            var streams = JsonToTopStreamsList(response);
            var streamsToReturn = new ObservableCollection<Stream>();
            foreach (var stream in streams)
                streamsToReturn.Add(stream);
            return streamsToReturn;
        }

        public static async Task<ObservableCollection<Stream>> GetTopStreamsForGame(string gameName)
        {
            Uri top_streams_path = new Uri(string.Format(PathStrings.TOP_STREAMS_FOR_GAME_PATH, gameName, 0));
            var request = HttpWebRequest.Create(top_streams_path);
            request.Method = "GET";
            var response = await HttpRequest(request);
            var streams = JsonToTopStreamsList(response);
            var streamsToReturn = new ObservableCollection<Stream>();
            foreach (var stream in streams)
                streamsToReturn.Add(stream);
            return streamsToReturn;
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

        private static List<Stream> JsonToFeaturedStreamsList(string json)
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

        private static List<Stream> JsonToTopStreamsList(string json)
        {
            List<Stream> featuredStreams = new List<Stream>();
            JToken o = JObject.Parse(json);
            JArray featured = JArray.Parse(o.SelectToken("streams").ToString());

            foreach (var arrayValue in featured)
            {
                featuredStreams.Add(new Stream()
                {
                    viewers = int.Parse(arrayValue.SelectToken("viewers").ToString()),
                    preview = new Preview
                    {
                        small = new BitmapImage(new Uri(arrayValue.SelectToken("preview").SelectToken("small").ToString())),
                        medium = new BitmapImage(new Uri(arrayValue.SelectToken("preview").SelectToken("medium").ToString()))
                    },
                    channel = new Channel
                    {
                        display_name = arrayValue.SelectToken("channel").SelectToken("display_name").ToString(),
                        name = arrayValue.SelectToken("channel").SelectToken("name").ToString(),
                        status = arrayValue.SelectToken("channel").SelectToken("status").ToString()
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
