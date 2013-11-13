using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace TwitchAPIHandler.Objects
{
    public class M3U8Playlist
    {
        public Dictionary<string, string> streams { get; set; }

        public static async Task<M3U8Playlist> GetStreamPlaylist(string Channel, AccessToken accessToken)
        {
            Uri m3u8_path = new Uri(string.Format(PathStrings.M3U8_PATH, Channel, accessToken.Token, accessToken.Signature));
            var request = HttpWebRequest.Create(m3u8_path);
            request.Method = "GET";
            var response = await HttpRequest(request);

            return new M3U8Playlist
            {
                streams = GetStreamsFromM3U8(response)
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

        private static Dictionary<string, string> GetStreamsFromM3U8(string fileContent)
        {
            Dictionary<string, string> QualityAndStreamPair = new Dictionary<string, string>();

            string[] lines = fileContent.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];

                if (line.Contains("NAME="))
                {
                    string quality = line.Substring(line.IndexOf("NAME=") + 6);
                    quality = quality.Remove(quality.IndexOf(",AUTOSELECT=")-1);

                    QualityAndStreamPair.Add(quality, lines[i + 2]);
                }
            }

            return QualityAndStreamPair;
        }
    }
}


/*
#EXTM3U
#EXT-X-TWITCH-INFO:NODE="video4.iad02",CLUSTER="iad02",MANIFEST-CLUSTER="iad02",MANIFEST-NODE="video4.iad02"
#EXT-X-MEDIA:TYPE=VIDEO,GROUP-ID="high",NAME="High",AUTOSELECT=YES,DEFAULT=YES
#EXT-X-STREAM-INF:PROGRAM-ID=1,BANDWIDTH=1760000,VIDEO="high"
http://video4.iad02.hls.justin.tv/hls4/sirhcez_7481245584_34193496/high/index.m3u8?token=id=4400828025259116645,bid=7481245584,exp=1384441201,node=video4-1.iad02.hls.justin.tv,nname=video4.iad02,fmt=high&sig=b88ecabceebd334b3201fe2dc78431480750593c&
#EXT-X-MEDIA:TYPE=VIDEO,GROUP-ID="medium",NAME="Medium",AUTOSELECT=YES,DEFAULT=YES
#EXT-X-STREAM-INF:PROGRAM-ID=1,BANDWIDTH=928000,VIDEO="medium"
http://video4.iad02.hls.justin.tv/hls4/sirhcez_7481245584_34193496/medium/index.m3u8?token=id=4400828025259116645,bid=7481245584,exp=1384441201,node=video4-1.iad02.hls.justin.tv,nname=video4.iad02,fmt=medium&sig=513f79dfe1219085b265a4b9e186950b3af88627&
#EXT-X-MEDIA:TYPE=VIDEO,GROUP-ID="low",NAME="Low",AUTOSELECT=YES,DEFAULT=YES
#EXT-X-STREAM-INF:PROGRAM-ID=1,BANDWIDTH=596000,VIDEO="low"
http://video4.iad02.hls.justin.tv/hls4/sirhcez_7481245584_34193496/low/index.m3u8?token=id=4400828025259116645,bid=7481245584,exp=1384441201,node=video4-1.iad02.hls.justin.tv,nname=video4.iad02,fmt=low&sig=ad7981f170c5bf2b047590632f581db5980350db&
#EXT-X-MEDIA:TYPE=VIDEO,GROUP-ID="mobile",NAME="Mobile",AUTOSELECT=YES,DEFAULT=YES
#EXT-X-STREAM-INF:PROGRAM-ID=1,BANDWIDTH=164000,VIDEO="mobile"
http://video4.iad02.hls.justin.tv/hls4/sirhcez_7481245584_34193496/mobile/index.m3u8?token=id=4400828025259116645,bid=7481245584,exp=1384441201,node=video4-1.iad02.hls.justin.tv,nname=video4.iad02,fmt=mobile&sig=041be9181eace3e28e883e092ce98287a7484967&
*/