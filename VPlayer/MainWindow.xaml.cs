using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Ninject;
using VPlayer.AudioStorage;
using VPlayer.AudioStorage.AudioDatabase;
using VPlayer.AudioStorage.Interfaces;
using VPlayer.AudioStorage.Models;
using VPlayer.Other;
using VPlayer.Views;
using VPlayer.WebPlayer;
using VPlayer.WebPlayer.Views;
using VPlayer.WindowsPlayer.Views;


namespace VPlayer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private WebPlayerPage _webPlayerPage = new WebPlayerPage();
        private WindowsPlayerPage _windowsPlayerPage = new WindowsPlayerPage();
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Test()
        {
            //var albums = await AudioInfoDownloader.AudioInfoDownloader.Instance.GetAudioInfosFromWindowsDirectoryAsync("D:\\Hudba\\Metallica", true);
            //await StorageManager.GetStorage().StoreData(albums);


            //AudioInfoDownloader.AudioInfoDownloader.Instance.GetAudioInfosFromDirectory(Directory.Text, true);


            AudioInfoDownloader.AudioInfoDownloader.Instance.SubdirectoryLoaded += Instance_SubdirectoryLoaded;

        }

        private void Instance_SubdirectoryLoaded(object sender, List<AudioStorage.Models.AudioInfo> e)
        {
            Task.Run(() =>
            {
                using (IStorage storage = StorageManager.GetStorage())
                {
                    storage.StoreData(e);
                }
            });

        }

        private void ListViewItem_Click(object sender, MouseButtonEventArgs e)
        {
            if (((ListViewItem)sender).Content.Equals("Internet player"))
            {
                Frame_Players.Content = _webPlayerPage;
            }
            else
            {
                Frame_Players.Content = _windowsPlayerPage;
            }
        }

        private async Task<MusicProperties> GetMusicProperties(string path)
        {
            StorageFile file = await StorageFile.GetFileFromPathAsync(path);
            MusicProperties musicProperties = await file.Properties.GetMusicPropertiesAsync();

            return musicProperties;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Frame_Players.Content = _windowsPlayerPage;
        }

        private void ListViewItem_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            Test();
        }

        private async void ListViewItem_PreviewMouseDown1(object sender, MouseButtonEventArgs e)
        {
            await StorageManager.GetStorage().ClearStorage();
        }

        private void ListViewItem_PreviewMouseDown2(object sender, MouseButtonEventArgs e)
        {
            Task.Run(async () =>
            {
                List<Album> existingAlbums = null;


                using (IStorage storage = StorageManager.GetStorage())
                {
                    existingAlbums = (from x in storage.Albums
                                      where x.AlbumFrontCoverBLOB == null
                                      select x).ToList();
                }

                List<Album> updatedAlbums = new List<Album>();

                foreach (var album in existingAlbums)
                {
                    using (IStorage storage = StorageManager.GetStorage())
                    {
                        var updatedAlbum = await AudioInfoDownloader.AudioInfoDownloader.Instance.UpdateAlbum(album);

                        if (updatedAlbum != null)
                            await storage.UpdateAlbum(updatedAlbum);
                    }
                }

            });
        }
    }
}
