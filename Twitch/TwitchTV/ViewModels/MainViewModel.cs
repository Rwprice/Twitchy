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

        public MainViewModel()
        {
            curTopGame = new TopGame();
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
            this.FeaturedStreams = await Stream.GetFeaturedStreams();
            this.TopGames = await TopGame.GetTopGames();
            this.TopStreams = await Stream.GetTopStreams();

            IsDataLoaded = true;
        }
    }
}