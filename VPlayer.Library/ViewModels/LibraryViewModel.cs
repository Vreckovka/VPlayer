using System;
using System.Windows.Input;
using VPlayer.AudioStorage;
using VPlayer.AudioStorage.Models;
using VirtualListWithPagingTechnique.VirtualLists;
using VPlayer.Core.ViewModels;
using Prism.Ioc;
using Prism.Regions;
using VPlayer.Core;
using VPlayer.Library.Annotations;
using VPlayer.Library.ViewModels.AlbumsViewModels;
using VPlayer.Library.ViewModels.ArtistsViewModels;
using VPlayer.Library.ViewModels.LibraryViewModels;
using VPlayer.Library.ViewModels.LibraryViewModels.ArtistsViewModels;
using VPlayer.Library.Views;

namespace VPlayer.Library.ViewModels
{
    public class LibraryViewModel : ModuleViewModel 
    {
        private readonly AlbumsViewModel albumsViewModel;

        public LibraryViewModel(
            ArtistsViewModel artistsViewModel,
            [NotNull] AlbumsViewModel albumsViewModel)
        {
            this.albumsViewModel = albumsViewModel ?? throw new ArgumentNullException(nameof(albumsViewModel));

            StorageManager.AlbumStored += StorageManager_AlbumStored;
            StorageManager.AlbumUpdated += StorageManager_AlbumUpdated;
            StorageManager.AlbumRemoved += StorageManager_AlbumRemoved;
            StorageManager.StorageCleared += StorageManagerStorageCleared;

            ActualCollectionViewModel = artistsViewModel;
        }

        #region FinderKeyDown

        private ActionCommand<string> finderKeyDown;
        public ICommand FinderKeyDown
        {
            get
            {
                if (finderKeyDown == null)
                {
                    finderKeyDown = new ActionCommand<string>(OnFinderKeyDown);
                }

                return finderKeyDown;
            }
        }

        private void OnFinderKeyDown(string phrase)
        {
            ActualCollectionViewModel.Filter(phrase);
        }

        #endregion

        #region ShowAlbums

        private ActionCommand showAlbums;
        public ICommand ShowAlbums
        {
            get
            {
                if (showAlbums == null)
                {
                    showAlbums = new ActionCommand(OnShowAlbums);
                }

                return showAlbums;
            }
        }

        private void OnShowAlbums()
        {
            ActualCollectionViewModel = albumsViewModel;
        }

        #endregion


        #region ActualCollectionViewModel

        private ILibraryCollection<IPlayableViewModel> actualCollectionViewModel;
        public ILibraryCollection<IPlayableViewModel> ActualCollectionViewModel
        {
            get { return actualCollectionViewModel; }
            set
            {
                if (value != actualCollectionViewModel)
                {
                    actualCollectionViewModel = value;

                    RaisePropertyChanged();
                }
            }
        }

        #endregion

        public override void OnInitialized(IContainerProvider containerProvider)
        {
            var regionManager = containerProvider.Resolve<IRegionManager>();

            regionManager.RegisterViewWithRegion("LibraryRegion", typeof(LibraryView));
        }

        private void StorageManagerStorageCleared(object sender, EventArgs e)
        {
            //try
            //{
            //    Application.Current.Dispatcher.Invoke(() => { Albums.Clear(); });
            //}
            //catch (Exception ex)
            //{
            //    Logger.Logger.Instance.Log(Logger.MessageType.Error, ex.Message);
            //}
        }

        private void StorageManager_AlbumRemoved(object sender, Album e)
        {
            //try
            //{
            //    Application.Current.Dispatcher.Invoke(() =>
            //    {
            //        var album = (from x in Albums where x.AlbumId == e.AlbumId select x).FirstOrDefault();

            //        if (album == null)
            //        {
            //            album = (from x in Albums
            //                     where x.Name == e.Name
            //                     where x.Artist.Name == e.Artist.Name
            //                     select x).FirstOrDefault();
            //        }

            //        if (album != null)
            //            Albums.Remove(album);
            //        else
            //            Logger.Logger.Instance.Log(Logger.MessageType.Warning, $"Album {e.Name} was not removed localy");

            //    });
            //}
            //catch (Exception ex)
            //{
            //    Logger.Logger.Instance.Log(Logger.MessageType.Error, ex.Message);
            //}
        }

        private void StorageManager_AlbumStored(object sender, Album e)
        {

            //try
            //{
            //    Application.Current.Dispatcher.Invoke(() =>
            //    {
            //        Albums.Add(e);
            //    });
            //}
            //catch (Exception ex)
            //{
            //    Logger.Logger.Instance.Log(Logger.MessageType.Error, ex.Message);
            //}
        }

        private void StorageManager_AlbumUpdated(object sender, Album e)
        {
            //try
            //{
            //    Application.Current.Dispatcher.Invoke(() =>
            //       {
            //           var album = (from x in Albums where x.AlbumId == e.AlbumId select x).FirstOrDefault();

            //           if (album == null)
            //           {
            //               album = (from x in Albums
            //                        where x.Name == e.Name
            //                        where x.Artist.Name == e.Artist.Name
            //                        select x).FirstOrDefault();
            //           }

            //           if (album != null)
            //               album.UpdateAlbum(e);
            //           else
            //               Logger.Logger.Instance.Log(Logger.MessageType.Warning, $"Album {e.Name} was not updated localy");

            //       });
            //}
            //catch (Exception ex)
            //{
            //    Logger.Logger.Instance.Log(Logger.MessageType.Error, ex.Message);
            //}
        }
    }
}
