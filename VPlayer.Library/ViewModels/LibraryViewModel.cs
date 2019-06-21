using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PropertyChanged;
using VPlayer.AudioStorage;
using VPlayer.AudioStorage.Interfaces;
using VPlayer.AudioStorage.Models;

namespace VPlayer.Library.ViewModels
{
    [AddINotifyPropertyChangedInterface]
    public class LibraryViewModel
    {
        public ObservableCollection<Album> Albums { get; set; } = new ObservableCollection<Album>();

        public static LibraryViewModel Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new LibraryViewModel();
                }

                return _instance;
            }
        }

        private static LibraryViewModel _instance;


        public LibraryViewModel()
        {
            StorageManager.AlbumStored += StorageManager_AlbumStored;
            StorageManager.AlbumUpdated += StorageManager_AlbumUpdated;
        }

        private void StorageManager_AlbumStored(object sender, Album e)
        {
            Albums.Add(e);
        }

        private void StorageManager_AlbumUpdated(object sender, Album e)
        {
            var album = (from x in Albums where x.AlbumId == e.AlbumId select x).First();
            album.UpdateAlbum(e);
        }

        private void LoadData()
        {
            using (IStorage storage = StorageManager.GetStorage())
            {
                foreach (var album in storage.Albums)
                    Albums.Add(album);
            }
        }

        public static void Init()
        {
            Instance.LoadData();
        }
    }
}
