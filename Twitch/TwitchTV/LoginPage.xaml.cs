using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using TwitchAPIHandler;
using TwitchAPIHandler.Objects;
using Windows.Storage;
using Windows.Storage.Streams;

namespace TwitchTV
{
    public partial class LoginPage : PhoneApplicationPage
    {
        public LoginPage()
        {
            InitializeComponent();
            this.WebBrowser.Loaded += WebBrowser_Loaded;
            this.WebBrowser.Navigating += WebBrowser_Navigating;
        }

        async void WebBrowser_Navigating(object sender, NavigatingEventArgs e)
        {
            if (e.Uri.Host == "localhost")
            {
                string token = e.Uri.AbsoluteUri.Substring(e.Uri.AbsoluteUri.IndexOf('=') + 1);
                token = token.Remove(token.IndexOf('&'));

                var user = await User.GetUserFromOauth(token);

                User.SaveUser(user);

                App.ViewModel.user = user;

                await this.WebBrowser.ClearCookiesAsync();
                await this.WebBrowser.ClearInternetCacheAsync();

                NavigationService.GoBack();
            }
        }

        void WebBrowser_Loaded(object sender, RoutedEventArgs e)
        {
            this.WebBrowser.Navigate(new Uri(AccessToken.GetAuthorizationTokenURI()));
        }
    }
}