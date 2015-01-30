using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using TwitchAPIHandler.Objects;
using TwitchTV.Resources;
using Windows.Storage;
using Windows.Storage.Streams;

namespace TwitchTV.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public Stream stream { get; set; }
        public TopGame curTopGame { get; set; }
        public User user { get; set; }

        public bool AutoJoinChat = false;
        public bool LockLandscape = false;
        public bool LiveTilesEnabled = false;

        public MainViewModel()
        {

        }

        public async void SaveSettings()
        {
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            StorageFile textFile = await localFolder.CreateFileAsync("settingV2", CreationCollisionOption.ReplaceExisting);

            using (IRandomAccessStream textStream = await textFile.OpenAsync(FileAccessMode.ReadWrite))
            {
                using (DataWriter textWriter = new DataWriter(textStream))
                {
                    textWriter.WriteString(AutoJoinChat.ToString() + "\n" + LockLandscape.ToString() + "\n" + LiveTilesEnabled.ToString());
                    await textWriter.StoreAsync();
                }
            }
        }

        public async void LoadSettings()
        {
            try
            {
                string contents;

                StorageFolder localFolder = ApplicationData.Current.LocalFolder;
                StorageFile textFile = await localFolder.GetFileAsync("settingV2");

                using (IRandomAccessStream textStream = await textFile.OpenReadAsync())
                {
                    using (DataReader textReader = new DataReader(textStream))
                    {
                        uint textLength = (uint)textStream.Size;
                        await textReader.LoadAsync(textLength);
                        contents = textReader.ReadString(textLength);
                    }
                }

                string[] lines = contents.Split('\n');

                bool.TryParse(lines[0], out AutoJoinChat);
                bool.TryParse(lines[1], out LockLandscape);
                bool.TryParse(lines[2], out LiveTilesEnabled);
            }

            catch { }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}