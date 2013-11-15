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
            this.TopStreams = new ObservableCollection<ItemViewModel>();
            this.TopGames = new List<TopGame>();
            this.Settings = new ObservableCollection<ItemViewModel>();
            this.FeaturedStreams = new List<Stream>();
        }

        /// <summary>
        /// Featured Streams
        /// </summary>
        public List<Stream> FeaturedStreams { get; set; }

        /// <summary>
        /// A collection for ItemViewModel objects.
        /// </summary>
        public ObservableCollection<ItemViewModel> TopStreams { get; private set; }

        /// <summary>
        /// A collection for ItemViewModel objects.
        /// </summary>
        public List<TopGame> TopGames { get; private set; }

        /// <summary>
        /// A collection for ItemViewModel objects.
        /// </summary>
        public ObservableCollection<ItemViewModel> Settings { get; private set; }

        private string _sampleProperty = "Sample Runtime Property Value";
        /// <summary>
        /// Sample ViewModel property; this property is used in the view to display its value using a Binding
        /// </summary>
        /// <returns></returns>
        public string SampleProperty
        {
            get
            {
                return _sampleProperty;
            }
            set
            {
                if (value != _sampleProperty)
                {
                    _sampleProperty = value;
                    NotifyPropertyChanged("SampleProperty");
                }
            }
        }

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

        /// <summary>
        /// Creates and adds a few ItemViewModel
        /// </summary>
        public async void LoadData()
        {
            // Sample data; replace with real data
            this.Settings.Add(new ItemViewModel() 
            { 
                LineOne = "Some Setting"
            });

            this.TopGames = await TopGame.GetTopGames();

            this.TopStreams.Add(new ItemViewModel()
            {
                LineOne = "Some Stream",
                LineTwo = "128382"
            });

            this.IsDataLoaded = true;
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
    }
}