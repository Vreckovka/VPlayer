using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using PropertyChanged;
using VPlayer.AudioStorage;
using VPlayer.AudioStorage.Interfaces;
using VPlayer.AudioStorage.Models;
using VPlayer.Library.VirtualList;
using VirtualListWithPagingTechnique.VirtualLists;

namespace VPlayer.Library.ViewModels
{
    [AddINotifyPropertyChangedInterface]
    public class LibraryViewModel
    {
        public VirtualList<Album> Albums
        {
            get { return new VirtualList<Album>(new AlbumGenerator()); }
        }

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
            StorageManager.AlbumRemoved += StorageManager_AlbumRemoved;

            StorageManager.StorageCleared += StorageManagerStorageCleared;
        }

        private void StorageManagerStorageCleared(object sender, EventArgs e)
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() => { Albums.Clear(); });
            }
            catch (Exception ex)
            {
                Logger.Logger.Instance.Log(Logger.MessageType.Error, ex.Message);
            }

        }

        private void StorageManager_AlbumRemoved(object sender, Album e)
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var album = (from x in Albums where x.AlbumId == e.AlbumId select x).FirstOrDefault();

                    if (album == null)
                    {
                        album = (from x in Albums
                                 where x.Name == e.Name
                                 where x.Artist.Name == e.Artist.Name
                                 select x).FirstOrDefault();
                    }

                    if (album != null)
                        Albums.Remove(album);
                    else
                        Logger.Logger.Instance.Log(Logger.MessageType.Warning, $"Album {e.Name} was not removed localy");

                });
            }
            catch (Exception ex)
            {
                Logger.Logger.Instance.Log(Logger.MessageType.Error, ex.Message);
            }
        }

        private void StorageManager_AlbumStored(object sender, Album e)
        {

            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Albums.Add(e);
                });
            }
            catch (Exception ex)
            {
                Logger.Logger.Instance.Log(Logger.MessageType.Error, ex.Message);
            }
        }

        private void StorageManager_AlbumUpdated(object sender, Album e)
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                   {
                       var album = (from x in Albums where x.AlbumId == e.AlbumId select x).FirstOrDefault();

                       if (album == null)
                       {
                           album = (from x in Albums
                                    where x.Name == e.Name
                                    where x.Artist.Name == e.Artist.Name
                                    select x).FirstOrDefault();
                       }

                       if (album != null)
                           album.UpdateAlbum(e);
                       else
                           Logger.Logger.Instance.Log(Logger.MessageType.Warning, $"Album {e.Name} was not updated localy");

                   });
            }
            catch (Exception ex)
            {
                Logger.Logger.Instance.Log(Logger.MessageType.Error, ex.Message);
            }
        }



        private void LoadData()
        {

        }

        public static void Init()
        {
            Instance.LoadData();
        }
    }
}
