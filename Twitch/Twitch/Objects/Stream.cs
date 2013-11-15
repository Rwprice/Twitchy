using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace TwitchAPIHandler.Objects
{
    public class Stream
    {
        public int viewers { get; set; }
        public Preview preview { get; set; }
        public Channel channel { get; set; }

        public static async Task<List<Stream>> GetFeaturedStreams()
        {
            Uri front_page_streams_path = new Uri(PathStrings.FRONT_PAGE_STREAMS_PATH);
            var request = HttpWebRequest.Create(front_page_streams_path);
            request.Method = "GET";
            var response = await HttpRequest(request);
            return JsonToFeaturedStreamsList(response);
        }

        public static async Task<List<Stream>> GetTopStreams()
        {
            Uri top_streams_path = new Uri(PathStrings.TOP_STREAMS_PATH);
            var request = HttpWebRequest.Create(top_streams_path);
            request.Method = "GET";
            var response = await HttpRequest(request);
            return JsonToTopStreamsList(response);
        }

        public static async Task<List<Stream>> GetTopStreamsForGame(string gameName)
        {
            Uri top_streams_path = new Uri(string.Format(PathStrings.TOP_STREAMS_FOR_GAME_PATH, gameName));
            var request = HttpWebRequest.Create(top_streams_path);
            request.Method = "GET";
            var response = await HttpRequest(request);
            return JsonToTopStreamsList(response);
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
                        small = new Uri(stream.SelectToken("preview").SelectToken("small").ToString()),
                        medium = new Uri(stream.SelectToken("preview").SelectToken("medium").ToString())
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
                        small = new Uri(arrayValue.SelectToken("preview").SelectToken("small").ToString()),
                        medium = new Uri(arrayValue.SelectToken("preview").SelectToken("medium").ToString())
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
    }

    public class Preview
    {
        public Uri medium { get; set; }
        public Uri small { get; set; }
    }

    public class Channel
    {
        public string status { get; set; }
        public string display_name { get; set; }
        public string name { get; set; }
    }
}
