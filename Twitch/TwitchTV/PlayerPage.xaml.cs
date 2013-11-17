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
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace TwitchTV
{
    public partial class PlayerPage : PhoneApplicationPage
    {
        public AccessToken token { get; set; }
        public M3U8Playlist playlist { get; set; }

        public List<string> qualitys { get; set; }

        public string quality { get; set; }

        public PlayerPage()
        {
            qualitys = new List<string>();
            token = new AccessToken();
            playlist = new M3U8Playlist();
            InitializeComponent();
        }

        public async void GetQualities()
        {
            if (string.IsNullOrEmpty(token.Token) || string.IsNullOrEmpty(token.Signature))
            {
                //Go to the video player page
                token = await AccessToken.GetToken(App.ViewModel.channel);
                playlist = await M3U8Playlist.GetStreamPlaylist(App.ViewModel.channel, token);
            }

            foreach (var qual in playlist.streams.Keys)
                qualitys.Add(qual);

            if(quality == "" || quality == null)
                quality = qualitys[qualitys.Count-1];

            qualitys.Reverse();

            this.QualitySelection.ItemsSource = qualitys;
            this.QualitySelection.SelectedItem = quality;
        }

        public void GetVideo()
        {
            this.Video.Close();

            string path_to_video = playlist.streams[quality];

            this.Video.ControlPanel.IsEnabled = false;
            this.Video.ControlPanel.Visibility = System.Windows.Visibility.Collapsed;

            this.Video.Source = new Uri(path_to_video, UriKind.RelativeOrAbsolute);
            this.Video.Play();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            GetQualities();
            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.Video.Close();
            base.OnNavigatedFrom(e);
        }

        protected override void OnOrientationChanged(OrientationChangedEventArgs e)
        {
            base.OnOrientationChanged(e);
        }

        private void QualitySelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var obj = ((ListPicker)(sender)).SelectedItem;
            if (obj != null)
            {
                quality = (string)obj;
                GetVideo();
            }
        }

        private void Video_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (this.TaskBar.Opacity == 0)
            {
                this.TaskBar.Opacity = 1;
                this.QualitySelection.Opacity = 1;
            }
            else
            {
                this.TaskBar.Opacity = 0;
                this.QualitySelection.Opacity = 0;
            }
        }
    }
}