using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using TwitchTV.ViewModels;
using System.Windows.Data;
using System.Diagnostics;

namespace TwitchTV
{
    public partial class NotificationsPage : PhoneApplicationPage
    {
        private int _pageNumber = 0;
        private int _offsetKnob = 1;
        NotificationsViewModel _viewModel;

        List<TwitchAPIHandler.Objects.Notification> channelsToSave = new List<TwitchAPIHandler.Objects.Notification>();

        public NotificationsPage()
        {
            InitializeComponent();
            _viewModel = new NotificationsViewModel();
            NotificationsList.ItemRealized += notificationsList_ItemRealized;
            this.Loaded += new RoutedEventHandler(NotificationPage_Loaded);
        }

        private async void NotificationPage_Loaded(object sender, RoutedEventArgs e)
        {
            NotificationsList.ItemsSource = _viewModel.NotificationsList;
            var progressIndicator = SystemTray.ProgressIndicator;
            if (progressIndicator != null)
            {
                return;
            }

            progressIndicator = new ProgressIndicator();

            SystemTray.SetProgressIndicator(this, progressIndicator);

            Binding binding = new Binding("IsLoading") { Source = _viewModel };
            BindingOperations.SetBinding(
                progressIndicator, ProgressIndicator.IsVisibleProperty, binding);

            binding = new Binding("IsLoading") { Source = _viewModel };
            BindingOperations.SetBinding(
                progressIndicator, ProgressIndicator.IsIndeterminateProperty, binding);

            progressIndicator.Text = "Loading";

            _pageNumber = 0;

            //Load shit
            channelsToSave = await App.ViewModel.LoadNotificationsList() ?? new List<TwitchAPIHandler.Objects.Notification>();
            foreach (var notif in channelsToSave)
            {
                _viewModel.NotificationsList.Add(notif);
            }

            if (App.ViewModel.user != null)
            {
                _viewModel.LoadPage(App.ViewModel.user.Name, _pageNumber++);
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            _viewModel.ClearList();
            App.ViewModel.SaveNotificationsList(channelsToSave);
            base.OnNavigatedFrom(e);
        }

        private void notificationsList_ItemRealized(object sender, ItemRealizationEventArgs e)
        {
            if (App.ViewModel.user != null)
            {
                if (!_viewModel.IsLoading && NotificationsList.ItemsSource != null && NotificationsList.ItemsSource.Count >= _offsetKnob)
                {
                    if (e.ItemKind == LongListSelectorItemKind.Item)
                    {
                        if ((e.Container.Content as TwitchAPIHandler.Objects.Notification).Equals(NotificationsList.ItemsSource[NotificationsList.ItemsSource.Count - _offsetKnob]))
                        {
                            Debug.WriteLine("Searching for Notification Page {0}", _pageNumber);
                            _viewModel.LoadPage(App.ViewModel.user.Name, _pageNumber++);
                        }
                    }
                }
            }
        }

        private void ToggleSwitch_Checked(object sender, RoutedEventArgs e)
        {
            var channel = (TwitchAPIHandler.Objects.Notification)(sender as ToggleSwitch).DataContext;

            if(!channelsToSave.Contains(channel))
                channelsToSave.Add(channel);
        }

        private void ToggleSwitch_Unchecked(object sender, RoutedEventArgs e)
        {
            var channel = (TwitchAPIHandler.Objects.Notification)(sender as ToggleSwitch).DataContext;

            if (channelsToSave.Contains(channel))
                channelsToSave.Remove(channel);
        }
    }
}