using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Net.NetworkInformation;
using TwitchAPIHandler.Objects;
using Windows.Storage;
using Windows.Storage.Streams;

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

            if (e.PropertyName == "FollowedStreams")
            {
                this.FollowedStreamsList.ItemsSource = App.ViewModel.FollowedStreams;
            }

            if (e.PropertyName == "TopGames")
            {
                this.TopGamesList.ItemsSource = App.ViewModel.TopGames;
            }

            if (e.PropertyName == "FeaturedStreams")
            {
                Image image;
                TextBlock text;
                for (int i = 0; i < App.ViewModel.FeaturedStreams.Count; i++)
                {
                    image = (Image)this.FeaturedStreams.FindName("FP" + i + "Image");
                    image.Source = App.ViewModel.FeaturedStreams[i].preview.medium;
                    text = (TextBlock)this.FeaturedStreams.FindName("FP" + i + "Text");
                    text.Text = App.ViewModel.FeaturedStreams[i].channel.display_name;
                }
            }
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            try
            {
                this.TopStreamsList.SelectedItem = null;
                this.TopGamesList.SelectedItem = null;
                this.FollowedStreamsList.SelectedItem = null;

                if (App.ViewModel.user == null)
                {
                    var user = await User.TryLoadUser();
                    if (user != null)
                    {
                        App.ViewModel.user = user;
                        this.Account.Text = "Logout";
                    }
                }

                else
                    this.Account.Text = "Logout";

                if (isNetwork)
                {
                    App.ViewModel.LoadData();
                }
            }

            catch (Exception ex)
            {
                MessageBox.Show("Can't load front page data. Try again later", "Well, this is embarrassing...", MessageBoxButton.OK);
                Debug.WriteLine(ex.Message);
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
            if((((TextBlock)sender).Text) != "Logout")
                NavigationService.Navigate(new Uri("/" + (((TextBlock)sender).Text) + "Page.xaml", UriKind.RelativeOrAbsolute));
            else if ((((TextBlock)sender).Text) == "Logout")
            {
                User.LogoutUser();

                App.ViewModel.user = null;

                MessageBox.Show("User has been logged out!");

                if (this.FollowedStreamsList.Items.Count > 0)
                    App.ViewModel.FollowedStreams.Clear();

                this.Account.Text = "Login";
            }
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

        private void FollowedStreamsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (((Stream)((ListBox)sender).SelectedItem) != null)
            {
                App.ViewModel.stream = ((Stream)((ListBox)sender).SelectedItem);
                NavigationService.Navigate(new Uri("/PlayerPage.xaml", UriKind.RelativeOrAbsolute));
            }
        }

        private void SearchButton_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/SearchPage.xaml", UriKind.RelativeOrAbsolute));
        }

        private void RefreshButton_Click(object sender, EventArgs e)
        {
            App.ViewModel.lastUpdate = DateTime.MinValue;
            App.ViewModel.LoadData();
        }
    }
}