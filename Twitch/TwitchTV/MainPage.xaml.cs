using System;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using TwitchAPIHandler.Objects;

namespace TwitchTV
{
    public partial class MainPage : PhoneApplicationPage
    {
        // Constructor
        public MainPage()
        {
            InitializeComponent();

            // Set the data context of the listbox control to the sample data
            DataContext = App.ViewModel;
        }

        // Load data for the ViewModel Items
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            if (!App.ViewModel.IsDataLoaded)
            {
                App.ViewModel.LoadData();
            }

            //Put Breakpoints in to see the functionality
            //This won't stay here just a test
            AccessToken token = await AccessToken.GetToken("trick2g");
            M3U8Playlist playlist = await M3U8Playlist.GetStreamPlaylist("trick2g", token);
            StreamFileList indexList = await StreamFileList.UpdateStreamFileList(playlist, "Low");
        }
    }
}