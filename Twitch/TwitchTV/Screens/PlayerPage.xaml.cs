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
using System.Windows.Media.Animation;
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
        public bool handledReturn = false;

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

        public PlayerPage() : base()
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
            phoneAppRootFrame.Unobscured += Unobscured;
        }

        private void Unobscured(object sender, EventArgs e)
        {
            HandleReturn(new Uri("app://phonecall/"));
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
            if (handledReturn)
                return;

            handledReturn = true;

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
                (App.RootFrame as TwitchPhoneApplicationFrame).CurrentStream.Margin = new Thickness(0);
                (App.RootFrame as TwitchPhoneApplicationFrame).CurrentStream.VerticalAlignment = System.Windows.VerticalAlignment.Stretch;
                (App.RootFrame as TwitchPhoneApplicationFrame).CurrentStream.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
                Canvas.SetZIndex((App.RootFrame as TwitchPhoneApplicationFrame).CurrentStream, 0);
            }

            else
            {
                (App.RootFrame as TwitchPhoneApplicationFrame).CurrentStream.Margin = new Thickness(0, 70, 0, 0);
                (App.RootFrame as TwitchPhoneApplicationFrame).CurrentStream.VerticalAlignment = System.Windows.VerticalAlignment.Top;
                (App.RootFrame as TwitchPhoneApplicationFrame).CurrentStream.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
                Canvas.SetZIndex((App.RootFrame as TwitchPhoneApplicationFrame).CurrentStream, 0);
            }

            if (quality == null)
                GetQualities();

            else
                (App.RootFrame as TwitchPhoneApplicationFrame).PlayVideo(quality, playlist.streams[quality]);
        }

        private void HandleLeave(Uri uri)
        {
            handledReturn = false;

            if (chatJoined)
                rejoinChat = true;

            if (uri.OriginalString == @"app://external/")
            {
                if (App.ViewModel.BackgroundAudioEnabled)
                {
                    if (BackgroundAudioPlayer.Instance.PlayerState != PlayState.Playing)
                    {
                        (App.RootFrame as TwitchPhoneApplicationFrame).StopMedia();
                        BackgroundAudioPlayer.Instance.Play();
                    }
                }

                else
                    (App.RootFrame as TwitchPhoneApplicationFrame).PauseMedia();
            }

            else
            {
                client = null;
            }
        }

        #region Video Methods

        private void QualitySelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var obj = (string)((ListPicker)(sender)).SelectedItem;
            if (!string.IsNullOrEmpty(obj))
            {
                if (quality != obj)
                {
                    quality = obj;
                    (App.RootFrame as TwitchPhoneApplicationFrame).PlayVideo(quality, playlist.streams[quality]);
                }
            }
        }

        private void CurrentStream_Tap(object sender, System.Windows.Input.GestureEventArgs e)
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
                if (playlist == null)
                {
                    //Stream is offline
                    playlist = new M3U8Playlist() { streams = new Dictionary<string, Uri>() };
                    playlist.streams.Add("Offline", null);
                    QualitySelection.IsEnabled = false;
                }

                else
                {
                    var list = new Playlist()
                    {
                        Address = playlist.streams[playlist.streams.Keys.ElementAt(playlist.streams.Keys.Count - 1)].OriginalString,
                        Name = App.ViewModel.stream.channel.display_name,
                        Status = App.ViewModel.stream.channel.status,
                        Key = 0
                    };

                    App.Database.SaveAsync<Playlist>(list).Wait();
                    App.Database.FlushAsync().Wait();
                }

                this.QualitySelection.ItemsSource = playlist.streams.Keys;

                QualitySelection.SelectionChanged += QualitySelection_SelectionChanged;
                this.QualitySelection.SelectedItem = playlist.streams.Keys.ElementAt(playlist.streams.Keys.Count - 1);
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
            var CurrentStream = (App.RootFrame as TwitchPhoneApplicationFrame).CurrentStream;

            if ((e.Orientation == PageOrientation.Landscape || e.Orientation == PageOrientation.LandscapeLeft || e.Orientation == PageOrientation.LandscapeRight))
            {
                if (CurrentStream != null)
                {
                    CurrentStream.Margin = new Thickness(0);
                    CurrentStream.VerticalAlignment = System.Windows.VerticalAlignment.Stretch;
                    CurrentStream.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
                }
            }

            else
            {
                if (CurrentStream != null)
                {
                    CurrentStream.Margin = new Thickness(0, 70, 0, 0);
                    CurrentStream.VerticalAlignment = System.Windows.VerticalAlignment.Top;
                    CurrentStream.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
                }
            }

            ToggleUI();
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