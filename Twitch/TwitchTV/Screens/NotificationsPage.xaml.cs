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

        private void NotificationPage_Loaded(object sender, RoutedEventArgs e)
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

            if (App.ViewModel.user != null)
            {
                _viewModel.LoadPage(App.ViewModel.user.Oauth, _pageNumber++);
            }
        }

        private void notificationsList_ItemRealized(object sender, ItemRealizationEventArgs e)
        {
            if (App.ViewModel.user != null)
            {
                if (!_viewModel.IsLoading && NotificationsList.ItemsSource != null && NotificationsList.ItemsSource.Count >= _offsetKnob)
                {
                    if (e.ItemKind == LongListSelectorItemKind.Item)
                    {
                        if ((e.Container.Content as TwitchAPIHandler.Objects.Channel).Equals(NotificationsList.ItemsSource[NotificationsList.ItemsSource.Count - _offsetKnob]))
                        {
                            Debug.WriteLine("Searching for Followed Page {0}", _pageNumber);
                            _viewModel.LoadPage(App.ViewModel.user.Oauth, _pageNumber++);
                        }
                    }
                }
            }
        }

        private void ToggleSwitch_Checked(object sender, RoutedEventArgs e)
        {
            TwitchAPIHandler.Objects.Channel channel = (TwitchAPIHandler.Objects.Channel)(sender as ToggleSwitch).DataContext;
        }

        private void ToggleSwitch_Unchecked(object sender, RoutedEventArgs e)
        {
            TwitchAPIHandler.Objects.Channel channel = (TwitchAPIHandler.Objects.Channel)(sender as ToggleSwitch).DataContext;
        }
    }
}