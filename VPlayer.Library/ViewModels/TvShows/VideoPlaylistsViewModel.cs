﻿using System.Collections.Generic;
using System.Linq;
using Prism.Events;
using VCore.Standard.Factories.ViewModels;
using VCore.Standard.Helpers;
using VCore.WPF;
using VCore.WPF.Modularity.RegionProviders;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.DomainClasses.Video;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Modularity.Regions;
using VPlayer.Home.ViewModels.LibraryViewModels;
using VPlayer.Home.Views;

namespace VPlayer.Home.ViewModels.TvShows
{
  public class VideoPlaylistsViewModel : PlaylistsViewModel<PlaylistsView, VideoPlaylistViewModel, VideoFilePlaylist, PlaylistVideoItem, VideoItem>
  {
    public VideoPlaylistsViewModel(
      IRegionProvider regionProvider, 
      IViewModelsFactory viewModelsFactory, 
      IStorageManager storageManager,
      LibraryCollection<VideoPlaylistViewModel, VideoFilePlaylist> libraryCollection,
      IEventAggregator eventAggregator) :
      base(regionProvider, viewModelsFactory, storageManager, libraryCollection, eventAggregator)
    {
    }

    public override bool ContainsNestedRegions => false;
    public override string Header { get; } = "Videos";
    public override string RegionName { get; protected set; } = RegionNames.HomeContentRegion;
    protected override List<PinnedItem> GetPinnedTypedItems(List<PinnedItem> pinnedItems)
    {
      return pinnedItems.Where(x =>
        x.PinnedType == PinnedType.VideoPlaylist ||
        x.PinnedType == PinnedType.VideoFolder ||
        x.PinnedType == PinnedType.VideoFile
      ).ToList();
    }
  }
}