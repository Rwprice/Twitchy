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
using System.Windows.Media.Imaging;

namespace TwitchTV
{
    public partial class TopGamePage : PhoneApplicationPage
    {
        /// <summary>
        /// Top Streams
        /// </summary>
        public List<Stream> TopStreams { get; set; }

        public TopGamePage()
        {
            InitializeComponent();
            this.TopStreams = new List<Stream>();
            this.TGHeader.Header = App.ViewModel.curTopGame.game.name;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            this.TopStreams = await Stream.GetTopStreamsForGame(App.ViewModel.curTopGame.game.name);

            #region Set Top Stream
            this.TS0Image.Source = new BitmapImage(this.TopStreams[0].preview.small);
            this.TS0Text.Text = this.TopStreams[0].channel.display_name + "\nViewers: " + this.TopStreams[0].viewers;
            if (TopStreams.Count > 1)
            {
                this.TS1Image.Source = new BitmapImage(this.TopStreams[1].preview.small);
                this.TS1Text.Text = this.TopStreams[1].channel.display_name + "\nViewers: " + this.TopStreams[1].viewers;
            }
            if (TopStreams.Count > 2)
            {
                this.TS2Image.Source = new BitmapImage(this.TopStreams[2].preview.small);
                this.TS2Text.Text = this.TopStreams[2].channel.display_name + "\nViewers: " + this.TopStreams[2].viewers;
            }
            if (TopStreams.Count > 3)
            {
                this.TS3Image.Source = new BitmapImage(this.TopStreams[3].preview.small);
                this.TS3Text.Text = this.TopStreams[3].channel.display_name + "\nViewers: " + this.TopStreams[3].viewers;
            }
            if (TopStreams.Count > 4)
            {
                this.TS4Image.Source = new BitmapImage(this.TopStreams[4].preview.small);
                this.TS4Text.Text = this.TopStreams[4].channel.display_name + "\nViewers: " + this.TopStreams[4].viewers;
            }
            if (TopStreams.Count > 5)
            {
                this.TS5Image.Source = new BitmapImage(this.TopStreams[5].preview.small);
                this.TS5Text.Text = this.TopStreams[5].channel.display_name + "\nViewers: " + this.TopStreams[5].viewers;
            }
            if (TopStreams.Count > 6)
            {
                this.TS6Image.Source = new BitmapImage(this.TopStreams[6].preview.small);
                this.TS6Text.Text = this.TopStreams[6].channel.display_name + "\nViewers: " + this.TopStreams[6].viewers;
            }
            if (TopStreams.Count > 7)
            {
                this.TS7Image.Source = new BitmapImage(this.TopStreams[7].preview.small);
                this.TS7Text.Text = this.TopStreams[7].channel.display_name + "\nViewers: " + this.TopStreams[7].viewers;
            }
            #endregion
        }

        private void SendToVideoPage(object sender, System.Windows.Input.GestureEventArgs e)
        {
            int index = int.Parse(((StackPanel)sender).Name.Remove(0, 2));
            App.ViewModel.channel = this.TopStreams[index].channel.name;
            NavigationService.Navigate(new Uri("/PlayerPage.xaml", UriKind.RelativeOrAbsolute));
        }
    }
}