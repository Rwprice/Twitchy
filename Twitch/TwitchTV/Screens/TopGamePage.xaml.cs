using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using TwitchAPIHandler.Objects;
using System.Windows.Media.Imaging;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Data;
using Twitchy.ViewModels;
using LiveTileTaskAgent;

namespace TwitchTV
{
    public partial class TopGamePage : PhoneApplicationPage
    {
        private int _pageNumber = 0;
        private int _offsetKnob = 1;
        TopGameStreamsViewModel _viewModel;  

        public TopGamePage()
        {
            InitializeComponent();
            _viewModel = new TopGameStreamsViewModel();
            this.TGHeader.Header = App.ViewModel.curTopGame.game.name;
            TopStreamsList.ItemRealized += resultList_ItemRealized;
            this.Loaded += new RoutedEventHandler(TopGamePage_Loaded);
        }

        private void TopGamePage_Loaded(object sender, RoutedEventArgs e)
        {
            this.TopStreamsList.ItemsSource = _viewModel.StreamList;
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

            progressIndicator.Text = "Loading streams...";

            _pageNumber = 0;

            _viewModel.LoadPage(App.ViewModel.curTopGame.game.name, _pageNumber++);
        }

        private void resultList_ItemRealized(object sender, ItemRealizationEventArgs e)
        {
            if (!_viewModel.IsLoading && TopStreamsList.ItemsSource != null && TopStreamsList.ItemsSource.Count >= _offsetKnob)
            {
                if (e.ItemKind == LongListSelectorItemKind.Item)
                {
                    if ((e.Container.Content as Stream).Equals(TopStreamsList.ItemsSource[TopStreamsList.ItemsSource.Count - _offsetKnob]))
                    {
                        Debug.WriteLine("Searching for {0}", _pageNumber);
                        _viewModel.LoadPage(App.ViewModel.curTopGame.game.name, _pageNumber++);
                    }
                }
            }
        }

        private void TopStreamsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (((Stream)((LongListSelector)sender).SelectedItem) != null)
            {
                App.ViewModel.stream = ((Stream)((LongListSelector)sender).SelectedItem);
                ((LongListSelector)sender).SelectedItem = null;
                NavigationService.Navigate(new Uri("/Screens/PlayerPage.xaml", UriKind.RelativeOrAbsolute));
            }
        }

        #region Context Menu

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            Stream stream = (Stream)(sender as MenuItem).DataContext;
            App.ViewModel.stream = stream;
            NavigationService.Navigate(new Uri("/Screens/PlayerPage.xaml", UriKind.RelativeOrAbsolute));
        }

        private async void Follow_Click(object sender, RoutedEventArgs e)
        {
            Stream stream = (Stream)(sender as MenuItem).DataContext;

            if (((string)((MenuItem)(sender)).Header) == "Unfollow")
            {
                await User.UnfollowStream(stream.channel.name, App.ViewModel.user);
                ((MenuItem)(sender)).Header = "Follow";
            }

            else
            {
                await User.FollowStream(stream.channel.name, App.ViewModel.user);
                ((MenuItem)(sender)).Header = "Unfollow";
            }
        }

        private async void Follow_Loaded(object sender, RoutedEventArgs e)
        {
            if (App.ViewModel.user != null)
            {
                var menuItem = sender as MenuItem;
                var contextMenu = menuItem.Parent as ContextMenu;
                Stream stream = (Stream)contextMenu.DataContext;
                bool isFollowedTask = await User.IsStreamFollowed(stream.channel.name, App.ViewModel.user);

                menuItem.IsEnabled = true;

                if (isFollowedTask)
                    menuItem.Header = "Unfollow";
            }
        }

        private void ContextMenu_Unloaded(object sender, RoutedEventArgs e)
        {
            ContextMenu conmen = (sender as ContextMenu);
            conmen.ClearValue(FrameworkElement.DataContextProperty);
        }

        #region Live Tiles
        private void Pin_to_Start_Click(object sender, RoutedEventArgs e)
        {
            Stream stream = (Stream)(sender as MenuItem).DataContext;
            ShellTile tile;

            if ((sender as MenuItem).Header.ToString() == "Pin to Start")
            {
                tile = LiveTileHelper.FindTile(stream.channel.name);

                if (tile == null)
                {
                    Uri uri;
                    if (stream.channel.logoUri != "")
                        uri = new Uri(stream.channel.logoUri);
                    else
                        uri = new Uri("/Assets/noProfPic.png", UriKind.Relative);

                    StandardTileData tileData = new StandardTileData
                    {
                        BackgroundImage = uri,
                        Title = stream.channel.display_name
                    };

                    LiveTileHelper.SaveTileImages(stream.channel.name, uri);
                    string tileUri = string.Concat("/Screens/PlayerPage.xaml?", stream.channel.name);
                    ShellTile.Create(new Uri(tileUri, UriKind.Relative), tileData);
                }
            }

            else
            {
                tile = LiveTileHelper.FindTile(stream.channel.name);
                if (tile != null)
                {
                    tile.Delete();
                    LiveTileHelper.DeleteImage(stream.channel.name);
                }
            }
        }

        private void Pin_to_Start_Loaded(object sender, RoutedEventArgs e)
        {
            Stream stream = (Stream)(sender as MenuItem).DataContext;
            ShellTile tile = LiveTileHelper.FindTile(stream.channel.name);

            if (tile == null)
                (sender as MenuItem).Header = "Pin to Start";
            else
                (sender as MenuItem).Header = "Unpin from Start";
        }
        #endregion
        #endregion
    }
}