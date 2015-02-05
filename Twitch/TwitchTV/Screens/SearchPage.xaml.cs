using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using TwitchAPIHandler.Objects;
using Twitchy.ViewModels;
using System.Windows.Data;
using LiveTileTaskAgent;

namespace TwitchTV
{
    public partial class SearchPage : PhoneApplicationPage
    {
        private int _pageNumberGames = 0;
        private int _offsetKnobGames = 1;
        private int _pageNumberStreams = 0;
        private int _offsetKnobStreams = 1;
        SearchViewModel _viewModel;

        public SearchPage()
        {
            InitializeComponent();
            _viewModel = new SearchViewModel();
            GamesList.ItemRealized += gamesList_ItemRealized;
            StreamsList.ItemRealized += streamsList_ItemRealized;
            this.Loaded += new RoutedEventHandler(SearchPage_Loaded);
        }

        private void streamsList_ItemRealized(object sender, ItemRealizationEventArgs e)
        {
            if (!_viewModel.IsLoading && StreamsList.ItemsSource != null && StreamsList.ItemsSource.Count >= _offsetKnobStreams)
            {
                if (e.ItemKind == LongListSelectorItemKind.Item)
                {
                    if ((e.Container.Content as Stream).Equals(StreamsList.ItemsSource[StreamsList.ItemsSource.Count - _offsetKnobStreams]))
                    {
                        Debug.WriteLine("Searching for {0}", _pageNumberStreams);
                        _viewModel.SearchStreams(StreamsSearchBox.Text, _pageNumberStreams++);
                    }
                }
            }
        }

        private void SearchPage_Loaded(object sender, RoutedEventArgs e)
        {
            this.GamesList.ItemsSource = _viewModel.GameList;
            this.StreamsList.ItemsSource = _viewModel.StreamList;

            this.StreamsList.SelectedItem = null;
            this.GamesList.SelectedItem = null;

            this.StreamsSearchBox.Text = "Search...";
            this.GamesSearchBox.Text = "Search...";

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
        }

        private void gamesList_ItemRealized(object sender, ItemRealizationEventArgs e)
        {
            if (!_viewModel.IsLoading && GamesList.ItemsSource != null && GamesList.ItemsSource.Count >= _offsetKnobGames)
            {
                if (e.ItemKind == LongListSelectorItemKind.Item)
                {
                    if ((e.Container.Content as Game).Equals(GamesList.ItemsSource[GamesList.ItemsSource.Count - _offsetKnobGames]))
                    {
                        if (GamesList.ItemsSource.Count % 8 == 0)
                        {
                            Debug.WriteLine("Searching for {0}", _pageNumberGames);
                            _viewModel.SearchGames(GamesSearchBox.Text, _pageNumberGames++);
                        }
                    }
                }
            }
        }

        private void StreamSearchButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (this.StreamsSearchBox.Text != "Search...")
                {
                    _pageNumberStreams = 0;
                    _viewModel.SearchStreams(this.StreamsSearchBox.Text, _pageNumberStreams++);
                }
            }

            catch (Exception ex)
            {
                MessageBox.Show("Something went wrong while searching", "Well, this is embarrassing...", MessageBoxButton.OK);
                Debug.WriteLine(ex.Message);
            }
        }

        private void GameSearchButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (this.GamesSearchBox.Text != "Search...")
                {
                    _pageNumberGames = 0;
                    _viewModel.SearchGames(this.GamesSearchBox.Text, _pageNumberGames++);
                }
            }

            catch (Exception ex)
            {
                MessageBox.Show("Something went wrong while searching", "Well, this is embarrassing...", MessageBoxButton.OK);
                Debug.WriteLine(ex.Message);
            }
        }

        private void StreamsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (((Stream)((LongListSelector)sender).SelectedItem) != null)
            {
                App.ViewModel.stream = ((Stream)((LongListSelector)sender).SelectedItem);
                ((LongListSelector)sender).SelectedItem = null;
                NavigationService.Navigate(new Uri("/Screens/PlayerPage.xaml", UriKind.RelativeOrAbsolute));
            }
        }

        private void GamesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (((Game)((LongListSelector)sender).SelectedItem) != null)
            {
                var selected = ((Game)((LongListSelector)sender).SelectedItem);
                var topGameToSave = new TopGame()
                {
                    game = selected
                };

                App.ViewModel.curTopGame = topGameToSave;
                ((LongListSelector)sender).SelectedItem = null;
                NavigationService.Navigate(new Uri("/Screens/TopGamePage.xaml", UriKind.RelativeOrAbsolute));
            }
        }

        private void GamesSearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            this.GamesSearchBox.Text = "";
        }

        private void StreamsSearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            this.StreamsSearchBox.Text = "";
        }

        private void StreamsSearchBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                StreamSearchButton_Click(null, null);
                this.Focus();
            }
        }

        private void GamesSearchBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                GameSearchButton_Click(null, null);
                this.Focus();
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