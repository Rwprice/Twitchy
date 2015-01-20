using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using TwitchAPIHandler.Objects;
using Windows.Storage;

namespace TwitchTV.ViewModels
{
    class TopGamesViewModel: INotifyPropertyChanged
    {
        private bool _isLoading = false;
        private bool _isGamesLoaded = false;

        public bool IsLoading
        {
            get
            {
                return _isLoading;
            }
            set
            {
                _isLoading = value;
                NotifyPropertyChanged("IsLoading");

            }
        }

        public bool IsGamesLoaded
        {
            get
            {
                return _isGamesLoaded;
            }
            set
            {
                _isGamesLoaded = value;
                NotifyPropertyChanged("IsGamesLoaded");

            }
        }

        public TopGamesViewModel()
        {
            this.GamesList = new ObservableCollection<TopGame>();
            this.IsLoading = false;
        }

        public ObservableCollection<TopGame> GamesList
        {
            get;
            private set;
        }

        public void LoadPage(int pageNumber)
        {
            if (pageNumber == 0)
            {
                this.GamesList.Clear();
                IsGamesLoaded = false;
            }

            if (!IsGamesLoaded)
            {
                IsLoading = true;
                HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(new Uri(String.Format(TopGame.TOP_GAMES_PATH, 8 * pageNumber)));
                request.BeginGetResponse(new AsyncCallback(ReadGamesCallback), request);
            }
        }

        private void ReadGamesCallback(IAsyncResult asynchronousResult)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)asynchronousResult.AsyncState;
                HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(asynchronousResult);
                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    JToken o = JObject.Parse(reader.ReadLine());
                    JArray featured = JArray.Parse(o.SelectToken("top").ToString());

                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        if (featured.Count == 0)
                            IsGamesLoaded = true;
                        else
                        {
                            foreach (var arrayValue in featured)
                            {
                                var medium = new BitmapImage();
                                var name = arrayValue.SelectToken("game").SelectToken("name").ToString();
                                var channels = int.Parse(arrayValue.SelectToken("channels").ToString());

                                try
                                {
                                    medium = new BitmapImage(new Uri(arrayValue.SelectToken("game").SelectToken("box").SelectToken("medium").ToString()));
                                    medium.ImageFailed += medium_ImageFailed;
                                }

                                catch (Exception ex)
                                {
                                    Debug.WriteLine(ex);
                                }

                                GamesList.Add(new TopGame()
                                {
                                    channels = channels,
                                    game = new Game()
                                    {
                                        name = name,
                                        box = new Box
                                        {
                                            medium = medium
                                        }
                                    }
                                });
                            }
                        }
                        IsLoading = false;
                    });
                }
            }
            catch (Exception e)
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    MessageBox.Show("Network error occured: Couldn't load Top Games");
                });
                Debug.WriteLine(e);
            }
        }

        void medium_ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {
            ((BitmapImage)(sender)).UriSource = new Uri(TopGame.NO_BOX_ART, UriKind.Relative);
        }

        public void ClearList()
        {
            GamesList.Clear();
            IsLoading = false;
            IsGamesLoaded = false;
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
