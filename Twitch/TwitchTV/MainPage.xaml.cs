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
        // Constructor
        public MainPage()
        {
            InitializeComponent();

            // Set the data context of the listbox control to the sample data
            DataContext = App.ViewModel;
        }

        // Load data for the ViewModel Items
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            if (!App.ViewModel.IsDataLoaded)
            {
                App.ViewModel.LoadData();
                App.ViewModel.FeaturedStreams = await Stream.GetFeaturedStreams();
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
            }
        }

        private async void FrontPageIconTapped(object sender, System.Windows.Input.GestureEventArgs e)
        {
            int index = int.Parse(((Canvas)sender).Name.Remove(0, 2)) - 1;
            string name = App.ViewModel.FeaturedStreams[index].channel.name;

            AccessToken token = await AccessToken.GetToken(name);
            M3U8Playlist playlist = await M3U8Playlist.GetStreamPlaylist(name, token);
            StreamFileList fileList = await StreamFileList.UpdateStreamFileList(playlist, "Mobile");
            MessageBox.Show(fileList.IndexList[0]);
        }

        private void TopStreamTapped(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Debug.WriteLine(((TextBlock)sender).Text);
        }

        private void TopGameTapped(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Debug.WriteLine(((TextBlock)sender).Text);
        }

        private void SettingTapped(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Debug.WriteLine(((TextBlock)sender).Text);
        }
    }
}