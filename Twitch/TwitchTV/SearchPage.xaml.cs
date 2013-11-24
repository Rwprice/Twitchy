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

namespace TwitchTV
{
    public partial class SearchPage : PhoneApplicationPage
    {
        public SearchPage()
        {
            InitializeComponent();
            App.ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "SearchStreams")
            {
                this.StreamsList.ItemsSource = App.ViewModel.SearchStreams;
            }

            if (e.PropertyName == "SearchGames")
            {
                this.GamesList.ItemsSource = App.ViewModel.SearchGames;
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.StreamsList.SelectedItem = null;
            this.GamesList.SelectedItem = null;
            this.StreamsSearchBox.Text = "Search...";
            this.GamesSearchBox.Text = "Search...";
        }

        private async void StreamSearchButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (this.StreamsSearchBox.Text != "Search...")
                {
                    App.ViewModel.SearchStreams = null;
                    App.ViewModel.SearchStreams = await Stream.SearchStreams(this.StreamsSearchBox.Text);
                }
            }

            catch (Exception ex)
            {
                MessageBox.Show("Something went wrong while searching", "Well, this is embarrassing...", MessageBoxButton.OK);
                Debug.WriteLine(ex.Message);
            }
        }

        private async void GameSearchButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (this.GamesSearchBox.Text != "Search...")
                {
                    App.ViewModel.SearchGames = null;
                    App.ViewModel.SearchGames = await TopGame.SearchGames(this.GamesSearchBox.Text);
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
            if (((Stream)((ListBox)sender).SelectedItem) != null)
            {
                App.ViewModel.stream = ((Stream)((ListBox)sender).SelectedItem);
                NavigationService.Navigate(new Uri("/PlayerPage.xaml", UriKind.RelativeOrAbsolute));
            }
        }

        private void GamesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (((TopGame)((ListBox)sender).SelectedItem) != null)
            {
                App.ViewModel.curTopGame = ((TopGame)((ListBox)sender).SelectedItem);
                NavigationService.Navigate(new Uri("/TopGamePage.xaml", UriKind.RelativeOrAbsolute));
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
    }
}