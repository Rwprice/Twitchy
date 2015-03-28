using Microsoft.Phone.BackgroundAudio;
using Microsoft.Phone.Controls;
using SM.Media;
using SM.Media.Playlists;
using SM.Media.Segments;
using SM.Media.Utility;
using SM.Media.Web;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Threading;
using TwitchAPIHandler.Objects;

namespace TwitchTV
{
    public partial class PlayerPage : PhoneApplicationPage
    {

        #region Variables
        public AccessToken token { get; set; }
        public M3U8Playlist playlist { get; set; }
        public string quality { get; set; }

        public bool isFollowed = false;
        public bool isScrolling = false;
        public bool chatJoined = false;
        public bool isLoggedIn = false;
        public bool rejoinChat = false;
        

        #region Video
        IMediaStreamFacade _mediaStreamFacade;
        #endregion

        #region UI
        DispatcherTimer uiTimeout;
        DispatcherTimer chatGoToBottom;
        #endregion

        #region Chat
        ChatClient client;
        ObservableCollection<ChatLine> ChatList = new ObservableCollection<ChatLine>();
        public static Dictionary<string, string> userColors = new Dictionary<string, string>();
        public static Random random = new Random();
        ScrollViewer scrollViewer;
        BackgroundWorker backgroundWorker = new BackgroundWorker();
        #endregion
        #endregion

        public PlayerPage()
        {
            token = new AccessToken();
            playlist = new M3U8Playlist();
            InitializeComponent();

            uiTimeout = new DispatcherTimer();
            uiTimeout.Interval = new TimeSpan(0, 0, 4);
            uiTimeout.Tick += uiTimeout_Tick;

            chatGoToBottom = new DispatcherTimer();
            chatGoToBottom.Interval = new TimeSpan(0, 0, 3);
            chatGoToBottom.Tick += chatGoToBottom_Tick;

            backgroundWorker.DoWork += backgroundWorker_DoWork;
            backgroundWorker.ProgressChanged += backgroundWorker_ProgressChanged;
            backgroundWorker.WorkerReportsProgress = true;

            PhoneApplicationFrame phoneAppRootFrame = App.RootFrame;
            phoneAppRootFrame.Obscured += OnObscured;
            phoneAppRootFrame.Unobscured += Unobscured;
        }

        private void Unobscured(object sender, EventArgs e)
        {
            HandleReturn(new Uri("app://phonecall/"));
        }

        private void OnObscured(object sender, ObscuredEventArgs e)
        {
            HandleLeave(new Uri("app://phonecall/"));
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            HandleReturn(e.Uri);
            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            HandleLeave(e.Uri);

            base.OnNavigatedFrom(e);
        }

        //Method to Update UI when navigating directly
        //to the player plage from the home screen
        private async void LoadSettingsAndStream(string streamName)
        {
            App.ViewModel.LoadSettings();

            if (App.ViewModel.stream == null)
            {
                App.ViewModel.stream = new Stream() { channel = new Channel() { name = streamName } };

                //Load stream
                App.ViewModel.stream = await Stream.GetStream(streamName);

                if (App.ViewModel.stream.channel == null)
                {
                    MessageBox.Show("This Stream appears to be offline!");
                    Application.Current.Terminate();
                }

                this.Status.Text = App.ViewModel.stream.channel.status;
            }

            if (App.ViewModel.user == null)
            {
                var user = await User.TryLoadUser();
                if (user != null)
                {
                    App.ViewModel.user = user;
                    isLoggedIn = true;
                }
            }

            IsFollowed(App.ViewModel.stream.channel.name);
        }

