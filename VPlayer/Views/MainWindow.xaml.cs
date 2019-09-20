using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Windows.Storage;
using Windows.Storage.FileProperties;
using KeyListener;
using Prism.Events;
using VCore.Factories;
using VPlayer.AudioStorage;
using VPlayer.AudioStorage.Interfaces;
using VPlayer.AudioStorage.Models;
using VPlayer.ViewModels;
using VPlayer.WebPlayer.Views;

namespace VPlayer.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        //private void Test()
        //{
        //    AudioInfoDownloader.AudioInfoDownloader.Instance.SubdirectoryLoaded += Instance_SubdirectoryLoaded;
        //}

        //private void Instance_SubdirectoryLoaded(object sender, List<AudioStorage.Models.AudioInfo> e)
        //{
        //    Task.Run(() =>
        //    {
        //        using (IStorage storage = StorageManager.GetStorage())
        //        {
        //            storage.StoreData(e);
        //        }
        //    });

        //}

        //private async Task<MusicProperties> GetMusicProperties(string path)
        //{
        //    StorageFile file = await StorageFile.GetFileFromPathAsync(path);
        //    MusicProperties musicProperties = await file.Properties.GetMusicPropertiesAsync();

        //    return musicProperties;
        //}

        //private void ListViewItem_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        //{
        //    Test();
        //}

        //private async void ListViewItem_PreviewMouseDown1(object sender, MouseButtonEventArgs e)
        //{
        //    await StorageManager.GetStorage().ClearStorage();
        //}

        //private void ListViewItem_PreviewMouseDown2(object sender, MouseButtonEventArgs e)
        //{
        //    Task.Run(async () =>
        //    {
        //        List<Album> existingAlbums = null;


        //        using (IStorage storage = StorageManager.GetStorage())
        //        {
        //            existingAlbums = (from x in storage.Albums
        //                              where x.AlbumFrontCoverBLOB == null
        //                              select x).ToList();
        //        }

        //        List<Album> updatedAlbums = new List<Album>();

        //        foreach (var album in existingAlbums)
        //        {
        //            using (IStorage storage = StorageManager.GetStorage())
        //            {
        //                var updatedAlbum = await AudioInfoDownloader.AudioInfoDownloader.Instance.UpdateAlbum(album);

        //                if (updatedAlbum != null)
        //                    await storage.UpdateAlbum(updatedAlbum);
        //            }
        //        }

        //    });
        //}
    }
}
