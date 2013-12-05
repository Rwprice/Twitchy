using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Windows.Storage;
using Windows.Storage.Streams;

namespace TwitchAPIHandler.Objects
{
    public class User
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Oauth { get; set; }

        public static async Task<User> GetUserFromOauth(string oauth)
        {
            Uri access_token_path = new Uri(string.Format(PathStrings.GET_USER_PATH, oauth));
            var request = HttpWebRequest.Create(access_token_path);
            request.Method = "GET";
            var response = await HttpRequest(request);
            JToken o = JObject.Parse(response);
            return new User
            {
                Name = (string)o.SelectToken("name"),
                DisplayName = (string)o.SelectToken("display_name"),
                Oauth = oauth
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

        public static async void SaveUser(User user)
        {
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            StorageFile textFile = await localFolder.CreateFileAsync("user", CreationCollisionOption.ReplaceExisting);

            using (IRandomAccessStream textStream = await textFile.OpenAsync(FileAccessMode.ReadWrite))
            {
                using (DataWriter textWriter = new DataWriter(textStream))
                {
                    textWriter.WriteString(user.Name + "\n" + user.DisplayName + "\n"
                        + user.Oauth);
                    await textWriter.StoreAsync();
                }
            }
        }

        public static async Task<User> TryLoadUser()
        {
            try
            {
                string contents;

                StorageFolder localFolder = ApplicationData.Current.LocalFolder;
                StorageFile textFile = await localFolder.GetFileAsync("user");

                using (IRandomAccessStream textStream = await textFile.OpenReadAsync())
                {
                    using (DataReader textReader = new DataReader(textStream))
                    {
                        uint textLength = (uint)textStream.Size;
                        await textReader.LoadAsync(textLength);
                        contents = textReader.ReadString(textLength);
                    }
                }

                string[] lines = contents.Split('\n');

                return new User
                {
                    Name = lines[0],
                    DisplayName = lines[1],
                    Oauth = lines[2]
                };
            }

            catch { return null; }
        }

        public static async void LogoutUser()
        {
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            StorageFile textFile = await localFolder.GetFileAsync("user");
            await textFile.DeleteAsync();
        }

        public static async Task<bool> IsStreamFollowed(string stream, User user)
        {
            Uri access_token_path = new Uri(string.Format(PathStrings.IS_STREAM_FOLLOWED_PATH, user.Name, stream));
            var request = HttpWebRequest.Create(access_token_path);
            request.Method = "GET";
            string response;

            try
            {
                response = await HttpRequest(request);
                return true;
            }
            catch { return false; }
            
        }

        public static async Task FollowStream(string stream, User user)
        {
            Uri access_token_path = new Uri(string.Format(PathStrings.FOLLOW_USER, user.Name, stream, user.Oauth));
            var request = HttpWebRequest.Create(access_token_path);
            request.Method = "PUT";
            var response = await HttpRequest(request);
        }

        public static async Task UnfollowStream(string stream, User user)
        {
            Uri access_token_path = new Uri(string.Format(PathStrings.FOLLOW_USER, user.Name, stream, user.Oauth));
            var request = HttpWebRequest.Create(access_token_path);
            request.Method = "DELETE";
            var response = await HttpRequest(request);
        }
    }
}
