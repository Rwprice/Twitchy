using Microsoft.Phone.BackgroundAudio;
using Microsoft.Phone.Controls;
using SM.Media;
using SM.Media.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TwitchTV
{
    public partial class TwitchPhoneApplicationFrame : PhoneApplicationFrame
    {
        #region Video
        private static IMediaStreamFacade _mediaStreamFacade;
        public MediaElement CurrentStream;
        #endregion

        public void SetMediaElement(MediaElement value)
        {
            CurrentStream = value;
            CurrentStream.AutoPlay = true;

            CurrentStream.CurrentStateChanged += CurrentStream_CurrentStateChanged;
            CurrentStream.MediaFailed += CurrentStream_CurrentStateChanged;
            CurrentStream.Hold += CurrentStream_Hold;
        }

        #region Video Methods
        public void PlayVideo(string quality, Uri track)
        {
            if (CurrentStream.CurrentState == MediaElementState.Paused)
            {
                CurrentStream.Play();
            }

            try
            {
                if (quality != "Offline")
                {
                    var task = PlayCurrentTrackAsync(track);

                    TaskCollector.Default.Add(task, "MainViewModel PlayVideo");
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

        private async Task PlayCurrentTrackAsync(Uri track)
        {
            if (null == track)
            {
                await _mediaStreamFacade.StopAsync(CancellationToken.None);

                StopMedia();

                return;
            }

            try
            {
                InitializeMediaStream();

                var mss = await _mediaStreamFacade.CreateMediaStreamSourceAsync(track, CancellationToken.None);

                if (null == mss)
                {
                    Debug.WriteLine("PlayerPage.PlayCurrentTrackAsync() Unable to create media stream source");
                    return;
                }

                CurrentStream.SetSource(mss);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("PlayerPage.PlayCurrentTrackAsync() Unable to create media stream source: " + ex.Message);

                if (ex.Message.Contains("404 (Not Found)"))
                {
                    //GetQualities();
                }
                return;
            }
        }

        private void InitializeMediaStream()
        {
            if (null != _mediaStreamFacade)
                return;

            _mediaStreamFacade = MediaStreamFacadeSettings.Parameters.Create();
        }

        public void StopMedia()
        {
            if (null != CurrentStream)
            {
                CurrentStream.Stop();
                CurrentStream.Source = null;
            }

            BackgroundAudioPlayer.Instance.Close();
        }

        public void PauseMedia()
        {
            if (null != CurrentStream)
                CurrentStream.Pause();
        }

        public void CleanupMedia()
        {
            StopMedia();

            if (null == _mediaStreamFacade)
                return;

            var mediaStreamFacade = _mediaStreamFacade;

            _mediaStreamFacade = null;

            mediaStreamFacade.DisposeBackground("PlayerPage CleanupMedia");
        }

        private void CurrentStream_CurrentStateChanged(object sender, RoutedEventArgs e)
        {
            var currentState = (sender as MediaElement).CurrentState;

            switch (currentState)
            {
                case MediaElementState.Closed:
                    StopMedia();
                    break;

                default:
                    Debug.WriteLine("Stream.CurrentState: " + currentState);
                    break;
            }
        }

        private void CurrentStream_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            CleanupMedia();
        }

        private void CurrentStream_Hold(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (CurrentStream.CurrentState == MediaElementState.Playing && !CurrentSource.OriginalString.Contains("//Screens/PlayerPage.xaml"))
            {
                Canvas.SetZIndex(CurrentStream, 0);
                CleanupMedia();
            }
        }
        #endregion
    }
}
