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

        /// <summary>
        /// Featured Streams
        /// </summary>
        public List<Stream> FeaturedStreams { get; set; }

        /// <summary>
        /// Top Games
        /// </summary>
        public List<TopGame> TopGames { get; set; }

        /// <summary>
        /// Top Streams
        /// </summary>
        public List<Stream> TopStreams { get; set; }

        // Constructor
        public MainPage()
        {
            isNetwork = NetworkInterface.GetIsNetworkAvailable();

            if (!isNetwork)
            {
                MessageBox.Show("You are not connected to a network. Twitch is unavailable");
            }

            this.TopGames = new List<TopGame>();
            this.FeaturedStreams = new List<Stream>();
            this.TopStreams = new List<Stream>();

            InitializeComponent();

            // Set the data context of the listbox control to the sample data
            DataContext = App.ViewModel;
        }

        // Load data for the ViewModel Items
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            if (isNetwork)
            {
                this.FeaturedStreams = await Stream.GetFeaturedStreams();
                this.TopGames = await TopGame.GetTopGames();
                this.TopStreams = await Stream.GetTopStreams();

                #region Set Featured Streams
                this.FP0Image.Source = new BitmapImage(this.FeaturedStreams[0].preview.medium);
                this.FP0Text.Text = this.FeaturedStreams[0].channel.display_name;
                this.FP1Image.Source = new BitmapImage(this.FeaturedStreams[1].preview.medium);
                this.FP1Text.Text = this.FeaturedStreams[1].channel.display_name;
                this.FP2Image.Source = new BitmapImage(this.FeaturedStreams[2].preview.medium);
                this.FP2Text.Text = this.FeaturedStreams[2].channel.display_name;
                this.FP3Image.Source = new BitmapImage(this.FeaturedStreams[3].preview.medium);
                this.FP3Text.Text = this.FeaturedStreams[3].channel.display_name;
                this.FP4Image.Source = new BitmapImage(this.FeaturedStreams[4].preview.medium);
                this.FP4Text.Text = this.FeaturedStreams[4].channel.display_name;
                this.FP5Image.Source = new BitmapImage(this.FeaturedStreams[5].preview.medium);
                this.FP5Text.Text = this.FeaturedStreams[5].channel.display_name;
                this.FP6Image.Source = new BitmapImage(this.FeaturedStreams[6].preview.medium);
                this.FP6Text.Text = this.FeaturedStreams[6].channel.display_name;
                this.FP7Image.Source = new BitmapImage(this.FeaturedStreams[7].preview.medium);
                this.FP7Text.Text = this.FeaturedStreams[7].channel.display_name;
                #endregion

                #region Set Top Games
                this.TG0Image.Source = new BitmapImage(this.TopGames[0].game.box.medium);
                this.TG0Text.Text = this.TopGames[0].game.name + "\nChannels: " + this.TopGames[0].channels;
                this.TG1Image.Source = new BitmapImage(this.TopGames[1].game.box.medium);
                this.TG1Text.Text = this.TopGames[1].game.name + "\nChannels: " + this.TopGames[1].channels;
                this.TG2Image.Source = new BitmapImage(this.TopGames[2].game.box.medium);
                this.TG2Text.Text = this.TopGames[2].game.name + "\nChannels: " + this.TopGames[2].channels;
                this.TG3Image.Source = new BitmapImage(this.TopGames[3].game.box.medium);
                this.TG3Text.Text = this.TopGames[3].game.name + "\nChannels: " + this.TopGames[3].channels;
                this.TG4Image.Source = new BitmapImage(this.TopGames[4].game.box.medium);
                this.TG4Text.Text = this.TopGames[4].game.name + "\nChannels: " + this.TopGames[4].channels;
                this.TG5Image.Source = new BitmapImage(this.TopGames[5].game.box.medium);
                this.TG5Text.Text = this.TopGames[5].game.name + "\nChannels: " + this.TopGames[5].channels;
                this.TG6Image.Source = new BitmapImage(this.TopGames[6].game.box.medium);
                this.TG6Text.Text = this.TopGames[6].game.name + "\nChannels: " + this.TopGames[6].channels;
                this.TG7Image.Source = new BitmapImage(this.TopGames[7].game.box.medium);
                this.TG7Text.Text = this.TopGames[7].game.name + "\nChannels: " + this.TopGames[7].channels;
                #endregion

                #region Set Top Stream
                this.TS0Image.Source = new BitmapImage(this.TopStreams[0].preview.small);
                this.TS0Text.Text = this.TopStreams[0].channel.display_name + "\nViewers: " + this.TopStreams[0].viewers;
                this.TS1Image.Source = new BitmapImage(this.TopStreams[1].preview.small);
                this.TS1Text.Text = this.TopStreams[1].channel.display_name + "\nViewers: " + this.TopStreams[1].viewers;
                this.TS2Image.Source = new BitmapImage(this.TopStreams[2].preview.small);
                this.TS2Text.Text = this.TopStreams[2].channel.display_name + "\nViewers: " + this.TopStreams[2].viewers;
                this.TS3Image.Source = new BitmapImage(this.TopStreams[3].preview.small);
                this.TS3Text.Text = this.TopStreams[3].channel.display_name + "\nViewers: " + this.TopStreams[3].viewers;
                this.TS4Image.Source = new BitmapImage(this.TopStreams[4].preview.small);
                this.TS4Text.Text = this.TopStreams[4].channel.display_name + "\nViewers: " + this.TopStreams[4].viewers;
                this.TS5Image.Source = new BitmapImage(this.TopStreams[5].preview.small);
                this.TS5Text.Text = this.TopStreams[5].channel.display_name + "\nViewers: " + this.TopStreams[5].viewers;
                this.TS6Image.Source = new BitmapImage(this.TopStreams[6].preview.small);
                this.TS6Text.Text = this.TopStreams[6].channel.display_name + "\nViewers: " + this.TopStreams[6].viewers;
                this.TS7Image.Source = new BitmapImage(this.TopStreams[7].preview.small);
                this.TS7Text.Text = this.TopStreams[7].channel.display_name + "\nViewers: " + this.TopStreams[7].viewers;
                #endregion
            }
        }

        private void FrontPageIconTapped(object sender, System.Windows.Input.GestureEventArgs e)
        {
            int index = int.Parse(((Canvas)sender).Name.Remove(0, 2));
            App.ViewModel.channel = this.FeaturedStreams[index].channel.name;
            NavigationService.Navigate(new Uri("/PlayerPage.xaml", UriKind.RelativeOrAbsolute));
        }

        private void TopStreamTapped(object sender, System.Windows.Input.GestureEventArgs e)
        {
            int index = int.Parse(((StackPanel)sender).Name.Remove(0, 2));
            App.ViewModel.channel = this.TopStreams[index].channel.name;
            NavigationService.Navigate(new Uri("/PlayerPage.xaml", UriKind.RelativeOrAbsolute));
        }

        private void TopGameTapped(object sender, System.Windows.Input.GestureEventArgs e)
        {
            int index = int.Parse(((StackPanel)sender).Name.Remove(0, 2));
            App.ViewModel.curTopGame = this.TopGames[index];
            NavigationService.Navigate(new Uri("/TopGamePage.xaml", UriKind.RelativeOrAbsolute));
        }

        private void SettingTapped(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Debug.WriteLine(((TextBlock)sender).Text);
        }
    }
}