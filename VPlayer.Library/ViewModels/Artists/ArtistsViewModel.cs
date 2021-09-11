using System;
using System.Linq;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Prism.Events;
using VCore.Modularity.Events;
using VCore.Modularity.RegionProviders;
using VCore.Standard.Factories.ViewModels;
using VCore.Standard.Helpers;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Interfaces.ViewModels;
using VPlayer.Core.Modularity.Regions;
using VPlayer.Core.ViewModels.Artists;
using VPlayer.Home.ViewModels.LibraryViewModels;
using VPlayer.Home.Views.Music.Artists;

namespace VPlayer.Home.ViewModels.Artists
{
  public class ArtistsViewModel : PlayableItemsViewModel<ArtistsView, ArtistViewModel, Artist>, IArtistsViewModel
  {
    #region Constructors

    public ArtistsViewModel(
      IRegionProvider regionProvider,
      IViewModelsFactory viewModelsFactory,
      IStorageManager storageManager,
      LibraryCollection<ArtistViewModel, Artist> libraryCollection,
      IEventAggregator eventAggregator) :
      base(regionProvider, viewModelsFactory, storageManager, libraryCollection, eventAggregator)
    {
      //this.storageManager.ItemChanged.Where(x => x.Item.GetType() == typeof(Song)).Subscribe(SongChange);

      this.storageManager.SubscribeToItemChange<Album>(AlbumChange).DisposeWith(this);

      //this.storageManager.ItemChanged.Where(x => x.Item.GetType() == typeof(Album));
    }

    #endregion 

    #region Properties

    public override bool ContainsNestedRegions => false;
    public override string Header => "Artists";
    public override string RegionName { get; protected set; } = RegionNames.HomeContentRegion;
    public override IQueryable<Artist> LoadQuery => base.LoadQuery.Include(x => x.Albums).ThenInclude(x => x.Songs);

    #endregion 

    #region AlbumChange

    private void AlbumChange(IItemChanged<Album> itemChanged)
    {
      if (itemChanged.Item is Album album && LibraryCollection.WasLoaded)
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

    #endregion

    #region SongChange

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

    #endregion
  }
}