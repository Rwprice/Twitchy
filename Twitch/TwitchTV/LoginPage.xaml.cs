using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using TwitchAPIHandler;
using TwitchAPIHandler.Objects;

namespace TwitchTV
{
    public partial class LoginPage : PhoneApplicationPage
    {
        public LoginPage()
        {
            InitializeComponent();
            this.WebBrowser.Loaded += WebBrowser_Loaded;
        }

        void WebBrowser_Loaded(object sender, RoutedEventArgs e)
        {
            this.WebBrowser.Navigate(new Uri("https://api.twitch.tv/kraken/oauth2/authorize?response_type=token&client_id=b4v9ttxqtldlobe5jswfqdhrmzp52hi&redirect_uri=http://localhost&scope=user_read chat_login"));
        }
    }
}