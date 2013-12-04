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
using SM.Media.Web;
using System.Windows.Threading;
using SM.Media;
using SM.Media.Playlists;
using System.Net.Http.Headers;
using SM.Media.Utility;
using SM.Media.Segments;
using System.Windows.Media;

namespace TwitchTV
{
    public partial class PlayerPage : PhoneApplicationPage
    {
        public AccessToken token { get; set; }
        public M3U8Playlist playlist { get; set; }
        public string quality { get; set; }

        public bool isFollowed = false;
        public bool firstRun = true;

        readonly IHttpClients _httpClients;
        IMediaElementManager _mediaElementManager;
        PlaylistSegmentManager _playlist;
        ITsMediaManager _tsMediaManager;
        TsMediaStreamSource _tsMediaStreamSource;
        Program program;
        ISubProgram subProgram;
        DispatcherTimer uiTimeout;

        public PlayerPage()
        {
            token = new AccessToken();
            playlist = new M3U8Playlist();
            InitializeComponent();
            _httpClients = new HttpClients();
            
            if(App.ViewModel.stream != null)
                this.Status.Text = App.ViewModel.stream.channel.status;

            uiTimeout = new DispatcherTimer();
            uiTimeout.Interval = new TimeSpan(0, 0, 4);
            uiTimeout.Tick += uiTimeout_Tick;
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
                isFollowed = await User.IsStreamFollowed(App.ViewModel.stream.channel.name, App.ViewModel.user);

                if (isFollowed)
                    this.FavoriteLabel.Text = "Unfollow";
                else
                    this.FavoriteLabel.Text = "Follow";

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
                        var me = new MediaElement
                        {
                            Margin = new Thickness(0),
                            Height = 480,
                            Width = 800
                        };

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

                if (null != _tsMediaManager)
                    _tsMediaManager.OnStateChange -= TsMediaManagerOnOnStateChange;

                _tsMediaStreamSource = new TsMediaStreamSource();

                _tsMediaManager = new TsMediaManager(segmentReaderManager, _mediaElementManager, _tsMediaStreamSource);

                _tsMediaManager.OnStateChange += TsMediaManagerOnOnStateChange;

                this.uiTimeout.Start();

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

                if (null != _playlist)
                    await _playlist.StopAsync();

                if (null != _mediaElementManager)
                    await _mediaElementManager.Close();
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

            GetQualities();
            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            CleanupMedia();

            if (null != _mediaElementManager)
            {
                _mediaElementManager.Close()
                                    .Wait();
            }
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
            Debug.WriteLine("MediaElement State: " + state);
        }

        void TsMediaManagerOnOnStateChange(object sender, TsMediaManagerStateEventArgs tsMediaManagerStateEventArgs)
        {
            Dispatcher.InvokeAsync(() =>
            {
                var message = tsMediaManagerStateEventArgs.Message;

                if (!string.IsNullOrWhiteSpace(message))
                {
                    Debug.WriteLine(message);
                }

                mediaElement1_CurrentStateChanged(null, null);
            });
        }

        private void mediaElement1_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            MessageBox.Show("The media failed to load", "Well, this is embarrassing...", MessageBoxButton.OK);
            Debug.WriteLine(e.ErrorException.Message);
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
            if (this.TaskBar.Opacity == 0)
            {
                this.TaskBar.Opacity = 1;
                this.QualitySelection.Opacity = 1;
                this.Status.Opacity = 1;
                this.QualitySelection.IsEnabled = true;
                this.FavoriteButton.Opacity = .5;
                this.FavoriteLabel.Opacity = .5;
                this.uiTimeout.Start();
            }

            else
            {
                this.TaskBar.Opacity = 0;
                this.QualitySelection.Opacity = 0;
                this.Status.Opacity = 0;
                this.QualitySelection.IsEnabled = false;
                this.FavoriteButton.Opacity = 0;
                this.FavoriteLabel.Opacity = 0;
                this.uiTimeout.Stop();
            }
        }

        private void QualitySelection_GotFocus(object sender, RoutedEventArgs e)
        {
            this.uiTimeout.Start();
        }

        private void FavoriteButton_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (isFollowed)
            {
                User.UnfollowStream(App.ViewModel.stream.channel.name, App.ViewModel.user);
                isFollowed = false;
                this.FavoriteLabel.Text = "Follow";
            }
            else
            {
                User.FollowStream(App.ViewModel.stream.channel.name, App.ViewModel.user);
                isFollowed = true;
                this.FavoriteLabel.Text = "Unfollow";
            }
        }
    }
}