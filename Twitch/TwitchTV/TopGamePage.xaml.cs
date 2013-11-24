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
using System.Windows.Media.Imaging;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;

namespace TwitchTV
{
    public partial class TopGamePage : PhoneApplicationPage
    {
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

        public TopGamePage()
        {
            InitializeComponent();
            this.TGHeader.Header = App.ViewModel.curTopGame.game.name;
            PropertyChanged += TopGamePage_PropertyChanged;
        }

        private void TopGamePage_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            this.TopStreamsList.ItemsSource = this.TopStreams;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            try
            {
                this.TopStreams = await Stream.GetTopStreamsForGame(App.ViewModel.curTopGame.game.name);
            }

            catch (Exception ex)
            {
                MessageBox.Show("Can't load the top streams for this game", "Well, this is embarrassing...", MessageBoxButton.OK);
                Debug.WriteLine(ex.Message);
            }
        }

        private void SendToVideoPage(object sender, System.Windows.Input.GestureEventArgs e)
        {
            int index = int.Parse(((StackPanel)sender).Name.Remove(0, 2));
            App.ViewModel.stream = this.TopStreams[index];
            NavigationService.Navigate(new Uri("/PlayerPage.xaml", UriKind.RelativeOrAbsolute));
        }

        private void TopStreamsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            App.ViewModel.stream = ((Stream)((ListBox)sender).SelectedItem);
            NavigationService.Navigate(new Uri("/PlayerPage.xaml", UriKind.RelativeOrAbsolute));
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