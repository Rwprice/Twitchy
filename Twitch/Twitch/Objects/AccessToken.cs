using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace TwitchAPIHandler.Objects
{
    public class AccessToken
    {
        public string Token { get; set; }
        public string Signature { get; set; }

        public static async Task<AccessToken> GetToken(string Channel)
        {
            Uri access_token_path = new Uri(string.Format(PathStrings.TOKEN_PATH, Channel));
            var request = HttpWebRequest.Create(access_token_path);
            request.Method = "GET";
            var response = await HttpRequest(request);
            JToken o = JObject.Parse(response);
            return new AccessToken
            {
                Token = (string)o.SelectToken("token"),
                Signature = (string)o.SelectToken("sig")
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

        public static string GetAuthorizationTokenURI()
        {
            return PathStrings.GET_AUTHORIZATION_TOKEN;
        }
    }
}