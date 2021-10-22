using Prism.Events;
using VCore.Standard.Factories.ViewModels;
using VCore.WPF;
using VCore.WPF.Modularity.RegionProviders;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Modularity.Regions;
using VPlayer.Home.ViewModels.LibraryViewModels;
using VPlayer.Home.Views;

namespace VPlayer.Home.ViewModels
{
  public class SongPlaylistsViewModel : PlayableItemsViewModel<PlaylistsView, SongsPlaylistViewModel, SoundItemFilePlaylist>
  {
    public SongPlaylistsViewModel(
      IRegionProvider regionProvider,
      IViewModelsFactory viewModelsFactory,
      IStorageManager storageManager,
      LibraryCollection<SongsPlaylistViewModel, SoundItemFilePlaylist> libraryCollection,
      IEventAggregator eventAggregator) :
      base(regionProvider, viewModelsFactory, storageManager, libraryCollection, eventAggregator)
    {
      LoadingStatus = new LoadingStatus();

    }

    public override bool ContainsNestedRegions => false;
    public override string Header { get; } = "Music";
    public override string RegionName { get; protected set; } = RegionNames.HomeContentRegion;


    public LoadingStatus LoadingStatus { get; }
  }
}
