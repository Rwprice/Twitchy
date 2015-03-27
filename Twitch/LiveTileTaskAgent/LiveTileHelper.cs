using Microsoft.Phone.Shell;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Resources;
using Windows.Storage;
using Windows.Storage.Streams;

namespace LiveTileTaskAgent
{
    public class LiveTileHelper
    {
        static string imageFolder = "shared\\shellcontent\\";

        public async static Task<bool> UpdateLiveTile(string oAuth)
        {
            var StreamsList = new List<TwitchAPIHandler.Objects.Stream>();
            string received = "";

            try
            {
                HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(new Uri(String.Format(TwitchAPIHandler.Objects.Stream.GET_ALL_LIVE_FOLLOWED_STREAMS, oAuth)));
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

                JToken o = JObject.Parse(received);
                JArray featured = JArray.Parse(o.SelectToken("streams").ToString());

                foreach (var arrayValue in featured)
                {
                    var display_name = arrayValue.SelectToken("channel").SelectToken("display_name").ToString();
                    var status = "";

                    try
                    {
                        status = arrayValue.SelectToken("channel").SelectToken("status").ToString();
                    }
                    catch { }

                    StreamsList.Add(new TwitchAPIHandler.Objects.Stream() { channel = new TwitchAPIHandler.Objects.Channel() { status = status, display_name = display_name } });
                }

                string content = "Live Now:";
                string contentTwo = "None";
                string contentThree = "";

                if (StreamsList.Count > 0)
                {
                    var index = new Random().Next(0, StreamsList.Count);
                    contentTwo = StreamsList[index].channel.display_name;
                    contentThree = StreamsList[index].channel.status;
                }

                IconicTileData flipTileData = new IconicTileData()
                {
                    WideContent1 = content,
                    WideContent2 = contentTwo,
                    WideContent3 = contentThree,
                    Count = StreamsList.Count,
                    Title = "Twitchy",
                    IconImage = new Uri("/Assets/logo.png", UriKind.Relative)
                };

                ShellTile appTile = ShellTile.ActiveTiles.First();
                if (appTile != null)
                {
                    appTile.Update(flipTileData);
                }
            }

            catch { }

            return true;
        }