        private void HandleReturn(Uri uri)
        {
            if (uri.OriginalString.Contains('?'))
            {
                var streamName = uri.OriginalString.Substring(uri.OriginalString.IndexOf('?') + 1);
                LoadSettingsAndStream(streamName);
            }

            isLoggedIn = App.ViewModel.user != null;
            this.Status.Text = App.ViewModel.stream.channel.status;

            if (App.ViewModel.LockLandscape)
            {
                this.TaskBar.Opacity = 0;
                this.Status.Opacity = 0;
                this.QualitySelection.Opacity = 0;
                this.FavoriteButton.Opacity = 0;
                this.FavoriteLabel.Opacity = 0;
                this.SupportedOrientations = SupportedPageOrientation.Landscape;
            }

            if ((App.ViewModel.AutoJoinChat || rejoinChat))
            {
                JoinChatAndListen();
            }

            if (this.Orientation == PageOrientation.Landscape || this.Orientation == PageOrientation.LandscapeLeft || this.Orientation == PageOrientation.LandscapeRight)
            {
                mediaElement1.Margin = new Thickness(0);
                mediaElement1.VerticalAlignment = System.Windows.VerticalAlignment.Stretch;
                mediaElement1.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
            }

            else
            {
                mediaElement1.Margin = new Thickness(0, 70, 0, 0);
                mediaElement1.VerticalAlignment = System.Windows.VerticalAlignment.Top;
                mediaElement1.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
            }

            if (quality == null)
                GetQualities();

            else
                playVideo();
        }

        private void HandleLeave(Uri uri)
        {
            CleanupMedia();
            client = null;

            //Leaving App
            if (uri.OriginalString == @"app://external/")
            {
                if (chatJoined)
                    rejoinChat = true;

                //var mediaTrack = new Playlist()
                //{
                //    Address = "http://video10.iad02.hls.ttvnw.net/hls132/starladder_cs_en_13758889632_223696550/mobile/py-index-live.m3u8?token=id=7719667929993536106,bid=13758889632,exp=1427649363,node=video10-1.iad02.hls.justin.tv,nname=video10.iad02,fmt=mobile&sig=e31ede099a8dce344e0a2b8b0933e7568fe4f4ea",
                //    Name = App.ViewModel.stream.channel.display_name
                //};

                //mediaTrack.Save();

                //BackgroundAudioPlayer.Instance.Play();
            }
        }

        #region Video Methods
        void playVideo()
        {
            try
            {
                if (quality != "Offline")
                {
                    var task = PlayCurrentTrackAsync();

                    TaskCollector.Default.Add(task, "PlayerPage playVideo");
                }

                else
                {
                    Debug.WriteLine("Stream should be offline");
                }
            }

            catch (Exception ex)
            {
                CleanupMedia();
                MessageBox.Show("Can't play this particular stream. Try another or try again later", "Well, this is embarrassing...", MessageBoxButton.OK);
                Debug.WriteLine(ex.Message);
            }
        }

        async Task PlayCurrentTrackAsync()
        {
            var track = playlist.streams[quality];

            if (null == track)
            {
                await _mediaStreamFacade.StopAsync(CancellationToken.None);

                mediaElement1.Stop();
                mediaElement1.Source = null;

                return;
            }

            mediaElement1.Source = null;

            try
            {
                InitializeMediaStream();

                var mss = await _mediaStreamFacade.CreateMediaStreamSourceAsync(track, CancellationToken.None);

                if (null == mss)
                {
                    Debug.WriteLine("PlayerPage.PlayCurrentTrackAsync() Unable to create media stream source");
                    return;
                }

                mediaElement1.SetSource(mss);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("PlayerPage.PlayCurrentTrackAsync() Unable to create media stream source: " + ex.Message);

                if (ex.Message.Contains("404 (Not Found)"))
                {
                    GetQualities();
                }
                return;
            }

            mediaElement1.Play();
        }

        void InitializeMediaStream()
        {
            if (null != _mediaStreamFacade)
                return;

            _mediaStreamFacade = MediaStreamFacadeSettings.Parameters.Create();
        }

        void StopMedia()
        {
            if (null != mediaElement1)
            {
                mediaElement1.Stop();
                mediaElement1.Source = null;
            }
        }

        void CleanupMedia()
        {
            StopMedia();

            if (null == _mediaStreamFacade)
                return;

            var mediaStreamFacade = _mediaStreamFacade;

            _mediaStreamFacade = null;

            //Don't block the dispose
            mediaStreamFacade.DisposeBackground("PlayerPage CleanupMedia");
        }

        private void mediaElement1_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            CleanupMedia();
        }

