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
using VPlayer.Core.ViewModels.Artists;
using VPlayer.Library.Views;

namespace VPlayer.Library.ViewModels.LibraryViewModels.ArtistsViewModels
{
  public class ArtistsViewModel : PlayableItemsViewModel<ArtistsView, ArtistViewModel, Artist>, IArtistsViewModel
  {
    #region Constructors

    public ArtistsViewModel(
      IRegionProvider regionProvider,
      IViewModelsFactory viewModelsFactory,
      IStorageManager storageManager,
      LibraryCollection<ArtistViewModel, Artist> libraryCollection) :
      base(regionProvider, viewModelsFactory, storageManager, libraryCollection)
    {
    }

    #endregion Constructors

    #region Properties

    public override bool ContainsNestedRegions => false;
    public override string Header => "Artists";
    public override string RegionName { get; protected set; } = RegionNames.LibraryContentRegion;
    public override IQueryable<Artist> LoadQuery => base.LoadQuery.Include(x => x.Albums.Select(y => y.Songs));

    #endregion Properties

    protected override void ItemsChanged(ItemChanged itemChanged)
    {
      base.ItemsChanged(itemChanged);

      if (itemChanged.Item is Album album)
      {
        ArtistViewModel artist = null;

        if (album.Artist == null)
        {
          artist = LibraryCollection.Items.SingleOrDefault(x => x.Model.Albums.Count(y => y.Id == album.Id) != 0);
        }
        else
          artist = LibraryCollection.Items.SingleOrDefault(x => x.ModelId == album.Artist.Id);

        if (artist != null && !artist.Model.Albums.Contains(album))
        {
          switch (itemChanged.Changed)
          {
            case Changed.Added:
              artist.Model.Albums.Add(album);
              break;
            case Changed.Removed:
              artist.Model.Albums.Remove(album);
              break;
            case Changed.Updated:
              break;
            default:
              throw new ArgumentOutOfRangeException();
          }

          Application.Current?.Dispatcher.Invoke(() =>
          {
            artist.RaisePropertyChanges();
          });
        }
      }
    }

    protected void SongChange(ItemChanged itemChanged)
    {
      if (itemChanged.Item is Song song)
      {
        //var artist = LibraryCollection.Items.SingleOrDefault(x => x.ModelId == song.Album.Artist.Id);
        //var album = artist?.Model.Albums.SingleOrDefault(x => x.Id == song.Album.Id);

        switch (itemChanged.Changed)
        {
          case Changed.Added:
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
      }
    }
  }
}