        public async static Task<bool> UpdateSecondaryTiles()
        {
            //Get Secondary Tiles
            var tiles = ShellTile.ActiveTiles.Where(x => x.NavigationUri.OriginalString.Contains("PlayerPage")).ToList();
            foreach (var tile in tiles)
            {
                var streamName = tile.NavigationUri.OriginalString.Substring(tile.NavigationUri.OriginalString.IndexOf('?') + 1);

                //Lookup live or not
                var stream = await TwitchAPIHandler.Objects.Stream.GetStream(streamName);
                bool streamLive = stream.channel != null ? true : false;

                StandardTileData tileData = new StandardTileData();
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    var image = GetImagePath(streamName, streamLive);
                    if (image != null)
                    {
                        tileData.BackgroundImage = image;
                        tile.Update(tileData);
                    }
                });
            }
            return true;
        }

        public async static Task<bool> SendNotifications()
        {
            try
            {
                string contents;

                StorageFolder localFolder = ApplicationData.Current.LocalFolder;
                StorageFile textFile = await localFolder.GetFileAsync("notifications");

                using (IRandomAccessStream textStream = await textFile.OpenReadAsync())
                {
                    using (DataReader textReader = new DataReader(textStream))
                    {
                        uint textLength = (uint)textStream.Size;
                        await textReader.LoadAsync(textLength);
                        contents = textReader.ReadString(textLength);
                    }
                }

                List<TwitchAPIHandler.Objects.Notification> channelsToNotify = new List<TwitchAPIHandler.Objects.Notification>();
                foreach (var channel in contents.Split('\n'))
                {
                    if (channel != "")
                    {
                        var split = channel.Split(':');
                        if(split.Length == 3)
                            channelsToNotify.Add(new TwitchAPIHandler.Objects.Notification() { name = split[1], display_name = split[0], live = bool.Parse(split[2])});

                        else
                            channelsToNotify.Add(new TwitchAPIHandler.Objects.Notification() { name = split[1], display_name = split[0], live = false});
                    }
                }


                foreach (var channel in channelsToNotify)
                {
                    //Lookup live or not
                    var stream = await TwitchAPIHandler.Objects.Stream.GetStream(channel.name);
                    bool streamLive = stream.channel != null ? true : false;

                    if (streamLive)
                    {
                        if (!channel.live)
                        {
                            ShellToast toast = new ShellToast();
                            toast.Title = string.Format("{0} is now live!", stream.channel.display_name);
                            toast.Content = stream.channel.status ?? string.Format("Tap here to watch {0}", stream.channel.display_name);
                            toast.NavigationUri = new Uri(string.Concat("/Screens/PlayerPage.xaml?", stream.channel.name), UriKind.RelativeOrAbsolute);
                            toast.Show();
                            channel.live = true;
                        }
                    }


                    else
                    {
                        channel.live = false;
                    }
                }

                textFile = await localFolder.CreateFileAsync("notifications", CreationCollisionOption.ReplaceExisting);

                using (IRandomAccessStream textStream = await textFile.OpenAsync(FileAccessMode.ReadWrite))
                {
                    using (DataWriter textWriter = new DataWriter(textStream))
                    {
                        foreach (var notif in channelsToNotify)
                        {
                            textWriter.WriteString(string.Format("{0}:{1}:{2}{3}", notif.display_name, notif.name, notif.live, "\n"));
                        }
                        await textWriter.StoreAsync();
                    }
                }

                return true;
            }

            catch
            {
                return false;
            }
        }

        public static void ResetLiveTile()
        {
            IconicTileData flipTileData = new IconicTileData()
            {
                WideContent1 = "Login to Twitchy!",
                WideContent2 = "Login and enable live tiles",
                WideContent3 = "to view followed streams",
                Title = "Twitchy",
                IconImage = new Uri("/Assets/logo.png", UriKind.Relative)
            };

            ShellTile appTile = ShellTile.ActiveTiles.First();
            if (appTile != null)
            {
                appTile.Update(flipTileData);
            }
        }

        public static ShellTile FindTile(string partOfUri)
        {
            ShellTile shellTile = ShellTile.ActiveTiles.FirstOrDefault(
                tile => tile.NavigationUri.ToString().Contains(partOfUri));

            return shellTile;
        }

        public static void SaveTileImages(string channelName, Uri image)
        {
            var bitmapImage = new BitmapImage() { CreateOptions = BitmapCreateOptions.None };
            bitmapImage.ImageFailed += (s, e) => 
            { };
            bitmapImage.ImageOpened += (s, e) =>
            {
                #region Create Image with Live Logo
                using (IsolatedStorageFile local = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    // Create a new folder named DataFolder.
                    if (!local.DirectoryExists(imageFolder))
                        local.CreateDirectory(imageFolder);

                    var writeableBitmap = new WriteableBitmap(s as BitmapImage);
                    writeableBitmap.Invalidate();

                    string offlineFilePath = System.IO.Path.Combine(imageFolder, channelName + ".jpg");
                    using (var imageOfflineFileStream = new IsolatedStorageFileStream(offlineFilePath, System.IO.FileMode.Create, local))
                    {
                        writeableBitmap.SaveJpeg(imageOfflineFileStream, writeableBitmap.PixelWidth, writeableBitmap.PixelHeight, 0, 100);
                    }

                    BitmapImage icon = new BitmapImage();
                    icon.CreateOptions = BitmapCreateOptions.None;
                    icon.UriSource = new Uri("/Assets/live_icon.png", UriKind.Relative);
                    WriteableBitmap iconWB = new WriteableBitmap(icon);

                    int AnsX1 = 175;
                    int AnsY1 = 0;
                    int AnsX2 = 300;
                    int AnsY2 = 125;

                    Rect sourceRect = new Rect(0, 0, iconWB.PixelWidth, iconWB.PixelHeight);
                    Rect destRect = new Rect(AnsX1, AnsY1, AnsX2 - AnsX1, AnsY2 - AnsY1);

                    writeableBitmap.Blit(destRect, iconWB, sourceRect);
                    writeableBitmap.Invalidate();

                    string onlineFilePath = System.IO.Path.Combine(imageFolder, "live-" + channelName + ".jpg");
                    using (var imageOnlineFileStream = new IsolatedStorageFileStream(onlineFilePath, System.IO.FileMode.Create, local))
                    {
                        writeableBitmap.SaveJpeg(imageOnlineFileStream, writeableBitmap.PixelWidth, writeableBitmap.PixelHeight, 0, 100);
                    }
                }
                #endregion
            };

            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                bitmapImage.UriSource = image;
            });
        }

        public static Uri GetImagePath(string channelName, bool live)
        {
            if (live)
                channelName = "live-" + channelName + ".jpg";

            else
                channelName = channelName + ".jpg";

            string filePath = System.IO.Path.Combine(imageFolder, channelName);

            using (IsolatedStorageFile local = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if(local.FileExists(filePath))
                    return new Uri("isostore:" + filePath, UriKind.RelativeOrAbsolute);
            }

            return null;
        }

        public static void DeleteImage(string channelName)
        {
            using (IsolatedStorageFile local = IsolatedStorageFile.GetUserStoreForApplication())
            {
                local.DeleteFile(System.IO.Path.Combine(imageFolder, channelName + ".jpg"));
                local.DeleteFile(System.IO.Path.Combine(imageFolder, "live-" + channelName + ".jpg"));
            }
        }
    }
}
