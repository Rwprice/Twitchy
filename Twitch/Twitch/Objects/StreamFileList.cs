using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace TwitchAPIHandler.Objects
{
    public class StreamFileList
    {
        public List<string> IndexList { get; set; }

        public static async Task<StreamFileList> UpdateStreamFileList(M3U8Playlist playlist, string qualitySelection)
        {
            string path = playlist.streams[qualitySelection];
            Uri indexlist_path = new Uri(path);
            var request = HttpWebRequest.Create(indexlist_path);
            request.Method = "GET";
            var response = await HttpRequest(request);

            return new StreamFileList
            {
                IndexList = TranslateList(response)
            };
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

        private static List<string> TranslateList(string contents)
        {
            List<string> lines = contents.Split('\n').ToList<string>();

            for(int i = 0; i < lines.Count; i++)
            {
                string line = lines[i];
                if (line.Contains('#'))
                {
                    lines.Remove(line);
                    i--;
                }
                else if (line == "")
                {
                    lines.Remove(line);
                }
            }

            return lines;
        }
    }
}

/*
#EXTM3U
#EXT-X-VERSION:3
#EXT-X-TARGETDURATION:4
#EXT-X-MEDIA-SEQUENCE:2519
#EXTINF:4,
index2519.ts
#EXTINF:4,
index2520.ts
#EXTINF:4,
index2521.ts
#EXTINF:4,
index2522.ts
#EXTINF:4,
index2523.ts
*/