using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using TwitchAPIHandler.Objects;
using TwitchTV.Resources;

namespace TwitchTV.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// Place holder for the stream while switching screens
        /// </summary>
        public Stream stream { get; set; }

        /// <summary>
        /// Place holder for the chosen top game while switching screens
        /// </summary>
        public TopGame curTopGame { get; set; }

        /// <summary>
        /// Timestamp of the last update of the main menu streams
        /// </summary>
        public DateTime lastUpdate { get; set; }

        /// <summary>
        /// Timestamp of the last update of the main menu streams
        /// </summary>
        public User user { get; set; }

        private bool alreadyLoadedFromToken = false;

        /// <summary>
        /// Search Streams
        /// </summary>
        private ObservableCollection<Stream> _SearchStreams;
        public ObservableCollection<Stream> SearchStreams
        {
            get
            {
                return _SearchStreams;
            }
            set
            {
                if (value != _SearchStreams)
                {
                    _SearchStreams = value;
                    NotifyPropertyChanged("SearchStreams");
                }
            }
        }

        /// <summary>
        /// Search Games
        /// </summary>
        private ObservableCollection<TopGame> _SearchGames;
        public ObservableCollection<TopGame> SearchGames
        {
            get
            {
                return _SearchGames;
            }
            set
            {
                if (value != _SearchGames)
                {
                    _SearchGames = value;
                    NotifyPropertyChanged("SearchGames");
                }
            }
        }

        /// <summary>
        /// Featured Streams
        /// </summary>
        private ObservableCollection<Stream> _FeaturedStreams;
        public ObservableCollection<Stream> FeaturedStreams
        {
            get
            {
                return _FeaturedStreams;
            }
            set
            {
                if (value != _FeaturedStreams)
                {
                    _FeaturedStreams = value;
                    NotifyPropertyChanged("FeaturedStreams");
                }
            }
        }

        /// <summary>
        /// Top Games
        /// </summary>
        private ObservableCollection<TopGame> _TopGames;
        public ObservableCollection<TopGame> TopGames
        {
            get
            {
                return _TopGames;
            }
            set
            {
                if (value != _TopGames)
                {
                    _TopGames = value;
                    NotifyPropertyChanged("TopGames");
                }
            }
        }

        /// <summary>
        /// Top Streams
        /// </summary>
        private ObservableCollection<Stream> _TopStreams;
        public ObservableCollection<Stream> TopStreams
        {
            get
            {
                return _TopStreams;
            }
            set
            {
                if (value != _TopStreams)
                {
                    _TopStreams = value;
                    NotifyPropertyChanged("TopStreams");
                }
            }
        }

        // <summary>
        /// Top Streams
        /// </summary>
        private ObservableCollection<Stream> _FollowedStreams;
        public ObservableCollection<Stream> FollowedStreams
        {
            get
            {
                return _FollowedStreams;
            }
            set
            {
                if (value != _FollowedStreams)
                {
                    _FollowedStreams = value;
                    NotifyPropertyChanged("FollowedStreams");
                }
            }
        }

        public MainViewModel()
        {

        }

        public bool IsDataLoaded
        {
            get;
            private set;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (null != handler)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public async void LoadData()
        {
            if (lastUpdate == null || !alreadyLoadedFromToken || lastUpdate.AddMinutes(2) <= DateTime.Now)
            {
                this.FeaturedStreams = await Stream.GetFeaturedStreams();
                this.TopGames = await TopGame.GetTopGames();
                this.TopStreams = await Stream.GetTopStreams();
                if (user != null)
                {
                    this.FollowedStreams = await Stream.GetFollowedStreams(user.Oauth);
                    alreadyLoadedFromToken = true;
                }

                lastUpdate = DateTime.Now;
            }
        }
    }
}