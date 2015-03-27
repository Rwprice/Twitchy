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

namespace TwitchTV.ViewModels
{
    class NotificationsViewModel : INotifyPropertyChanged
    {
        private bool _isLoading = false;
        private bool _isNotificationsLoaded = false;

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

        public bool IsNotificationsLoaded
        {
            get
            {
                return _isNotificationsLoaded;
            }
            set
            {
                _isNotificationsLoaded = value;
                NotifyPropertyChanged("IsNotificationsLoaded");

            }
        }

        public NotificationsViewModel()
        {
            this.NotificationsList = new ObservableCollection<TwitchAPIHandler.Objects.Notification>();
            this.IsLoading = false;

        }

        public ObservableCollection<TwitchAPIHandler.Objects.Notification> NotificationsList
        {
            get;
            private set;
        }

        public void LoadPage(string userName, int pageNumber)
        {
            if (pageNumber == 0)
            {
                IsNotificationsLoaded = false;
            }

            if (!IsNotificationsLoaded)
            {
                IsLoading = true;
                HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(new Uri(String.Format(TwitchAPIHandler.Objects.Stream.GET_ALL_FOLLOWED_STREAMS, userName, 8 * pageNumber)));
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
                    JArray featured = JArray.Parse(o.SelectToken("follows").ToString());

                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        if (featured.Count == 0)
                            IsNotificationsLoaded = true;
                        else
                        {
                            foreach (var arrayValue in featured)
                            {
                                var display_name = arrayValue.SelectToken("channel").SelectToken("display_name").ToString();
                                var name = arrayValue.SelectToken("channel").SelectToken("name").ToString();

                                var channel = new TwitchAPIHandler.Objects.Notification() { display_name = display_name, name = name, notify = false };

                                if(!NotificationsList.Contains(channel))
                                    this.NotificationsList.Add(channel);
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
                    MessageBox.Show("Network error occured: Couldn't load Followed Streams");
                });
                Debug.WriteLine(e);
            }
        }

        public void ClearList()
        {
            NotificationsList.Clear();
            IsLoading = false;
            IsNotificationsLoaded = false;
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
