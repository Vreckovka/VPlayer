using Prism.Events;
using VCore.Modularity.RegionProviders;
using VCore.Standard.Factories.ViewModels;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Modularity.Regions;
using VPlayer.Home.ViewModels.LibraryViewModels;
using VPlayer.Home.Views;

namespace VPlayer.Home.ViewModels.TvShows
{
  public class VideoPlaylistsViewModel : PlayableItemsViewModel<PlaylistsView, VideoPlaylistViewModel, VideoFilePlaylist>
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

    public LoadingStatus LoadingStatus { get;  }
  }
}