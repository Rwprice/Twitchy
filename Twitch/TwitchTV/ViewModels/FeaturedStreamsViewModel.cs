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

namespace TwitchTV.ViewModels
{
    class FeaturedStreamsViewModel : INotifyPropertyChanged
    {
        private bool _isLoading = false;

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

        public FeaturedStreamsViewModel()
        {
            this.StreamList = new ObservableCollection<TwitchAPIHandler.Objects.Stream>();
            this.IsLoading = false;
        }

        public ObservableCollection<TwitchAPIHandler.Objects.Stream> StreamList
        {
            get;
            private set;
        }

        public void LoadPage()
        {
            IsLoading = true;
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(new Uri(String.Format(TwitchAPIHandler.Objects.Stream.FRONT_PAGE_STREAMS_PATH, 0)));
            request.BeginGetResponse(new AsyncCallback(ReadCallback), request);
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
                    JArray featured = JArray.Parse(o.SelectToken("featured").ToString());

                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        foreach (var arrayValue in featured)
                        {
                            JToken stream = arrayValue.SelectToken("stream");
                            var preview = new Preview();
                            var channel = new Channel();
                            var small = new BitmapImage();
                            var medium = new BitmapImage();

                            var viewers = int.Parse(stream.SelectToken("viewers").ToString());
                            var display_name = stream.SelectToken("channel").SelectToken("display_name").ToString();
                            var name = stream.SelectToken("channel").SelectToken("name").ToString();
                            var status = "";
                            var logo = stream.SelectToken("channel").SelectToken("logo").ToString();

                            try
                            {
                                status = stream.SelectToken("channel").SelectToken("status").ToString();
                            }

                            catch
                            {
                                if (status == "")
                                {
                                    status = display_name;
                                }
                            }

                            try
                            {
                                small = new BitmapImage(new Uri(stream.SelectToken("preview").SelectToken("small").ToString()));
                                medium = new BitmapImage(new Uri(stream.SelectToken("preview").SelectToken("medium").ToString()));
                            }

                            catch { }

                            preview = new Preview
                            {
                                small = small,
                                medium = medium
                            };

                            channel = new Channel
                            {
                                display_name = display_name,
                                name = name,
                                status = status,
                                logoUri = logo
                            };

                            StreamList.Add(new TwitchAPIHandler.Objects.Stream()
                            {
                                channel = channel,
                                preview = preview,
                                viewers = viewers
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
                    MessageBox.Show("Network error occured: Couldn't load Front Page");
                });
                Debug.WriteLine(e);
            }
        }

        void ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {

        }

        public void ClearList()
        {
            StreamList.Clear();
            IsLoading = false;
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
