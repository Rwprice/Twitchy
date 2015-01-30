using System.Diagnostics;
using System.Windows;
using Microsoft.Phone.Scheduler;
using System;
using System.Linq;
using Microsoft.Phone.Shell;
using System.Net;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LiveTileTaskAgent
{
    public class ScheduledAgent : ScheduledTaskAgent
    {
        private static string liveTileTaskName = "LiveTileTask";
        private Random rand = new Random();

        static ScheduledAgent()
        {
            // Subscribe to the managed exception handler
            Deployment.Current.Dispatcher.BeginInvoke(delegate
            {
                Application.Current.UnhandledException += UnhandledException;
            });
        }

        private static void UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            if (Debugger.IsAttached)
            {
                // An unhandled exception has occurred; break into the debugger
                Debugger.Break();
            }
        }

        protected async override void OnInvoke(ScheduledTask task)
        {
            if (task is PeriodicTask)
            {
                if (task.Name == liveTileTaskName)
                {
                    if (task.Description != "No OAuth to use")
                    {
                        #region LiveTile
                        var StreamsList = new List<TwitchAPIHandler.Objects.Stream>();
                        string received = "";

                        try
                        {
                            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(new Uri(String.Format(TwitchAPIHandler.Objects.Stream.GET_ALL_FOLLOWED_STREAMS, task.Description)));
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
                        }

                        catch { }

                        string content = "Live Now:";
                        string contentTwo = "None";
                        string contentThree = "";

                        if (StreamsList.Count > 0)
                        {
                            var index = rand.Next(0, StreamsList.Count);
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
                        #endregion

                        #region SecondaryTiles
                        //Get Secondary Tiles
                        var tiles = ShellTile.ActiveTiles.Where(x => x.NavigationUri.OriginalString.Contains("PlayerPage")).ToList();
                        foreach (var tile in tiles)
                        {
                            var streamName = tile.NavigationUri.OriginalString.Substring(tile.NavigationUri.OriginalString.IndexOf('?') + 1);

                            //Lookup live or not
                            var stream = await TwitchAPIHandler.Objects.Stream.GetStream(streamName);
                            bool streamLive = stream.channel != null ? true : false;

                            //Display Badge and update icon
                            StandardTileData tileData = new StandardTileData();
                            tile.Update(tileData);
                        }
                        #endregion
                    }

                    else
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
                    
                }
            }

            #if DEBUG
                ScheduledActionService.LaunchForTest(task.Name, TimeSpan.FromSeconds(60));
            #endif

            NotifyComplete();
        }
    }
}