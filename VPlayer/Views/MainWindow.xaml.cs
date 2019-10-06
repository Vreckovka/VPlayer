using System;
using System.Windows;
using VPlayer.AudioStorage.Interfaces.Storage;

namespace VPlayer.Views
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    #region Fields

    private readonly IStorageManager storageManager;

    #endregion Fields

    #region Constructors

    public MainWindow(IStorageManager storageManager)
    {
      this.storageManager = storageManager ?? throw new ArgumentNullException(nameof(storageManager));
      InitializeComponent();
    }

    #endregion Constructors

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

    #region Methods

    private async void Button_Click(object sender, RoutedEventArgs e)
    {
      await storageManager.ClearStorage();
    }

    #endregion Methods

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