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
        private static string liveTileTaskName = "UpdateLiveTileTask";
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
                    var StreamsList = new List<String>();
                    string received = "";

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
                        StreamsList.Add(display_name);
                    }

                    string content = "No streams followed are live";

                    if (StreamsList.Count > 0)
                    {
                        content = string.Format("{0} is live now!", StreamsList[rand.Next(0, StreamsList.Count)]);
                    }

                    FlipTileData flipTileData = new FlipTileData()
                    {
                        BackContent = content
                    };

                    ShellTile appTile = ShellTile.ActiveTiles.First();
                    if (appTile != null)
                    {
                        appTile.Update(flipTileData);
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