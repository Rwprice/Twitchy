﻿using Newtonsoft.Json.Linq;
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
    public class TopGameStreamsViewModel : INotifyPropertyChanged
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

        public TopGameStreamsViewModel()
        {
            this.StreamList = new ObservableCollection<TwitchAPIHandler.Objects.Stream>();
            this.IsLoading = false;

        }

        public ObservableCollection<TwitchAPIHandler.Objects.Stream> StreamList
        {
            get;
            private set;
        }

        public void LoadPage(string gameName, int pageNumber)
        {
            if (pageNumber == 0)
            {
                this.StreamList.Clear();
                IsStreamsLoaded = false;
            }

            if (!IsStreamsLoaded)
            {
                IsLoading = true;
                HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(new Uri(String.Format(TwitchAPIHandler.Objects.Stream.TOP_STREAMS_FOR_GAME_PATH, gameName, 8 * pageNumber)));
                request.BeginGetResponse(new AsyncCallback(ReadCallback), request);
            }
        }

        private void ReadCallback(IAsyncResult asynchronousResult)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)asynchronousResult.AsyncState;
                HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(asynchronousResult);
                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    JToken o = JObject.Parse(reader.ReadLine());
                    JArray featured = JArray.Parse(o.SelectToken("streams").ToString());

                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        if (featured.Count == 0)
                            IsStreamsLoaded = true;
                        else
                        {
                            foreach (var arrayValue in featured)
                            {
                                var preview = new Preview();
                                var channel = new Channel();
                                var small = new BitmapImage();
                                var medium = new BitmapImage();

                                var viewers = int.Parse(arrayValue.SelectToken("viewers").ToString());
                                var display_name = arrayValue.SelectToken("channel").SelectToken("display_name").ToString();
                                var name = arrayValue.SelectToken("channel").SelectToken("name").ToString();
                                var status = arrayValue.SelectToken("channel").SelectToken("status").ToString();

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

                                preview = new Preview
                                {
                                    small = small,
                                    medium = medium
                                };

                                channel = new Channel
                                {
                                    display_name = display_name,
                                    name = name,
                                    status = status
                                };

                                StreamList.Add(new TwitchAPIHandler.Objects.Stream()
                                {
                                    channel = channel,
                                    preview = preview,
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
                    MessageBox.Show("Network error occured: Couldn't load Top Streams for this Game");
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