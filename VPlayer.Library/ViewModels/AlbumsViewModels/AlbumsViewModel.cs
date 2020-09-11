using Prism.Events;
using System;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using VCore.Factories;
using VCore.Modularity.Events;
using VCore.Modularity.RegionProviders;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Interfaces.ViewModels;
using VPlayer.Core.Modularity.Regions;
using VPlayer.Library.Behaviors;
using VPlayer.Library.ViewModels.LibraryViewModels;
using VPlayer.Library.Views;

namespace VPlayer.Library.ViewModels.AlbumsViewModels
{
  public class AlbumsViewModel : PlayableItemsViewModel<AlbumsView, AlbumViewModel, Album>, IAlbumsViewModel
  {
    #region Constructors

    public AlbumsViewModel(
        IRegionProvider regionProvider,
        IViewModelsFactory viewModelsFactory,
        IStorageManager storageManager,
        LibraryCollection<AlbumViewModel, Album> libraryCollection, IEventAggregator eventAggregator)
        : base(regionProvider, viewModelsFactory, storageManager, libraryCollection)
    {
      EventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
    }

    #endregion Constructors

    #region Properties

    public override bool ContainsNestedRegions => false;
    public override string Header => "Albums";
    public override IQueryable<Album> LoadQuery => base.LoadQuery.Include(x => x.Artist).Include(x => x.Songs);
    public override string RegionName { get; protected set; } = RegionNames.LibraryContentRegion;
    public IEventAggregator EventAggregator { get; }

    #endregion Properties

    #region Initialize

    public override void Initialize()
    {
      base.Initialize();

      this.storageManager.ItemChanged.Where(x => x.Item.GetType() == typeof(Song)).Subscribe(SongChange);
      EventAggregator.GetEvent<ImageDeleteDoneEvent>().Subscribe(OnImageDeleted);
    }

    #endregion

    #region SongChange

    protected void SongChange(ItemChanged itemChanged)
    {
      if (itemChanged.Item is Song song)
      {
        if (LibraryCollection.WasLoaded && song.Album != null)
        {
          var album = LibraryCollection.Items.Single(x => x.ModelId == song.Album.Id);

          switch (itemChanged.Changed)
          {
            case Changed.Added:

              if (!album.Model.Songs.Contains(song))
              {
                album.Model.Songs.Add(song);
                album.RaisePropertyChanges();
              }

              break;
            case Changed.Removed:
              throw new NotImplementedException();
              break;

            case Changed.Updated:
              break;

            default:
              throw new ArgumentOutOfRangeException();
          }

          Application.Current.Dispatcher.Invoke(() => { album.RaisePropertyChanges(); });
        }
      }
    }

    #endregion

    protected override void OnDeleteItemChange(Album model)
    {
      if (!string.IsNullOrEmpty(model.AlbumFrontCoverFilePath))
      {
        try
        {
          EventAggregator.GetEvent<ImageDeleteRequestEvent>().Publish(new ImageDeleteDoneEventArgs()
          {
            Model = model,
          });
        }
        catch (Exception ex)
        {

          throw;
        }
      }
      else
      {
        base.OnDeleteItemChange(model);
      }

    }

    private void OnImageDeleted(ImageDeleteDoneEventArgs imageDeleteDoneEventArgs)
    {
      try
      {

        var model = ViewModels.SingleOrDefault(x => x.ModelId == imageDeleteDoneEventArgs.Model.Id)?.Model;

        if (model != null)
        {
          var path = imageDeleteDoneEventArgs.Model.AlbumFrontCoverFilePath;
          model.AlbumFrontCoverFilePath = null;

          Application.Current.Dispatcher.Invoke(() =>
          {
            base.OnDeleteItemChange(imageDeleteDoneEventArgs.Model);
            
            LibraryCollection.Recreate();
            
            regionProvider.RefreshView(Guid);

            GC.Collect();

            Task.Run(() =>
              {
                Thread.Sleep(2500);

                var fileNameLength = path.Split('\\').Last().Length;
                var directoryPath = path.Substring(0, path.Length - fileNameLength).Replace('/', '\\');

                try
                {
                  if (Directory.Exists(directoryPath))
                  {
                    Directory.Delete(directoryPath, true);

                    Logger.Logger.Instance.Log(Logger.MessageType.Inform, "PICTURE DELETED");
                  }
                  else
                  {
                    Logger.Logger.Instance.Log(Logger.MessageType.Inform, "NOT EXISTS");
                  }
                }
                catch (Exception ex)
                {
                  Logger.Logger.Instance.Log(Logger.MessageType.Error, directoryPath + " CANNOT BE DELETED");
                  Logger.Logger.Instance.Log(ex);
                  throw;
                }
              });
          });
        }
      }
      catch (Exception ex)
      {
        Logger.Logger.Instance.Log(ex);
        throw;
      }
    }
  }
}