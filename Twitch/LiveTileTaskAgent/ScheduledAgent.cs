using Microsoft.Phone.Scheduler;
using Microsoft.Phone.Shell;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Resources;

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
                        await LiveTileHelper.UpdateLiveTile(task.Description);
                        await LiveTileHelper.UpdateSecondaryTiles();
                    }

                    else
                        LiveTileHelper.ResetLiveTile();
                }
            }

            NotifyComplete();
        }
    }
}