        private void QualitySelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (quality != null) // Lowest hasn't been set yet
            {
                var obj = (string)((ListPicker)(sender)).SelectedItem;
                if (!string.IsNullOrEmpty(obj))
                {
                    quality = obj;
                    playVideo();
                }
            }
        }

        private void mediaElement1_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            ToggleUI();
        }

        #endregion

        #region API
        private async void FavoriteButton_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (isLoggedIn)
            {
                if (isFollowed)
                {
                    this.Focus();
                    this.FavoriteButton.IsEnabled = false;
                    await User.UnfollowStream(App.ViewModel.stream.channel.name, App.ViewModel.user);
                    isFollowed = false;
                    this.FavoriteLabel.Text = "Follow";
                    if (this.FavoriteButton.Opacity == .5)
                        this.FavoriteButton.IsEnabled = true;
                }
                else
                {
                    this.Focus();
                    this.FavoriteButton.IsEnabled = false;
                    await User.FollowStream(App.ViewModel.stream.channel.name, App.ViewModel.user);
                    isFollowed = true;
                    this.FavoriteLabel.Text = "Unfollow";
                    if (this.FavoriteButton.Opacity == .5)
                        this.FavoriteButton.IsEnabled = true;
                }
            }
        }

        private async void IsFollowed(string streamName)
        {
            if (isLoggedIn)
            {
                try
                {
                    bool isFollowed = await User.IsStreamFollowed(streamName, App.ViewModel.user);

                    if (isFollowed)
                        this.FavoriteLabel.Text = "Unfollow";
                    else
                        this.FavoriteLabel.Text = "Follow";
                }

                catch (Exception ex)
                {
                    Debug.WriteLine("Couldn't load followed status: " + ex);
                }
            }
        }

        private async void GetQualities()
        {
            IsFollowed(App.ViewModel.stream.channel.name);

            try
            {
                token = await AccessToken.GetToken(App.ViewModel.stream.channel.name);
                playlist = await M3U8Playlist.GetStreamPlaylist(App.ViewModel.stream.channel.name, token);
                if(playlist == null)
                {
                    //Stream is offline
                    playlist = new M3U8Playlist() { streams = new Dictionary<string, Uri>() };
                    playlist.streams.Add("Offline", null);
                    QualitySelection.IsEnabled = false;
                }

                this.QualitySelection.ItemsSource = playlist.streams.Keys;

                if (string.IsNullOrEmpty(quality))
                    quality = playlist.streams.Keys.ElementAt(playlist.streams.Keys.Count - 1);
                
                this.QualitySelection.SelectedItem = quality;
            }

            catch (Exception ex)
            {
                MessageBox.Show("Can't load the qualities list of this stream", "Well, this is embarrassing...", MessageBoxButton.OK);
                Debug.WriteLine(ex.Message);
            }
        }
        #endregion

        #region UI
        private void uiTimeout_Tick(object sender, EventArgs e)
        {
            if (this.TaskBar.Opacity == 1)
                ToggleUI();
        }

        private void ToggleUI()
        {
            if (this.Orientation == PageOrientation.Landscape || this.Orientation == PageOrientation.LandscapeLeft || this.Orientation == PageOrientation.LandscapeRight)
            {
                this.ChatBox.Opacity = 0;
                this.ChatBox.IsEnabled = false;
                this.SendMessageBox.Opacity = 0;
                this.SendMessageBox.IsEnabled = false;
                this.SendButton.Opacity = 0;
                this.SendButton.IsEnabled = false;
                this.ConnectToChat.IsEnabled = false;
                this.ConnectToChat.Opacity = 0;

                if (this.TaskBar.Opacity == 0)
                {
                    this.TaskBar.Opacity = 1;
                    this.QualitySelection.Opacity = 1;
                    this.Status.Opacity = 1;
                    this.QualitySelection.IsEnabled = true;
                    if (isLoggedIn)
                    {
                        this.FavoriteButton.Opacity = .5;
                        this.FavoriteLabel.Opacity = .5;
                        this.FavoriteButton.IsEnabled = true;
                    }
                    this.uiTimeout.Start();
                }

                else
                {
                    this.TaskBar.Opacity = 0;
                    this.QualitySelection.Opacity = 0;
                    this.Status.Opacity = 0;
                    this.QualitySelection.IsEnabled = false;
                    if (isLoggedIn)
                    {
                        this.FavoriteButton.Opacity = 0;
                        this.FavoriteLabel.Opacity = 0;
                        this.FavoriteButton.IsEnabled = false;
                    }
                    this.uiTimeout.Stop();
                }
            }

            else
            {
                this.ChatBox.Opacity = 1;
                this.ChatBox.IsEnabled = true;

                this.SendMessageBox.Opacity = 1;
                this.SendMessageBox.IsEnabled = true;

                this.SendButton.Opacity = 1;
                this.SendButton.IsEnabled = true;

                this.TaskBar.Opacity = 1;
                this.QualitySelection.Opacity = 1;
                this.Status.Opacity = 1;
                this.QualitySelection.IsEnabled = true;

                if (!chatJoined)
                {
                    this.ConnectToChat.IsEnabled = true;
                    this.ConnectToChat.Opacity = 1;
                }

                if (isLoggedIn && this.FavoriteButton.Opacity == 0)
                {
                    this.FavoriteButton.Opacity = .5;
                    this.FavoriteLabel.Opacity = .5;
                    this.FavoriteButton.IsEnabled = true;
                    this.uiTimeout.Start();
                }

                else if (isLoggedIn && this.FavoriteButton.Opacity != 0)
                {
                    this.FavoriteButton.Opacity = 0;
                    this.FavoriteLabel.Opacity = 0;
                    this.FavoriteButton.IsEnabled = false;
                    this.uiTimeout.Stop();
                }
            }
        }

        private void QualitySelection_GotFocus(object sender, RoutedEventArgs e)
        {
            this.uiTimeout.Start();
        }

        protected override void OnOrientationChanged(OrientationChangedEventArgs e)
        {
            if ((e.Orientation == PageOrientation.Landscape || e.Orientation == PageOrientation.LandscapeLeft || e.Orientation == PageOrientation.LandscapeRight) || App.ViewModel.LockLandscape)
            {
                if (this.mediaElement1 != null)
                {
                    this.mediaElement1.Margin = new Thickness(0);
                    this.mediaElement1.VerticalAlignment = System.Windows.VerticalAlignment.Stretch;
                    this.mediaElement1.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
                }
            }

            else
            {
                if (this.mediaElement1 != null)
                {
                    this.mediaElement1.Margin = new Thickness(0, 70, 0, 0);
                    this.mediaElement1.VerticalAlignment = System.Windows.VerticalAlignment.Top;
                    this.mediaElement1.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
                }
            }

            ToggleUI();

            base.OnOrientationChanged(e);
        }
        #endregion

        #region Chat
        void chatGoToBottom_Tick(object sender, EventArgs e)
        {
            isScrolling = false;
        }

        private void SendMessageBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (chatJoined)
            {
                if (e.Key == System.Windows.Input.Key.Enter)
                {
                    if (this.SendMessageBox.Text != "" && this.SendMessageBox.Text != null)
                    {
                        ChatList.Add(client.sendData("PRIVMSG", this.SendMessageBox.Text));
                        if (ChatList.Count > 20)
                        {
                            ChatList.RemoveAt(0);
                        }

                        ScrollIfAtBottom();

                        this.SendMessageBox.Text = "";
                    }
                }
            }
        }

        private void SendMessageBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (chatJoined)
                this.SendMessageBox.Text = "";
            else
                this.Focus();
        }

        private void SendButton_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (chatJoined)
            {
                if (this.SendMessageBox.Text != "" && this.SendMessageBox.Text != null)
                {
                    ChatList.Add(client.sendData("PRIVMSG", this.SendMessageBox.Text));
                    if (ChatList.Count > 20)
                    {
                        ChatList.RemoveAt(0);
                    }

                    ScrollIfAtBottom();

                    this.SendMessageBox.Text = "";
                }
            }
        }

        public void JoinChatAndListen()
        {
            this.Focus();
            this.ConnectToChat.IsEnabled = false;
            this.ConnectToChat.Opacity = 0;

            chatJoined = true;
            try
            {
                ChatList.Add(new ChatLine { Message = "Connecting to chat...", UserName = "", Color = "#ffffff" });
                this.ChatBox.ItemsSource = this.ChatList;

                IRCConfig config;
                if (isLoggedIn)
                {
                    config = new IRCConfig
                    {
                        channel = App.ViewModel.stream.channel.name,
                        nick = App.ViewModel.user.Name,
                        pass = "oauth:" + App.ViewModel.user.Oauth,
                        server = "irc.twitch.tv",
                        port = 6667
                    };
                }
                else
                {
                    config = new IRCConfig
                    {
                        channel = App.ViewModel.stream.channel.name,
                        nick = "justinfan"+random.Next(0,999999)+random.Next(0,99999999),
                        pass = "NoPasswordNeeded",
                        server = "irc.twitch.tv",
                        port = 6667
                    };
                }

                client = new ChatClient(config);
                client.sendData("JOIN", "#" + config.channel);

                backgroundWorker.RunWorkerAsync();
            }

            catch
            {
                MessageBox.Show("Connecting to chat has run into issues", "Well, this is embarrassing...", MessageBoxButton.OK);
            }
        }

        public void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            string[] ex;
            string data;
            char[] charSeparator = new char[] { ' ' };

            while (true)
            {
                try
                {
                    data = client.sr.ReadLine();
                }

                catch
                {
                    data = null;
                }

                if (client == null || data == null)
                    return;

                if(data.Contains("HISTORYEND"))
                    backgroundWorker.ReportProgress(0, new ChatLine { Message = "Connected!", UserName = "", Color = "#ffffff" });

                if (data.Contains("PRIVMSG #" + client.config.channel + " :"))
                {
                    string name = data.Substring(1, data.IndexOf('!') - 1);
                    string msg = data.Substring(data.IndexOf("PRIVMSG #" + client.config.channel + " :") + ("PRIVMSG #" + client.config.channel + " :").Length);

                    string color = GetUserColor(name);

                    ChatLine chatLine = new ChatLine
                    {
                        UserName = name,
                        Message = msg,
                        Color = color
                    };

                    backgroundWorker.ReportProgress(0, chatLine);
                }

                if (data.Contains("This room is in subscribers only mode. To talk"))
                {
                    backgroundWorker.ReportProgress(0, new ChatLine { Message = "This room is in sub only mode. Your message was not sent.", UserName = "NOTICE", Color = "#65000b" });
                }


                ex = data.Split(charSeparator, 5);
                if (ex[0] == "PING")
                {
                    client.sendData("PONG", ex[1]);
                }
            }
        }

        private void backgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            ChatLine chatLine = (ChatLine)(e.UserState);
            ChatList.Add(chatLine);

            if (ChatList.Count > 20)
            {
                ChatList.RemoveAt(0);
            }

            ScrollIfAtBottom();
        }

        public static string GetUserColor(string name)
        {
            if (userColors.Keys.Contains(name))
                return userColors[name];

            else
            {
                var color = String.Format("#{0:X6}", random.Next(0x1000000));
                userColors.Add(name, color);
                return color;
            }
        }

        public void ScrollIfAtBottom()
        {
            if (scrollViewer == null)
            {
                scrollViewer = FindScrollViewer(this.ChatBox);
                scrollViewer.ManipulationStarted += scrollViewer_ManipulationStarted;
            }

            if (!isScrolling)
            {
                scrollViewer.ScrollToVerticalOffset(scrollViewer.ExtentHeight);
            }
        }

        void scrollViewer_ManipulationStarted(object sender, System.Windows.Input.ManipulationStartedEventArgs e)
        {
            isScrolling = true;
            chatGoToBottom.Start();
        }

        static ScrollViewer FindScrollViewer(DependencyObject parent)
        {
            var childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (var i = 0; i < childCount; i++)
            {
                var elt = VisualTreeHelper.GetChild(parent, i);
                if (elt is ScrollViewer) return (ScrollViewer)elt;
                var result = FindScrollViewer(elt);
                if (result != null) return result;
            }
            return null;
        }

        private void ConnectToChat_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            JoinChatAndListen();
        }
        #endregion
    }
}