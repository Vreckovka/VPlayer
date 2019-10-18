using System;
using System.Data.Entity;
using System.Linq;
using System.Windows;
using VCore.Factories;
using VCore.Modularity.Events;
using VCore.Modularity.RegionProviders;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Interfaces.ViewModels;
using VPlayer.Core.Modularity.Regions;
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
        LibraryCollection<AlbumViewModel, Album> libraryCollection)
        : base(regionProvider, viewModelsFactory, storageManager, libraryCollection)
    {
      ;
    }

    #endregion Constructors

    #region Properties

    public override bool ContainsNestedRegions => false;
    public override string Header => "Albums";
    public override IQueryable<Album> LoadQuery => base.LoadQuery.Include(x => x.Artist).Include(x => x.Songs);
    public override string RegionName { get; protected set; } = RegionNames.LibraryContentRegion;

    protected override void ItemsChanged(ItemChanged itemChanged)
    {
      base.ItemsChanged(itemChanged);

      if (itemChanged.Item is Song)
      {
        SongChange(itemChanged);
      }
    }

    #endregion Properties

    protected void SongChange(ItemChanged itemChanged)
    {
      if (itemChanged.Item is Song song)
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
            throw new NotImplementedException();
            break;

          default:
            throw new ArgumentOutOfRangeException();
        }

        Application.Current.Dispatcher.Invoke(() =>
        {
          album.RaisePropertyChanges();
        });
      }
    }
  }
}