using System;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using TwitchAPIHandler.Objects;
using System.Diagnostics;
using System.Windows.Media.Imaging;
using Microsoft.Phone.Net.NetworkInformation;

namespace TwitchTV
{
    public partial class MainPage : PhoneApplicationPage
    {
        public bool isNetwork { get; set; }

        public MainPage()
        {
            isNetwork = NetworkInterface.GetIsNetworkAvailable();

            if (!isNetwork)
            {
                MessageBox.Show("You are not connected to a network. Twitchy is unavailable");
            }

            InitializeComponent();

            App.ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "TopStreams")
            {
                this.TopStreamsList.ItemsSource = App.ViewModel.TopStreams;
            }

            if (e.PropertyName == "TopGames")
            {
                this.TopGamesList.ItemsSource = App.ViewModel.TopGames;
            }

            if (e.PropertyName == "FeaturedStreams")
            {
                Image image;
                TextBlock text;
                for (int i = 0; i < 8; i++)
                {
                    image = (Image)this.FeaturedStreams.FindName("FP" + i + "Image");
                    image.Source = App.ViewModel.FeaturedStreams[i].preview.medium;
                    text = (TextBlock)this.FeaturedStreams.FindName("FP" + i + "Text");
                    text.Text = App.ViewModel.FeaturedStreams[i].channel.display_name;
                }
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.TopStreamsList.SelectedItem = null;
            this.TopGamesList.SelectedItem = null;

            if (isNetwork)
            {
                if (!App.ViewModel.IsDataLoaded)
                {
                    App.ViewModel.LoadData();
                }
            }
        }

        private void FrontPageIconTapped(object sender, System.Windows.Input.GestureEventArgs e)
        {
            int index = int.Parse(((Canvas)sender).Name.Remove(0, 2));
            App.ViewModel.stream = App.ViewModel.FeaturedStreams[index];
            NavigationService.Navigate(new Uri("/PlayerPage.xaml", UriKind.RelativeOrAbsolute));
        }

        private void SettingTapped(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Debug.WriteLine(((TextBlock)sender).Text);
        }

        private void TopStreamsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (((Stream)((ListBox)sender).SelectedItem) != null)
            {
                App.ViewModel.stream = ((Stream)((ListBox)sender).SelectedItem);
                NavigationService.Navigate(new Uri("/PlayerPage.xaml", UriKind.RelativeOrAbsolute));
            }
        }

        private void TopGamesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (((TopGame)((ListBox)sender).SelectedItem) != null)
            {
                App.ViewModel.curTopGame = ((TopGame)((ListBox)sender).SelectedItem);
                NavigationService.Navigate(new Uri("/TopGamePage.xaml", UriKind.RelativeOrAbsolute));
            }
        }

        private void SearchButton_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/SearchPage.xaml", UriKind.RelativeOrAbsolute));
        }
    }
}