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

namespace TwitchTV
{
    public partial class SettingsPage : PhoneApplicationPage
    {
        public SettingsPage()
        {
            InitializeComponent();
            this.AutoJoinChatButton.IsChecked = App.ViewModel.AutoJoinChat;
            this.LockLandscapeButton.IsChecked = App.ViewModel.LockLandscape;
        }

        private void AutoJoinChatButton_Checked(object sender, RoutedEventArgs e)
        {
            App.ViewModel.AutoJoinChat = true;
        }

        private void AutoJoinChatButton_Unchecked(object sender, RoutedEventArgs e)
        {
            App.ViewModel.AutoJoinChat = false;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            App.ViewModel.SaveSettings();
            base.OnNavigatedFrom(e);
        }

        private void LockLandscapeButton_Checked(object sender, RoutedEventArgs e)
        {
            App.ViewModel.LockLandscape = true;
        }

        private void LockLandscapeButton_Unchecked(object sender, RoutedEventArgs e)
        {
            App.ViewModel.LockLandscape = false;
        }
    }
}