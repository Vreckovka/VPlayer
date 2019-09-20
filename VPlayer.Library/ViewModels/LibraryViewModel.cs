using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using VPlayer.AudioStorage;
using VPlayer.AudioStorage.Models;
using VCore;
using VCore.Annotations;
using VCore.Factories;
using VCore.Modularity.RegionProviders;
using VCore.ViewModels;
using VCore.ViewModels.Navigation;
using VPlayer.AudioStorage.AudioDatabase;
using VPlayer.AudioStorage.Interfaces;
using VPlayer.Core.Modularity.Regions;
using VPlayer.Library.ViewModels.LibraryViewModels;
using VPlayer.Library.ViewModels.LibraryViewModels.ArtistsViewModels;
using VPlayer.Library.Views;

namespace VPlayer.Library.ViewModels
{
  public class LibraryViewModel : RegionViewModel<LibraryView>, INavigationItem
  {
    private readonly IViewModelsFactory viewModelsFactory;
    private readonly NavigationViewModel navigationViewModel;
    private readonly IStorage storage;

    public LibraryViewModel(IRegionProvider regionProvider, 
      [VCore.Annotations.NotNull] IViewModelsFactory viewModelsFactory,
      [VCore.Annotations.NotNull] NavigationViewModel navigationViewModel, [NotNull] IStorage storage) : base(regionProvider)
    {
      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));
      this.navigationViewModel = navigationViewModel ?? throw new ArgumentNullException(nameof(navigationViewModel));
      this.storage = storage ?? throw new ArgumentNullException(nameof(storage));

      StorageManager.AlbumStored += StorageManager_AlbumStored;
      StorageManager.AlbumUpdated += StorageManager_AlbumUpdated;
      StorageManager.AlbumRemoved += StorageManager_AlbumRemoved;
      StorageManager.StorageCleared += StorageManagerStorageCleared;
    }

    public override string RegionName => RegionNames.WindowsPlayerContentRegion;
    public override bool ContainsNestedRegions => true;
    public string Header => "Library";

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
      //ActualCollectionViewModel.Filter(phrase);
    }

    #endregion

    #region FilesDropped

    private ActionCommand<object> filesDropped;
    public ICommand FilesDropped
    {
      get
      {
        if (filesDropped == null)
        {
          filesDropped = new ActionCommand<object>(OnFilesDropped);
        }

        return filesDropped;
      }
    }

    private async void OnFilesDropped(object files)
    {
      IDataObject data = files as IDataObject;
      if (null == data) return;

      var draggedFiles = data.GetData(DataFormats.FileDrop) as IEnumerable<string>;

      var asd = await AudioInfoDownloader.AudioInfoDownloader.Instance.GetAudioInfo(draggedFiles.First());
      await storage.StoreData(asd);
    }

    #endregion

    public override void Initialize()
    {
      base.Initialize();

      var libraryViewModel = viewModelsFactory.Create<ArtistsViewModel>();

      libraryViewModel.IsActive = true;
      navigationViewModel.Items.Add(libraryViewModel);

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
