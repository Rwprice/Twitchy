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

namespace TwitchTV
{
    public partial class PlayerPage : PhoneApplicationPage
    {
        public AccessToken token { get; set; }
        public M3U8Playlist playlist { get; set; }

        public PlayerPage()
        {
            InitializeComponent();
        }

        public async void GetVideo()
        {
            //Go to the video player page
            token = await AccessToken.GetToken(App.ViewModel.channel);
            playlist = await M3U8Playlist.GetStreamPlaylist(App.ViewModel.channel, token);

            string path_to_video = playlist.streams["High"];
            this.Video.Source = new Uri(path_to_video, UriKind.RelativeOrAbsolute);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            GetVideo();
        }
    }
}