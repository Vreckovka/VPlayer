﻿using Prism.Events;
using VCore.Modularity.RegionProviders;
using VCore.Standard.Factories.ViewModels;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Modularity.Regions;
using VPlayer.Library.ViewModels.LibraryViewModels;
using VPlayer.Library.Views;

namespace VPlayer.Library.ViewModels.TvShows
{
  public class TvShowPlaylistsViewModel : PlayableItemsViewModel<PlaylistsView, TvShowPlaylistViewModel, TvShowPlaylist>
  {
    public TvShowPlaylistsViewModel(
      IRegionProvider regionProvider, 
      IViewModelsFactory viewModelsFactory, 
      IStorageManager storageManager,
      LibraryCollection<TvShowPlaylistViewModel, TvShowPlaylist> libraryCollection,
      IEventAggregator eventAggregator) :
      base(regionProvider, viewModelsFactory, storageManager, libraryCollection, eventAggregator)
    {
    }

    public override bool ContainsNestedRegions => false;
    public override string Header { get; } = "Tv show Playlists";
    public override string RegionName { get; protected set; } = RegionNames.LibraryContentRegion;

    #region IsBusy

    private bool isBusy;

    public bool IsBusy
    {
      get { return isBusy; }
      set
      {
        if (value != isBusy)
        {
          isBusy = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion
  }
}