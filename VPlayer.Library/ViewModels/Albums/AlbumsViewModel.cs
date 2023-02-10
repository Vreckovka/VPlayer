using System;
using System.Linq;
using System.Windows;
using Logger;
using Microsoft.EntityFrameworkCore;
using Prism.Events;
using VCore.Standard.Factories.ViewModels;
using VCore.Standard.Helpers;
using VCore.WPF.Modularity.Events;
using VCore.WPF.Modularity.RegionProviders;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Interfaces.ViewModels;
using VPlayer.Core.Modularity.Regions;
using VPlayer.Core.ViewModels.Albums;
using VPlayer.Home.ViewModels.LibraryViewModels;
using VPlayer.Home.Views.Music.Albums;

namespace VPlayer.Home.ViewModels.Albums
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
        : base(regionProvider, viewModelsFactory, storageManager, libraryCollection, eventAggregator)
    {
      this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
      EventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));

      this.storageManager.SubscribeToItemChange<Artist>(ArtistChanged).DisposeWith(this);
    }

    #endregion Constructors

    #region Properties

    public override bool ContainsNestedRegions => false;
    public override string Header => "Albums";
    public override IQueryable<Album> LoadQuery => base.LoadQuery.Include(x => x.Artist).Include(x => x.Songs);
    public override string RegionName { get; protected set; } = RegionNames.HomeContentRegion;
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

    #region ArtistChanged

    protected void ArtistChanged(IItemChanged<Artist> itemChanged)
    {
      var artist = itemChanged.Item;

      if (LibraryCollection.WasLoaded && artist != null)
      {
        var albums = LibraryCollection.Items.Where(x => x.Model?.ArtistId != null).Where(x => x.Model.ArtistId == artist.Id);

        switch (itemChanged.Changed)
        {
          case Changed.Removed:

            LibraryCollection.Remove(albums.Select(x => x.Model).ToList());

            RaisePropertyChanged(nameof(View));
            break;
        }
      }
    }

    #endregion

    protected override void OnUpdateItemChange(Album model)
    {
      var originalItem = LibraryCollection.Items?.SingleOrDefault(x => x.ModelId == model.Id);

      base.OnUpdateItemChange(model);

      if (originalItem != null && originalItem.Model.Artist != null && model.Artist == null)
      {
        model.Artist = originalItem.Model.Artist;
      }

    }

    #region SongChange

    protected void SongChange(IItemChanged<Song> itemChanged)
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
              break;

            case Changed.Updated:
              break;

            default:
              throw new ArgumentOutOfRangeException();
          }

          Application.Current?.Dispatcher?.Invoke(() => { album.RaisePropertyChanges(); });
        }
      }
    }

    #endregion

    #endregion

  }
}