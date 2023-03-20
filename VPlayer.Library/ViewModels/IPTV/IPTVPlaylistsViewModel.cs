using Prism.Events;
using VCore.Standard.Factories.ViewModels;
using VCore.WPF.Modularity.RegionProviders;
using VPlayer.AudioStorage.DomainClasses.IPTV;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Modularity.Regions;
using VPlayer.Home.ViewModels.LibraryViewModels;
using VPlayer.Home.Views;

namespace VPlayer.Home.ViewModels.IPTV
{
  //public class IPTVPlaylistsViewModel : PlayableItemsViewModel<PlaylistsView, IPTVPlaylistViewModel, TvPlaylist>
  //{
  //  public IPTVPlaylistsViewModel(IRegionProvider regionProvider, IViewModelsFactory viewModelsFactory, IStorageManager storageManager,
  //    LibraryCollection<IPTVPlaylistViewModel, TvPlaylist> libraryCollection,
  //    IEventAggregator eventAggregator) : base(regionProvider, viewModelsFactory, storageManager, libraryCollection, eventAggregator)
  //  {
  //  }

  //  public override bool ContainsNestedRegions => false;
  //  public override string Header { get; } = "IPTV";
  //  public override string RegionName { get; protected set; } = RegionNames.HomeContentRegion;
  //}
}
