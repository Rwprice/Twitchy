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
using System.Windows.Data;
using Twitchy.ViewModels;

namespace TwitchTV
{
    public partial class TopGamePage : PhoneApplicationPage
    {
        private int _pageNumber = 0;
        private int _offsetKnob = 1;
        TopGameStreamsViewModel _viewModel;  

        public TopGamePage()
        {
            InitializeComponent();
            _viewModel = new TopGameStreamsViewModel();
            this.TGHeader.Header = App.ViewModel.curTopGame.game.name;
            TopStreamsList.ItemRealized += resultList_ItemRealized;
            this.Loaded += new RoutedEventHandler(TopGamePage_Loaded);
        }

        private void TopGamePage_Loaded(object sender, RoutedEventArgs e)
        {
            this.TopStreamsList.ItemsSource = _viewModel.StreamList;
            var progressIndicator = SystemTray.ProgressIndicator;
            if (progressIndicator != null)
            {
                return;
            }

            progressIndicator = new ProgressIndicator();

            SystemTray.SetProgressIndicator(this, progressIndicator);

            Binding binding = new Binding("IsLoading") { Source = _viewModel };
            BindingOperations.SetBinding(
                progressIndicator, ProgressIndicator.IsVisibleProperty, binding);

            binding = new Binding("IsLoading") { Source = _viewModel };
            BindingOperations.SetBinding(
                progressIndicator, ProgressIndicator.IsIndeterminateProperty, binding);

            progressIndicator.Text = "Loading streams...";

            _pageNumber = 0;

            _viewModel.LoadPage(App.ViewModel.curTopGame.game.name, _pageNumber++);
        }

        private void resultList_ItemRealized(object sender, ItemRealizationEventArgs e)
        {
            if (!_viewModel.IsLoading && TopStreamsList.ItemsSource != null && TopStreamsList.ItemsSource.Count >= _offsetKnob)
            {
                if (e.ItemKind == LongListSelectorItemKind.Item)
                {
                    if ((e.Container.Content as Stream).Equals(TopStreamsList.ItemsSource[TopStreamsList.ItemsSource.Count - _offsetKnob]))
                    {
                        Debug.WriteLine("Searching for {0}", _pageNumber);
                        _viewModel.LoadPage(App.ViewModel.curTopGame.game.name, _pageNumber++);
                    }
                }
            }
        }

        private void TopStreamsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (((Stream)((LongListSelector)sender).SelectedItem) != null)
            {
                App.ViewModel.stream = ((Stream)((LongListSelector)sender).SelectedItem);
                NavigationService.Navigate(new Uri("/PlayerPage.xaml", UriKind.RelativeOrAbsolute));
            }
        }
    }
}