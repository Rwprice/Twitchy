using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace TwitchAPIHandler.Objects
{
    [DataContract]
    public class M3U8Playlist
    {
        [DataMember]
        public static Uri indexUri;
        [DataMember]
        public Dictionary<string, Uri> streams { get; set; }

        public static async Task<M3U8Playlist> GetStreamPlaylist(string Channel, AccessToken accessToken)
        {
            Uri m3u8_path = new Uri(string.Format(PathStrings.M3U8_PATH, Channel, accessToken.Token, accessToken.Signature));
            indexUri = m3u8_path;
            var request = HttpWebRequest.Create(m3u8_path);
            request.Method = "GET";
            var response = await HttpRequest(request);

            if (response == null)
                return null;

            return new M3U8Playlist
            {
                streams = GetStreamsFromM3U8(response)
            };
        }

        private static async Task<string> HttpRequest(WebRequest request)
        {
            try
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
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return null;
            }
        }

        private static Dictionary<string, Uri> GetStreamsFromM3U8(string fileContent)
        {
            Dictionary<string, Uri> QualityAndStreamPair = new Dictionary<string, Uri>();

            string[] lines = fileContent.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];

                if (line.Contains("NAME=") && !line.Contains("RESTRICTED"))
                {
                    string quality = line.Substring(line.IndexOf("NAME=") + 6);
                    quality = quality.Remove(quality.IndexOf(",AUTOSELECT=")-1);

                    QualityAndStreamPair.Add(quality, new Uri(lines[i + 2]));
                }
            }

            return QualityAndStreamPair;
        }

        public int GetIndexOfQuality(string Quality)
        {
            int index = -1;

            for (int i = 0; i < streams.Count; i++)
            {
                if (streams.Keys.ElementAt(i) == Quality)
                {
                    index = i;
                }
            }

            return index;
        }
    }
}