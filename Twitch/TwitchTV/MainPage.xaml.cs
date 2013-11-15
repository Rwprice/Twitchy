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

namespace TwitchTV
{
    public partial class MainPage : PhoneApplicationPage
    {
        /// <summary>
        /// Featured Streams
        /// </summary>
        public List<Stream> FeaturedStreams { get; set; }

        /// <summary>
        /// Top Games
        /// </summary>
        public List<TopGame> TopGames { get; set; }

        // Constructor
        public MainPage()
        {
            this.TopGames = new List<TopGame>();
            this.FeaturedStreams = new List<Stream>();

            InitializeComponent();

            // Set the data context of the listbox control to the sample data
            DataContext = App.ViewModel;
        }

        // Load data for the ViewModel Items
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            App.ViewModel.FeaturedStreams = await Stream.GetFeaturedStreams();
            App.ViewModel.TopGames = await TopGame.GetTopGames();

            #region Set Featured Streams
            this.FP1Image.Source = new BitmapImage(App.ViewModel.FeaturedStreams[0].preview.medium);
            this.FP1Text.Text = App.ViewModel.FeaturedStreams[0].channel.display_name;
            this.FP2Image.Source = new BitmapImage(App.ViewModel.FeaturedStreams[1].preview.medium);
            this.FP2Text.Text = App.ViewModel.FeaturedStreams[1].channel.display_name;
            this.FP3Image.Source = new BitmapImage(App.ViewModel.FeaturedStreams[2].preview.medium);
            this.FP3Text.Text = App.ViewModel.FeaturedStreams[2].channel.display_name;
            this.FP4Image.Source = new BitmapImage(App.ViewModel.FeaturedStreams[3].preview.medium);
            this.FP4Text.Text = App.ViewModel.FeaturedStreams[3].channel.display_name;
            this.FP5Image.Source = new BitmapImage(App.ViewModel.FeaturedStreams[4].preview.medium);
            this.FP5Text.Text = App.ViewModel.FeaturedStreams[4].channel.display_name;
            this.FP6Image.Source = new BitmapImage(App.ViewModel.FeaturedStreams[5].preview.medium);
            this.FP6Text.Text = App.ViewModel.FeaturedStreams[5].channel.display_name;
            this.FP7Image.Source = new BitmapImage(App.ViewModel.FeaturedStreams[6].preview.medium);
            this.FP7Text.Text = App.ViewModel.FeaturedStreams[6].channel.display_name;
            this.FP8Image.Source = new BitmapImage(App.ViewModel.FeaturedStreams[7].preview.medium);
            this.FP8Text.Text = App.ViewModel.FeaturedStreams[7].channel.display_name;
            #endregion

            #region Set Top Games
            this.TG0Image.Source = new BitmapImage(App.ViewModel.TopGames[0].game.box.medium);
            this.TG0Text.Text = App.ViewModel.TopGames[0].game.name + "\nChannels: " + App.ViewModel.TopGames[0].channels;
            this.TG1Image.Source = new BitmapImage(App.ViewModel.TopGames[1].game.box.medium);
            this.TG1Text.Text = App.ViewModel.TopGames[1].game.name + "\nChannels: " + App.ViewModel.TopGames[1].channels;
            this.TG2Image.Source = new BitmapImage(App.ViewModel.TopGames[2].game.box.medium);
            this.TG2Text.Text = App.ViewModel.TopGames[2].game.name + "\nChannels: " + App.ViewModel.TopGames[2].channels;
            this.TG3Image.Source = new BitmapImage(App.ViewModel.TopGames[3].game.box.medium);
            this.TG3Text.Text = App.ViewModel.TopGames[3].game.name + "\nChannels: " + App.ViewModel.TopGames[3].channels;
            this.TG4Image.Source = new BitmapImage(App.ViewModel.TopGames[4].game.box.medium);
            this.TG4Text.Text = App.ViewModel.TopGames[4].game.name + "\nChannels: " + App.ViewModel.TopGames[4].channels;
            this.TG5Image.Source = new BitmapImage(App.ViewModel.TopGames[5].game.box.medium);
            this.TG5Text.Text = App.ViewModel.TopGames[5].game.name + "\nChannels: " + App.ViewModel.TopGames[5].channels;
            this.TG6Image.Source = new BitmapImage(App.ViewModel.TopGames[6].game.box.medium);
            this.TG6Text.Text = App.ViewModel.TopGames[6].game.name + "\nChannels: " + App.ViewModel.TopGames[6].channels;
            this.TG7Image.Source = new BitmapImage(App.ViewModel.TopGames[7].game.box.medium);
            this.TG7Text.Text = App.ViewModel.TopGames[7].game.name + "\nChannels: " + App.ViewModel.TopGames[7].channels;
            #endregion

            #region Set Top Stream
            this.TS0Text.Text = "Some Top Stream\nViewers: 102924";
            #endregion
        }

        private async void FrontPageIconTapped(object sender, System.Windows.Input.GestureEventArgs e)
        {
            int index = int.Parse(((Canvas)sender).Name.Remove(0, 2)) - 1;
            string name = App.ViewModel.FeaturedStreams[index].channel.name;

            //Go to the video player page
            AccessToken token = await AccessToken.GetToken(name);
            M3U8Playlist playlist = await M3U8Playlist.GetStreamPlaylist(name, token);
            StreamFileList fileList = await StreamFileList.UpdateStreamFileList(playlist, "Mobile");
            MessageBox.Show(fileList.IndexList[0]);
        }

        private void TopStreamTapped(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Debug.WriteLine(((StackPanel)sender).Name);
        }

        private void TopGameTapped(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Debug.WriteLine(((StackPanel)sender).Name);
        }

        private void SettingTapped(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Debug.WriteLine(((TextBlock)sender).Text);
        }
    }
}