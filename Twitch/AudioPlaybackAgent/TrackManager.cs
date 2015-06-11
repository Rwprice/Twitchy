using System;
using System.Collections.Generic;
using System.Linq;
using SM.Media.Playlists;
using System.Collections.ObjectModel;
using Wintellect.Sterling.Core;
using Wintellect.Sterling.WP8;
using Wintellect.Sterling.WP8.IsolatedStorage;

namespace AudioPlaybackAgent
{
    static class TrackManager
    {
        private static ISterlingDatabaseInstance Database;

        static TwitchTVMediaTrack[] GetPlaylist()
        {
            if (Database == null)
            {
                var _engine = new SterlingEngine(new PlatformAdapter());
                _engine.Activate();
                Database = _engine.SterlingDatabase.RegisterDatabase<PlaylistDatabaseInstance>("PlaylistDatabase", new IsolatedStorageDriver());
            }

            var playlist = Database.Query<Playlist, int>().FirstOrDefault().LazyValue.Value;

            return new TwitchTVMediaTrack[]
            {
                new TwitchTVMediaTrack
                {
                    Title = playlist.Name,
                    Url = new Uri(playlist.Address),
                    Status = playlist.Status,
                }
            };
        }

        private static IList<TwitchTVMediaTrack> _tracks = null;

        public static IList<TwitchTVMediaTrack> Tracks
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

    class TwitchTVMediaTrack : MediaTrack
    {
        public string Status { get; set; }
    }
}
