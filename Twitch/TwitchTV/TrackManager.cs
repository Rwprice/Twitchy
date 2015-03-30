using System;
using System.Collections.Generic;
using System.Linq;
using SM.Media.Playlists;
using TwitchTV;
using System.Collections.ObjectModel;
using Wintellect.Sterling.Core;
using Wintellect.Sterling.WP8;
using Wintellect.Sterling.WP8.IsolatedStorage;

namespace SM.Media.BackgroundAudioStreamingAgent
{
    static class TrackManager
    {
        private static ISterlingDatabaseInstance Database;

        static MediaTrack[] GetPlaylist()
        {
            if (Database == null)
            {
                var _engine = new SterlingEngine(new PlatformAdapter());
                _engine.Activate();
                Database = _engine.SterlingDatabase.RegisterDatabase<PlaylistDatabaseInstance>("PlaylistDatabase", new IsolatedStorageDriver());
            }

            var playlist = Database.Query<Playlist, int>().FirstOrDefault().LazyValue.Value;

            return new MediaTrack[]
            {
                new MediaTrack
                {
                    Title = playlist.Name,
                    Url = new Uri(playlist.Address)
                }
            };
        }

        private static IList<MediaTrack> _tracks = null;

        public static IList<MediaTrack> Tracks
        {
            get
            {
                if (_tracks == null)
                {
                    _tracks = GetPlaylist();
                }

                return _tracks;
            }
        }
    }
}
