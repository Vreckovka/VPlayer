using Logger;
using Prism.Events;
using System;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using VCore.Annotations;
using VCore.Factories;
using VCore.Helpers;
using VCore.Modularity.Events;
using VCore.Modularity.RegionProviders;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Interfaces.ViewModels;
using VPlayer.Core.Messages.ImageDelete;
using VPlayer.Core.Modularity.Regions;
using VPlayer.Library.ViewModels.LibraryViewModels;
using VPlayer.Library.Views;

namespace VPlayer.Library.ViewModels.AlbumsViewModels
{
  public class AlbumsViewModel : PlayableItemsViewModel<AlbumsView, AlbumViewModel, Album>, IAlbumsViewModel
  {
    private readonly ILogger logger;

    #region Constructors

    public AlbumsViewModel(
        IRegionProvider regionProvider,
        IViewModelsFactory viewModelsFactory,
        IStorageManager storageManager,
        LibraryCollection<AlbumViewModel, Album> libraryCollection, 
        IEventAggregator eventAggregator,
        ILogger logger)
        : base(regionProvider, viewModelsFactory, storageManager, libraryCollection)
    {
      this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
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

    #region Methods

    #region Initialize

    public override void Initialize()
    {
      base.Initialize();

      this.storageManager.SubscribeToItemChange<Song>(SongChange).DisposeWith(this);

    }

    #endregion

    #region SongChange

    protected void SongChange(ItemChanged<Song> itemChanged)
    {
      var song = itemChanged.Item;

      if (LibraryCollection.WasLoaded && song.Album != null)
      {
        var album = LibraryCollection.Items.SingleOrDefault(x => x.ModelId == song.Album.Id);

        if (album != null)
        {
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

    #region OnUpdateItemChange

    protected override void OnUpdateItemChange(Album model)
    {
      base.OnUpdateItemChange(model);
    }

    #endregion

    #region OnDeleteItemChange

    protected override void OnDeleteItemChange(Album model)
    {
      if (!string.IsNullOrEmpty(model.AlbumFrontCoverFilePath))
      {
        try
        {
          var viewModel = LibraryCollection.Items.SingleOrDefault(x => x.ModelId == model.Id);

          if (viewModel != null)
          {
            viewModel.PublishDeleteImage(OnImageDeleted);
          }

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

    #endregion

    #region OnImageDeleted

    private void OnImageDeleted(ImageDeleteDoneEventArgs imageDeleteDoneEventArgs)
    {
      try
      {
        if (imageDeleteDoneEventArgs.Result)
        {
          var model = ViewModels.SingleOrDefault(x => x.ModelId == imageDeleteDoneEventArgs.Model.Id)?.Model;

          if (model != null)
          {
            var path = imageDeleteDoneEventArgs.Model.AlbumFrontCoverFilePath;
            model.AlbumFrontCoverFilePath = null;

            Application.Current.Dispatcher.Invoke(() =>
            {
              base.OnDeleteItemChange(imageDeleteDoneEventArgs.Model);

              RecreateCollection();

              Task.Run(() =>
              {
                //Wait for windows
                Thread.Sleep(2500);

                var fileNameLength = path.Split('\\').Last().Length;
                var directoryPath = path.Substring(0, path.Length - fileNameLength).Replace('/', '\\');

                try
                {
                  if (Directory.Exists(directoryPath))
                  {
                    Directory.Delete(directoryPath, true);

                    logger.Log(MessageType.Inform, "PICTURE DELETED");
                  }
                  else
                  {
                    logger.Log(MessageType.Inform, "NOT EXISTS");
                  }
                }
                catch (Exception ex)
                {
                  logger.Log(Logger.MessageType.Error, directoryPath + " CANNOT BE DELETED");
                  logger.Log(ex);

                  throw;
                }
              });
            });
          }
        }
      }
      catch (Exception ex)
      {
        logger.Log(ex);
        throw;
      }
    }
    #endregion

    #endregion

  }
}