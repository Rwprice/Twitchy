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
        public MainViewModel()
        {
            this.TopGames = new List<TopGame>();
            this.FeaturedStreams = new List<Stream>();
        }

        /// <summary>
        /// Featured Streams
        /// </summary>
        public List<Stream> FeaturedStreams { get; set; }

        /// <summary>
        /// Top Games
        /// </summary>
        public List<TopGame> TopGames { get; set; }

        /// <summary>
        /// Sample property that returns a localized string
        /// </summary>
        public string LocalizedSampleProperty
        {
            get
            {
                return AppResources.SampleProperty;
            }
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

        public void LoadData()
        {
            IsDataLoaded = true;
        }
    }
}