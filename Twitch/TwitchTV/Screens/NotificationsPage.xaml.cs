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

            foreach (var notif in await App.ViewModel.LoadNotificationsList() ?? new List<TwitchAPIHandler.Objects.Notification>())
            {
                _viewModel.NotificationsList.Add(notif);
            }

            if (App.ViewModel.user != null)
            {
                _viewModel.LoadPage(App.ViewModel.user.Name, _pageNumber++);
            }

            else
            {
                MessageBox.Show("Must be logged in to use notifications!");
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            var channelsToSave = (from notification in _viewModel.NotificationsList
                                 where notification.notify
                                 select notification).ToList();

            _viewModel.ClearList();
            App.ViewModel.SaveNotificationsList(channelsToSave);
            base.OnNavigatedFrom(e);
        }

        private void notificationsList_ItemRealized(object sender, ItemRealizationEventArgs e)
        {
            if (App.ViewModel.user != null)
            {
                var channel = (e.Container.Content as TwitchAPIHandler.Objects.Notification);
                if (channel.notify)
                {
                    channel.notify = false;
                    NotificationsList.SelectedItems.Add(channel);
                }

                if (!_viewModel.IsLoading && NotificationsList.ItemsSource != null && NotificationsList.ItemsSource.Count >= _offsetKnob)
                {
                    if (e.ItemKind == LongListSelectorItemKind.Item)
                    {
                        if (channel.Equals(NotificationsList.ItemsSource[NotificationsList.ItemsSource.Count - _offsetKnob]))
                        {
                            Debug.WriteLine("Searching for Notification Page {0}", _pageNumber);
                            _viewModel.LoadPage(App.ViewModel.user.Name, _pageNumber++);
                        }
                    }
                }
            }
        }

        private void NotificationsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach(TwitchAPIHandler.Objects.Notification notification in e.AddedItems)
                if (!notification.notify)
                    notification.notify = true;

            foreach (TwitchAPIHandler.Objects.Notification notification in e.RemovedItems)
                if (notification.notify)
                    notification.notify = false;
        }
    }
}