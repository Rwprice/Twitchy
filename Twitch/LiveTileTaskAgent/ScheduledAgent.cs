using Microsoft.Phone.Scheduler;
using System.Diagnostics;
using System.Windows;

namespace LiveTileTaskAgent
{
    public class ScheduledAgent : ScheduledTaskAgent
    {
        private static string liveTileTaskName = "LiveTileTask";

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

        protected override void OnInvoke(ScheduledTask task)
        {
            if (task is PeriodicTask)
            {
                if (task.Name == liveTileTaskName)
                {
                    if (task.Description != "No OAuth to use")
                    {
                        try
                        {
                            LiveTileHelper.UpdateLiveTile(task.Description).Wait();
                            LiveTileHelper.UpdateSecondaryTiles().Wait();
                            LiveTileHelper.SendNotifications().Wait();
                        }
                        catch { }
                    }

                    else
                        LiveTileHelper.ResetLiveTile();
                }
            }

#if DEBUG
            ScheduledActionService.LaunchForTest(liveTileTaskName, TimeSpan.FromSeconds(15));
#endif

            NotifyComplete();
        }
    }
}