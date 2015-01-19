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
                            string name = "";
                            var medium = new BitmapImage();

                            try
                            {
                                name = arrayValue.SelectToken("name").ToString();
                                medium = new BitmapImage(new Uri(arrayValue.SelectToken("box").SelectToken("medium").ToString()));
                            }

                            catch (Exception ex)
                            {
                                Debug.WriteLine(ex);
                            }

                            this.GameList.Add(new Game()
                            {
                                name = name,
                                box = new Box
                                {
                                    medium = medium
                                }
                            });
                        }
                        IsLoading = false;
                    });
                }
            }
            catch (Exception e)
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    MessageBox.Show("Network error occured " + e.Message);
                });
            }
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
                                string name = "";
                                string display_name = "";
                                string status = "";
                                var small = new BitmapImage();
                                var medium = new BitmapImage();
                                int viewers = 0;

                                try
                                {
                                    display_name = arrayValue.SelectToken("channel").SelectToken("display_name").ToString();
                                    name = arrayValue.SelectToken("channel").SelectToken("name").ToString();
                                    status = arrayValue.SelectToken("channel").SelectToken("status").ToString();
                                    small = new BitmapImage(new Uri(arrayValue.SelectToken("preview").SelectToken("small").ToString()));
                                    medium = new BitmapImage(new Uri(arrayValue.SelectToken("preview").SelectToken("medium").ToString()));
                                    viewers = int.Parse(arrayValue.SelectToken("viewers").ToString());
                                }

                                catch (Exception ex)
                                {
                                    Debug.WriteLine(ex);
                                }

                                StreamList.Add(new TwitchAPIHandler.Objects.Stream()
                                {
                                    channel = new Channel()
                                    {
                                        display_name = display_name,
                                        name = name,
                                        status = status
                                    },
                                    preview = new Preview()
                                    {
                                        medium = medium,
                                        small = small
                                    },
                                    viewers = viewers
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
                    MessageBox.Show("Network error occured " + e.Message);
                });
            }
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