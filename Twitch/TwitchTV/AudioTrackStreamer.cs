using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Phone.BackgroundAudio;
using Microsoft.Phone.Info;
using SM.Media.Buffering;
using SM.Media.MediaManager;
using SM.Media.Metadata;
using SM.Media.Utility;
using SM.Media.Web;
using SM.TsParser;

namespace SM.Media.BackgroundAudioStreamingAgent
{
    /// <summary>
    ///     A background agent that performs per-track streaming for playback
    /// </summary>
    public sealed class AudioTrackStreamer : AudioStreamingAgent, IDisposable
    {
        readonly IBufferingPolicy _bufferingPolicy;
        readonly IMediaManagerParameters _mediaManagerParameters;
        readonly AudioMetadataHandler _metadataHandler;

        readonly IWebReaderManagerParameters _webReaderManagerParameters = new WebReaderManagerParameters
        {
            DefaultHeaders = new[] { new KeyValuePair<string, string>("icy-metadata", "1") }
        };

        IMediaStreamFacade _mediaStreamFacade;
        CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        int _isDisposed;

        public AudioTrackStreamer()
        {
            Debug.WriteLine("AudioTrackStreamer ctor");

            _mediaManagerParameters = new MediaManagerParameters
            {
                ProgramStreamsHandler =
                    streams =>
                    {
                        var firstAudio = streams.Streams.First(x => x.StreamType.Contents == TsStreamType.StreamContents.Audio);

                        var others = streams.Streams.Where(x => x.Pid != firstAudio.Pid);
                        foreach (
                            var programStream in others)
                            programStream.BlockStream = true;
                    }
            };

            MediaStreamFacadeSettings.Parameters.UseHttpConnection = true;
            //MediaStreamFacadeSettings.Parameters.UseSingleStreamMediaManager = true;

            _bufferingPolicy = new DefaultBufferingPolicy
            {
                BytesMinimumStarting = 24 * 1024,
                BytesMinimum = 64 * 1024
            };

            _metadataHandler = new AudioMetadataHandler(_cancellationTokenSource.Token);
        }

