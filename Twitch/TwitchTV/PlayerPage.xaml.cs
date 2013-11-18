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

        public bool firstRun = true;

        readonly IHttpClients _httpClients;
        IMediaElementManager _mediaElementManager;
        PlaylistSegmentManager _playlist;
        ITsMediaManager _tsMediaManager;
        TsMediaStreamSource _tsMediaStreamSource;
        Program program;
        ISubProgram subProgram;

        public PlayerPage()
        {
            token = new AccessToken();
            playlist = new M3U8Playlist();
            InitializeComponent();
            _httpClients = new HttpClients();
        }

        private async void GetQualities()
        {
            token = await AccessToken.GetToken(App.ViewModel.channel);
            playlist = await M3U8Playlist.GetStreamPlaylist(App.ViewModel.channel, token);

            if (string.IsNullOrEmpty(quality))
                quality = playlist.streams.Keys.ElementAt(playlist.streams.Keys.Count - 1);

            this.QualitySelection.ItemsSource = playlist.streams.Keys;
            this.QualitySelection.SelectedItem = quality;

            firstRun = false;

            playVideo();
        }

        async void playVideo()
        {
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
                        Margin = new Thickness(0, 68, 0, 0)

                    };

                    me.MediaFailed += mediaElement1_MediaFailed;
                    me.CurrentStateChanged += mediaElement1_CurrentStateChanged;
                    me.BufferingProgressChanged += OnBufferingProgressChanged;
                    ContentPanel.Children.Add(me);

                    this.mediaElement1 = me;

                    UpdateState(MediaElementState.Opening);

                    return me;
                },
                me =>
                {
                    if (null != me)
                    {
                        ContentPanel.Children.Remove(me);

                        me.MediaFailed -= mediaElement1_MediaFailed;
                        me.CurrentStateChanged -= mediaElement1_CurrentStateChanged;
                        me.BufferingProgressChanged -= OnBufferingProgressChanged;
                    }

                    mediaElement1.Stop();
                    mediaElement1 = null;

                    UpdateState(MediaElementState.Closed);
                });
            #endregion

            var segmentReaderManager = new SegmentReaderManager(new[] { _playlist }, _httpClients.CreateSegmentClient);

            _tsMediaStreamSource = new TsMediaStreamSource();

            _tsMediaManager = new TsMediaManager(segmentReaderManager, _mediaElementManager, _tsMediaStreamSource);

            _tsMediaManager.OnStateChange += TsMediaManagerOnOnStateChange;

            _tsMediaManager.Play();
        }

        async void CleanupMedia()
        {
            await _mediaElementManager.Close();
            _mediaElementManager = null;

            _tsMediaManager.Close();
            _tsMediaManager = null;

            await _playlist.StopAsync();
            _playlist.Dispose();
            _playlist = null;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            GetQualities();
            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            CleanupMedia();
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

            if (MediaElementState.Buffering == state && null != mediaElement1)
                Debug.WriteLine(string.Format("Buffering {0:F2}%", mediaElement1.BufferingProgress * 100));
            else
                Debug.WriteLine(state.ToString());
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
    }
}