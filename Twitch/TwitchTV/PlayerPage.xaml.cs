using Microsoft.Phone.Controls;
using SM.Media;
using SM.Media.Playlists;
using SM.Media.Segments;
using SM.Media.Web;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
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
        public AccessToken token { get; set; }
        public M3U8Playlist playlist { get; set; }
        public string quality { get; set; }

        public bool isFollowed = false;
        public bool firstRun = true;
        public bool isScrolling = false;
        public bool chatJoined = false;
        public bool isLoggedIn = false;
        public bool navigatedFrom = false;

        readonly IHttpClients _httpClients;
        IMediaElementManager _mediaElementManager;
        PlaylistSegmentManager _playlist;
        ITsMediaManager _tsMediaManager;
        TsMediaStreamSource _tsMediaStreamSource;
        Program program;
        ISubProgram subProgram;

        DispatcherTimer uiTimeout;
        DispatcherTimer chatGoToBottom;

        ChatClient client;
        ObservableCollection<ChatLine> ChatList = new ObservableCollection<ChatLine>();
        public static Dictionary<string, string> userColors = new Dictionary<string, string>();
        public static Random random = new Random();
        ScrollViewer scrollViewer;
        BackgroundWorker backgroundWorker = new BackgroundWorker();

        public PlayerPage()
        {
            isLoggedIn = App.ViewModel.user != null;

            token = new AccessToken();
            playlist = new M3U8Playlist();
            InitializeComponent();
            _httpClients = new HttpClients();

            if (App.ViewModel.LockLandscape)
            {
                this.TaskBar.Opacity = 0;
                this.Status.Opacity = 0;
                this.QualitySelection.Opacity = 0;
                this.FavoriteButton.Opacity = 0;
                this.FavoriteLabel.Opacity = 0;
                this.SupportedOrientations = SupportedPageOrientation.Landscape;
            }
            
            if(App.ViewModel.stream != null)
                this.Status.Text = App.ViewModel.stream.channel.status;

            if (!isLoggedIn)
                this.ConnectToChatText.Text = "Log in to join chat";

            uiTimeout = new DispatcherTimer();
            uiTimeout.Interval = new TimeSpan(0, 0, 4);
            uiTimeout.Tick += uiTimeout_Tick;

            chatGoToBottom = new DispatcherTimer();
            chatGoToBottom.Interval = new TimeSpan(0, 0, 3);
            chatGoToBottom.Tick += chatGoToBottom_Tick;

            backgroundWorker.DoWork += backgroundWorker_DoWork;
            backgroundWorker.ProgressChanged += backgroundWorker_ProgressChanged;
            backgroundWorker.WorkerReportsProgress = true;
        }

        void chatGoToBottom_Tick(object sender, EventArgs e)
        {
            isScrolling = false;
        }

        private void uiTimeout_Tick(object sender, EventArgs e)
        {
            if (this.TaskBar.Opacity == 1)
                ToggleUI();
        }

        private async void GetQualities()
        {
            try
            {
                if (isLoggedIn)
                {
                    isFollowed = await User.IsStreamFollowed(App.ViewModel.stream.channel.name, App.ViewModel.user);

                    if (isFollowed)
                        this.FavoriteLabel.Text = "Unfollow";
                    else
                        this.FavoriteLabel.Text = "Follow";
                }

                token = await AccessToken.GetToken(App.ViewModel.stream.channel.name);
                playlist = await M3U8Playlist.GetStreamPlaylist(App.ViewModel.stream.channel.name, token);

                if (string.IsNullOrEmpty(quality))
                    quality = playlist.streams.Keys.ElementAt(playlist.streams.Keys.Count - 1);

                this.QualitySelection.ItemsSource = playlist.streams.Keys;
                this.QualitySelection.SelectedItem = quality;

                firstRun = false;
            }

            catch (Exception ex)
            {
                MessageBox.Show("Can't load the qualities list of this stream", "Well, this is embarrassing...", MessageBoxButton.OK);
                Debug.WriteLine(ex.Message);
            }

            if (!navigatedFrom)
                playVideo();
        }

        async void playVideo()
        {
            try
            {
                CleanupMedia();

                if (null != _playlist)
                {
                    _playlist.Dispose();
                    _playlist = null;
                }

                if (null != _tsMediaStreamSource)
                {
                    _tsMediaStreamSource.Dispose();
                    _tsMediaStreamSource = null;
                }

                var segmentsFactory = new SegmentsFactory(_httpClients);

                var programManager = new ProgramManager(_httpClients, segmentsFactory.CreateStreamSegments)
                {
                    Playlists = new Uri[] { M3U8Playlist.indexUri }
                };

                var programs = await programManager.LoadAsync();

                program = programs.Values.FirstOrDefault();

                subProgram = program.SubPrograms.ElementAt(playlist.GetIndexOfQuality(quality));

                var programClient = _httpClients.CreatePlaylistClient(program.Url);

                _playlist = new PlaylistSegmentManager(uri => new CachedWebRequest(uri, programClient), subProgram, segmentsFactory.CreateStreamSegments);

                #region MediaElementManager
                _mediaElementManager = new MediaElementManager(Dispatcher,
                    () =>
                    {
                        var me = new MediaElement();

                        if (this.Orientation == PageOrientation.Landscape || this.Orientation == PageOrientation.LandscapeLeft || this.Orientation == PageOrientation.LandscapeRight)
                        {
                            me = new MediaElement
                            {
                                Margin = new Thickness(0),
                                VerticalAlignment = System.Windows.VerticalAlignment.Stretch,
                                HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch
                            };
                        }

                        else
                        {
                            me = new MediaElement
                            {
                                Margin = new Thickness(0,70,0,0),
                                VerticalAlignment = System.Windows.VerticalAlignment.Top,
                                HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch
                            };
                        }
                        

                        me.MediaFailed += mediaElement1_MediaFailed;
                        me.CurrentStateChanged += mediaElement1_CurrentStateChanged;
                        me.BufferingProgressChanged += OnBufferingProgressChanged;
                        ContentPanel.Children.Add(me);

                        mediaElement1 = me;
                        mediaElement1.Tap += mediaElement1_Tap;

                        UpdateState(MediaElementState.Opening);

                        return me;
                    },
                    me =>
                    {
                        if (null != me)
                        {
                            Debug.Assert(ReferenceEquals(me, mediaElement1));

                            ContentPanel.Children.Remove(me);

                            me.MediaFailed -= mediaElement1_MediaFailed;
                            me.CurrentStateChanged -= mediaElement1_CurrentStateChanged;
                            me.BufferingProgressChanged -= OnBufferingProgressChanged;
                        }

                        mediaElement1 = null;

                        UpdateState(MediaElementState.Closed);
                    });
                #endregion

                var segmentReaderManager = new SegmentReaderManager(new[] { _playlist }, _httpClients.CreateSegmentClient);

                if (!navigatedFrom)
                {
                    if (null != _tsMediaManager)
                        _tsMediaManager.OnStateChange -= TsMediaManagerOnOnStateChange;

                    _tsMediaStreamSource = new TsMediaStreamSource();

                    _tsMediaManager = new TsMediaManager(segmentReaderManager, _mediaElementManager, _tsMediaStreamSource);

                    _tsMediaManager.OnStateChange += TsMediaManagerOnOnStateChange;

                    this.uiTimeout.Start();
                }

                if(!navigatedFrom)
                    _tsMediaManager.Play();
            }

            catch (Exception ex)
            {
                CleanupMedia();
                MessageBox.Show("Can't play this particular stream. Try another or try again later", "Well, this is embarrassing...", MessageBoxButton.OK);
                Debug.WriteLine(ex.Message);
            }
        }

        async void CleanupMedia()
        {
            try
            {
                if (null != _tsMediaManager)
                    _tsMediaManager.Close();

                _tsMediaManager = null;

                if (null != _playlist)
                    await _playlist.StopAsync();

                _playlist = null;

                if (null != _mediaElementManager)
                    await _mediaElementManager.Close();

                _mediaElementManager = null;
            }

            catch (Exception ex)
            {
                MessageBox.Show("There was an issue while closing the stream", "Well, this is embarrassing...", MessageBoxButton.OK);
                Debug.WriteLine(ex.Message);
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (null != _mediaElementManager)
            {
                _mediaElementManager.Close()
                                    .Wait();
            }

            if (App.ViewModel.AutoJoinChat && isLoggedIn)
            {
                JoinChatAndListen();
            }

            GetQualities();
            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            navigatedFrom = true;

            CleanupMedia();

            if (null != _mediaElementManager)
            {
                _mediaElementManager.Close()
                                    .Wait();
            }

            client = null;

            base.OnNavigatedFrom(e);
        }

        void OnBufferingProgressChanged(object sender, RoutedEventArgs routedEventArgs)
        {
            mediaElement1_CurrentStateChanged(sender, routedEventArgs);
        }

        void mediaElement1_CurrentStateChanged(object sender, RoutedEventArgs e)
        {
            var state = null == mediaElement1 ? MediaElementState.Closed : mediaElement1.CurrentState;

            if (null != _mediaElementManager)
            {
                var managerState = _tsMediaManager.State;

                if (MediaElementState.Closed == state)
                {
                    if (TsMediaManager.MediaState.OpenMedia == managerState || TsMediaManager.MediaState.Opening == managerState || TsMediaManager.MediaState.Playing == managerState)
                        state = MediaElementState.Opening;
                }
            }

            UpdateState(state);
        }

        void UpdateState(MediaElementState state)
        {
            
        }

        void TsMediaManagerOnOnStateChange(object sender, TsMediaManagerStateEventArgs tsMediaManagerStateEventArgs)
        {
            Dispatcher.InvokeAsync(() =>
            {
                var message = tsMediaManagerStateEventArgs.Message;

                if (!string.IsNullOrWhiteSpace(message))
                {
                    
                }

                mediaElement1_CurrentStateChanged(null, null);
            });
        }

        private void mediaElement1_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            MessageBox.Show("The media failed to load", "Well, this is embarrassing...", MessageBoxButton.OK);
            CleanupMedia();
        }

        private void QualitySelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!firstRun)
            {
                var obj = (string)((ListPicker)(sender)).SelectedItem;
                if (!string.IsNullOrEmpty(obj))
                {
                    quality = obj;
                    GetQualities();
                }
            }
        }

        private void mediaElement1_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
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

        protected override void OnOrientationChanged(OrientationChangedEventArgs e)
        {
            if ((e.Orientation == PageOrientation.Landscape || e.Orientation == PageOrientation.LandscapeLeft || e.Orientation == PageOrientation.LandscapeRight) || App.ViewModel.LockLandscape)
            {
                this.mediaElement1.Margin = new Thickness(0);
                this.mediaElement1.VerticalAlignment = System.Windows.VerticalAlignment.Stretch;
                this.mediaElement1.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
            }

            else
            {
                this.mediaElement1.Margin = new Thickness(0, 70, 0, 0);
                this.mediaElement1.VerticalAlignment = System.Windows.VerticalAlignment.Top;
                this.mediaElement1.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
            }

            ToggleUI();

            base.OnOrientationChanged(e);
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

            ChatList.Add(new ChatLine { Message = "Connecting to chat...", UserName = "", Color = "#ffffff" });
            this.ChatBox.ItemsSource = this.ChatList;

            IRCConfig config = new IRCConfig
            {
                channel = App.ViewModel.stream.channel.name,
                nick = App.ViewModel.user.Name,
                pass = "oauth:" + App.ViewModel.user.Oauth,
                server = "irc.twitch.tv",
                port = 6667
            };

            client = new ChatClient(config);            
            client.sendData("JOIN", "#" + config.channel);

            backgroundWorker.RunWorkerAsync();
        }

        public void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            string[] ex;
            string data;
            char[] charSeparator = new char[] { ' ' };

            while (true)
            {
                data = client.sr.ReadLine();

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
            if (isLoggedIn)
            {
                JoinChatAndListen();
            }
        }
    }
}