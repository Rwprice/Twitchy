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
        public StreamFileList fileList { get; set; }

        public PlayerPage()
        {
            InitializeComponent();
        }

        public async void GetVideo()
        {
            //Go to the video player page
            token = await AccessToken.GetToken(App.ViewModel.channel);
            playlist = await M3U8Playlist.GetStreamPlaylist(App.ViewModel.channel, token);
            fileList = await StreamFileList.UpdateStreamFileList(playlist, "Mobile");
            MessageBox.Show(fileList.IndexList[0]);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            GetVideo();
        }
    }
}