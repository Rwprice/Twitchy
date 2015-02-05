using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using TwitchTV.Resources;
using Microsoft.Phone.Scheduler;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using LiveTileTaskAgent;

namespace TwitchTV
{
    public partial class SettingsPage : PhoneApplicationPage
    {
        private static string liveTileTaskName = "LiveTileTask";

        public SettingsPage()
        {
            InitializeComponent();
            this.AutoJoinChatButton.IsChecked = App.ViewModel.AutoJoinChat;
            this.LockLandscapeButton.IsChecked = App.ViewModel.LockLandscape;
            this.LiveTilesButton.IsChecked = App.ViewModel.LiveTilesEnabled;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            App.ViewModel.SaveSettings();
            base.OnNavigatedFrom(e);
        }

        private void AutoJoinChatButton_Checked(object sender, RoutedEventArgs e)
        {
            App.ViewModel.AutoJoinChat = true;
        }

        private void AutoJoinChatButton_Unchecked(object sender, RoutedEventArgs e)
        {
            App.ViewModel.AutoJoinChat = false;
        }        

        private void LockLandscapeButton_Checked(object sender, RoutedEventArgs e)
        {
            App.ViewModel.LockLandscape = true;
        }

        private void LockLandscapeButton_Unchecked(object sender, RoutedEventArgs e)
        {
            App.ViewModel.LockLandscape = false;
        }

        private void LiveTilesButton_Checked(object sender, RoutedEventArgs e)
        {
            if (App.ViewModel.user == null)
            {
                MessageBox.Show("Must be logged in to show followed streams on live tile");
                this.LiveTilesButton.IsChecked = false;
            }

            else
            {
                App.ViewModel.LiveTilesEnabled = true;
                LiveTileHelper.UpdateLiveTile(App.ViewModel.user.Oauth);

                try
                {
                    if (ScheduledActionService.Find(liveTileTaskName) != null)
                    {
                        //if the agent exists, remove and then add it to ensure
                        //the agent's schedule is updated to avoid expiration
                        ScheduledActionService.Remove(liveTileTaskName);
                    }

                    PeriodicTask periodicTask = new PeriodicTask(liveTileTaskName);
                    periodicTask.Description = App.ViewModel.user.Oauth;
                    ScheduledActionService.Add(periodicTask);
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                }
            }
        }

        private void LiveTilesButton_Unchecked(object sender, RoutedEventArgs e)
        {
            App.ViewModel.LiveTilesEnabled = false;

            try
            {
                if (ScheduledActionService.Find(liveTileTaskName) != null)
                {
                    //if the agent exists, remove and then add it to ensure
                    //the agent's schedule is updated to avoid expiration
                    ScheduledActionService.Remove(liveTileTaskName);
                }

                PeriodicTask periodicTask = new PeriodicTask(liveTileTaskName);
                periodicTask.Description = "No OAuth to use";
                ScheduledActionService.Add(periodicTask);
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }
    }
}