        protected override async void OnBeginStreaming(AudioTrack track, AudioStreamer streamer)
        {
            Debug.WriteLine("AudioTrackStreamer.OnBeginStreaming() track.Source {0} track.Tag {1}",
                null == track ? "<no track>" : null == track.Source ? "<none>" : track.Source.ToString(),
                null == track ? "<no track>" : track.Tag ?? "<none>");

            try
            {
                if (_cancellationTokenSource.IsCancellationRequested)
                    _cancellationTokenSource = new CancellationTokenSource();

                await RunStreamingAsync(track, streamer).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            { }
            catch (Exception ex)
            {
                Debug.WriteLine("AudioTrackStreamer.OnBeginStreaming() failed: " + ex.ExtendedMessage());
            }
            finally
            {
                Debug.WriteLine("AudioTrackStreamer.OnBeginStreaming() play done NotifyComplete");
                NotifyComplete();
            }
        }

        async Task RunStreamingAsync(AudioTrack track, AudioStreamer streamer)
        {
            IMediaStreamFacade mediaStreamFacade = null;

            try
            {
                if (null == track || null == track.Tag)
                {
                    Debug.WriteLine("AudioTrackStreamer.RunStreamingAsync() null url");
                    return;
                }

                Uri url;
                if (!Uri.TryCreate(track.Tag, UriKind.Absolute, out url))
                {
                    Debug.WriteLine("AudioTrackStreamer.RunStreamingAsync() invalid url: " + track.Tag);
                    return;
                }

                var defaultTitle = "Unknown";

                var mediaTrack = TrackManager.Tracks.FirstOrDefault(t => t.Url == url);

                if (null != mediaTrack)
                    defaultTitle = mediaTrack.Title;

                _metadataHandler.DefaultTitle = defaultTitle;

                if (!string.Equals(track.Title, defaultTitle))
                {
                    track.BeginEdit();
                    track.Title = defaultTitle;
                    track.EndEdit();
                }

                mediaStreamFacade = await InitializeMediaStreamAsync().ConfigureAwait(false);

                Debug.Assert(null != mediaStreamFacade);

                var mss = await mediaStreamFacade.CreateMediaStreamSourceAsync(url, _cancellationTokenSource.Token).ConfigureAwait(false);

                if (null == mss)
                {
                    Debug.WriteLine("AudioTrackStreamer.RunStreamingAsync() unable to create media stream source");
                }
                else
                {
                    streamer.SetSource(mss);

                    await mediaStreamFacade.PlayingTask.ConfigureAwait(false);

                    return;
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("AudioTrackStreamer.RunStreamingAsync() failed: " + ex.ExtendedMessage());
            }

            if (null == mediaStreamFacade)
                return;

            await CleanupMediaStreamFacadeAsync(mediaStreamFacade).ConfigureAwait(false);
        }

        async Task<IMediaStreamFacade> InitializeMediaStreamAsync()
        {
#if SM_MEDIA_LEGACY
            var mediaStreamFacade = _mediaStreamFacade;
#else
            var mediaStreamFacade = Volatile.Read(ref _mediaStreamFacade);
#endif
            if (null != mediaStreamFacade)
            {
                try
                {
                    await mediaStreamFacade.StopAsync(_cancellationTokenSource.Token).ConfigureAwait(false);

                    return mediaStreamFacade;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("AudioTrackStreamer.InitializeMediaStreamAsync() stop failed: " + ex.ExtendedMessage());
                }

                await CleanupMediaStreamFacadeAsync(mediaStreamFacade).ConfigureAwait(false);
            }

            mediaStreamFacade = MediaStreamFacadeSettings.Parameters.Create();

            mediaStreamFacade.SetParameter(_bufferingPolicy);

            mediaStreamFacade.SetParameter(_mediaManagerParameters);

            mediaStreamFacade.SetParameter(_webReaderManagerParameters);

            mediaStreamFacade.SetParameter(_metadataHandler.MetadataSink);

            mediaStreamFacade.StateChange += TsMediaManagerOnStateChange;

            var wasNull = null == Interlocked.CompareExchange(ref _mediaStreamFacade, mediaStreamFacade, null);

            if (!wasNull)
            {
                await UnsafeCleanupMediaStreamFacadeAsync(mediaStreamFacade).ConfigureAwait(false);

                throw new InvalidOperationException("Mismatched media stream facade in InitializeMediaStreamAsync()");
            }

            return mediaStreamFacade;
        }

        void TsMediaManagerOnStateChange(object sender, MediaManagerStateEventArgs mediaManagerStateEventArgs)
        {
            var state = mediaManagerStateEventArgs.State;

            Debug.WriteLine("Media manager state in background agent: {0}, message: {1}", state, mediaManagerStateEventArgs.Message);

            if (null == _mediaStreamFacade)
                return;

            if (MediaManagerState.Closed == state || MediaManagerState.Error == state)
            {
                var cleanupTask = CleanupMediaStreamFacadeAsync();

                TaskCollector.Default.Add(cleanupTask, "TsMediaManagerOnStateChange CleanupMediaStreamFacade");
            }
        }

        async Task CleanupMediaStreamFacadeAsync()
        {
            Debug.WriteLine("AudioTrackStreamer.CleanupMediaStreamFacade()");

            var mediaStreamFacade = Interlocked.Exchange(ref _mediaStreamFacade, null);

            if (null == mediaStreamFacade)
                return;

            try
            {
                await UnsafeCleanupMediaStreamFacadeAsync(mediaStreamFacade).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("AudioTrackStreamer.CleanupMediaStreamFacade() cleanup failed: " + ex.ExtendedMessage());
            }
        }

        async Task CleanupMediaStreamFacadeAsync(IMediaStreamFacade mediaStreamFacade)
        {
            Debug.WriteLine("AudioTrackStreamer.CleanupMediaStreamFacade(msf)");

            try
            {
                var wasOk = mediaStreamFacade == Interlocked.CompareExchange(ref _mediaStreamFacade, null, mediaStreamFacade);

                if (wasOk)
                    await UnsafeCleanupMediaStreamFacadeAsync(mediaStreamFacade).ConfigureAwait(false);
                else
                    Debug.WriteLine("AudioTrackStreamer.CleanupMediaStreamFacade(msf) cleanup lost race with something");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("AudioTrackStreamer.CleanupMediaStreamFacade(msf) cleanup failed: " + ex.ExtendedMessage());
            }
        }

        async Task UnsafeCleanupMediaStreamFacadeAsync(IMediaStreamFacade mediaStreamFacade)
        {
            mediaStreamFacade.StateChange -= TsMediaManagerOnStateChange;

            try
            {
                var localMediaStreamFacade = mediaStreamFacade;

                var closeTask = TaskEx.Run(() => localMediaStreamFacade.CloseAsync());

                var timeoutTask = TaskEx.Delay(6000);

                await TaskEx.WhenAny(closeTask, timeoutTask).ConfigureAwait(false);

                if (closeTask.IsCompleted)
                    closeTask.Wait();
                else
                {
                    Debug.WriteLine("AudioTrackStreamer.UnsafeCleanupMediaStreamFacadeAsync() CloseAsync timeout");

                    var cleanupTask = closeTask.ContinueWith(t =>
                    {
                        var ex = t.Exception;
                        if (null != ex)
                            Debug.WriteLine("AudioTrackStreamer.UnsafeCleanupMediaStreamFacadeAsync() CloseAsync() failed: " + ex.Message);

                        localMediaStreamFacade.DisposeSafe();
                    });

                    TaskCollector.Default.Add(cleanupTask, "AudioTrackStreamer facade cleanup");

                    Abort();

                    return;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("AudioTrackStreamer.UnsafeCleanupMediaStreamFacadeAsync() close async failed: " + ex.ExtendedMessage());

                Abort();
            }

            // Should this Collect be inside the #if DEBUG?
            // The available memory is usually only on the
            // order of a couple of megabytes on a 512MB device.
            // We are at a point where we can afford to stall
            // for a while and we have just released a large
            // number of objects.
            GC.Collect();
        }

        /// <summary>
        ///     Called when the agent request is getting cancelled
        ///     The call to base.OnCancel() is necessary to release the background streaming resources
        /// </summary>
        protected override async void OnCancel()
        {
            Debug.WriteLine("AudioTrackStreamer.OnCancel()");

            try
            {
                TryCancel();

#if SM_MEDIA_LEGACY
                var mediaStreamFacade = _mediaStreamFacade;
#else
                var mediaStreamFacade = Volatile.Read(ref _mediaStreamFacade);
#endif
                if (null != mediaStreamFacade)
                {
                    var allOk = false;

                    try
                    {
                        using (var cts = new CancellationTokenSource())
                        {
                            cts.CancelAfter(TimeSpan.FromSeconds(5));

                            await mediaStreamFacade.StopAsync(cts.Token).ConfigureAwait(false);
                        }

                        allOk = true;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("AudioTrackStreamer.OnCancel() stop failed: " + ex.Message);
                    }

                    if (!allOk)
                    {
                        var cleanupTask = CleanupMediaStreamFacadeAsync();

                        await TaskEx.WhenAny(cleanupTask, TaskEx.Delay(5000)).ConfigureAwait(false);

                        if (!cleanupTask.IsCompleted)
                        {
                            Debug.WriteLine("AudioTrackStreamer.OnCancel() cleanup timeout");

                            TaskCollector.Default.Add(cleanupTask, "OnCancel CleanupMediaStreamFacade");

                            Abort();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("AudioTrackStreamer.OnCancel() failed: " + ex.ExtendedMessage());
            }
            finally
            {
                base.OnCancel();
            }
        }

        void TryCancel()
        {
            try
            {
                if (!_cancellationTokenSource.IsCancellationRequested)
                    _cancellationTokenSource.Cancel();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("AudioTrackStreamer.TryCancel() failed: " + ex.Message);
            }
        }

        public void Dispose()
        {
            Debug.WriteLine("AudioTrackStreamer.Dispose()");

            if (0 != Interlocked.Exchange(ref _isDisposed, 1))
                return;

            TryCancel();

            CleanupMediaStreamFacadeAsync().Wait();

            _cancellationTokenSource.Dispose();
        }
    }
}
