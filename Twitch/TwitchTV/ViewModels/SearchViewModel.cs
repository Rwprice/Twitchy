using Newtonsoft.Json.Linq;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Xml;
using TwitchAPIHandler.Objects;

namespace Twitchy.ViewModels
{
    public class SearchViewModel : INotifyPropertyChanged
    {
        private bool _isLoading = false;
        private bool _isStreamsLoaded = false;

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

        public bool IsStreamsLoaded
        {
            get
            {
                return _isStreamsLoaded;
            }
            set
            {
                _isStreamsLoaded = value;
                NotifyPropertyChanged("IsStreamsLoaded");

            }
        }

        public SearchViewModel()
        {
            this.StreamList = new ObservableCollection<TwitchAPIHandler.Objects.Stream>();
            this.GameList = new ObservableCollection<Game>();
            this.IsLoading = false;
            this.IsStreamsLoaded = false;
        }

        public ObservableCollection<TwitchAPIHandler.Objects.Stream> StreamList
        {
            get;
            private set;
        }

        public ObservableCollection<Game> GameList
        {
            get;
            private set;
        }

        public void SearchGames(string gameName, int pageNumber)
        {
            if (pageNumber == 0) this.GameList.Clear();

            IsLoading = true;
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(new Uri(String.Format(TwitchAPIHandler.Objects.Stream.SEARCH_GAME_PATH, gameName, 8 * pageNumber)));
            request.BeginGetResponse(new AsyncCallback(ReadGamesCallback), request);
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
                    JArray games = JArray.Parse(o.SelectToken("games").ToString());

                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        foreach (var arrayValue in games)
                        {
                            var medium = new BitmapImage();
                            string name = arrayValue.SelectToken("name").ToString();

                            try
                            {
                                medium = new BitmapImage(new Uri(arrayValue.SelectToken("box").SelectToken("medium").ToString()));
                                medium.ImageFailed += game_ImageFailed;
                            }

                            catch (Exception ex)
                            {
                                Debug.WriteLine(ex);
                            }

                            var game = new Game()
                            {
                                name = name,
                                box = new Box
                                {
                                    medium = medium
                                }
                            };

                            if (!this.GameList.Contains(game))
                                this.GameList.Add(game);
                        }
                        IsLoading = false;
                    });
                }
            }
            catch (Exception e)
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    MessageBox.Show("Network error occured: Couldn't load the search results");
                });
                Debug.WriteLine(e);
            }
        }

        private void game_ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {
            ((BitmapImage)(sender)).UriSource = new Uri(TopGame.NO_BOX_ART, UriKind.Relative);
        }

        public void SearchStreams(string streamName, int pageNumber)
        {
            if (pageNumber == 0)
            {
                this.StreamList.Clear();
                IsStreamsLoaded = false;
            }

            if (!IsStreamsLoaded)
            {
                IsLoading = true;
                HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(new Uri(String.Format(TwitchAPIHandler.Objects.Stream.SEARCH_STREAM_PATH, streamName, 8 * pageNumber)));
                request.BeginGetResponse(new AsyncCallback(ReadStreamsCallback), request);
            }
        }

        private void ReadStreamsCallback(IAsyncResult asynchronousResult)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)asynchronousResult.AsyncState;
                HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(asynchronousResult);
                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    JToken o = JObject.Parse(reader.ReadLine());
                    JArray streams = JArray.Parse(o.SelectToken("streams").ToString());

                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        if (streams.Count == 0)
                            IsStreamsLoaded = true;
                        else
                        {
                            foreach (var arrayValue in streams)
                            {
                                var display_name = arrayValue.SelectToken("channel").SelectToken("display_name").ToString();
                                var name = arrayValue.SelectToken("channel").SelectToken("name").ToString();
                                int viewers = int.Parse(arrayValue.SelectToken("viewers").ToString());
                                var small = new BitmapImage();
                                var medium = new BitmapImage();
                                var status = "";
                                var logo = arrayValue.SelectToken("channel").SelectToken("logo").ToString();

                                try
                                {
                                    status = arrayValue.SelectToken("channel").SelectToken("status").ToString();
                                }

                                catch (Exception ex)
                                {
                                    Debug.WriteLine(ex);
                                    if (status == "")
                                    {
                                        status = display_name;
                                    }
                                }

                                try
                                {
                                    small = new BitmapImage(new Uri(arrayValue.SelectToken("preview").SelectToken("small").ToString()));
                                    medium = new BitmapImage(new Uri(arrayValue.SelectToken("preview").SelectToken("medium").ToString()));

                                    small.ImageFailed += ImageFailed;
                                    medium.ImageFailed += ImageFailed;
                                }

                                catch (Exception ex)
                                {
                                    Debug.WriteLine(ex);
                                }

                                var stream = new TwitchAPIHandler.Objects.Stream()
                                {
                                    channel = new Channel()
                                    {
                                        display_name = display_name,
                                        name = name,
                                        status = status,
                                        logoUri = logo,
                                    },
                                    preview = new Preview()
                                    {
                                        medium = medium,
                                        small = small
                                    },
                                    viewers = viewers
                                };

                                if (!StreamList.Contains(stream))
                                    StreamList.Add(stream);
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
                    MessageBox.Show("Network error occured: Couldn't load the search results");
                });
                Debug.WriteLine(e);
            }
        }

        void ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {

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