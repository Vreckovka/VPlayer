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
using VPlayer.AudioStorage.Interfaces;
using VPlayer.Other;
using VPlayer.Views;
using VPlayer.WebPlayer;
using VPlayer.WebPlayer.Views;
using VPlayer.WindowsPlayer.Views;


namespace VPlayer
{
    //TODO:Create pages
    //TODO: Library

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
            Test();

        }

        private async Task Test()
        {
            //await StorageManager.GetStorage().ClearStorage();

            //var album = await AudioInfoDownloader.AudioInfoDownloader.Instance.GetAudioInfosFromWindowsDirectoryAsync("D:\\Hudba\\Drowning pool\\Drowning Pool - Hellelujah (2016)");

            //using (IStorage storage = StorageManager.GetStorage())
            //{
            //    await storage.StoreData(album);
            //}

            //album = await AudioInfoDownloader.AudioInfoDownloader.Instance.GetAudioInfosFromWindowsDirectoryAsync("D:\\Hudba\\Drowning pool\\Drowning_Pool-Desensitize-2004-h8me");

            //using (IStorage storage = StorageManager.GetStorage())
            //{
            //    await storage.StoreData(album);
            //}

            //album = await AudioInfoDownloader.AudioInfoDownloader.Instance.GetAudioInfosFromWindowsDirectoryAsync("D:\\Hudba\\Drowning pool\\Drowning_Pool-Full_Circle-2007-h8me");

            //using (IStorage storage = StorageManager.GetStorage())
            //{
            //    await storage.StoreData(album);
            //}

            // await AudioStorage.StorageManager.Instance.ClearStorage();

            // var audioInfos = await AudioInfoDownloader.AudioInfoDownloader.Instance.GetAudioInfosFromWindowsDirectoryAsync("D:\\Hudba\\Drowning pool\\Drowning Pool - Hellelujah (2016)");
            // await AudioStorage.StorageManager.Instance.StoreData(audioInfos);
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
    }
}
