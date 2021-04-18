using System.Text;
using Prism.Events;
using VCore.Modularity.RegionProviders;
using VCore.Standard.Factories.ViewModels;
using VCore.ViewModels.Navigation;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.DomainClasses.IPTV;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Modularity.Regions;
using VPlayer.IPTV.ViewModels;
using VPlayer.Library.ViewModels.LibraryViewModels;
using VPlayer.Library.Views;

namespace VPlayer.Library.ViewModels.IPTV
{
  public class IPTVPlaylistsViewModel : PlayableItemsViewModel<PlaylistsView, IPTVPlaylistViewModel, TvPlaylist>
  {
    public IPTVPlaylistsViewModel(IRegionProvider regionProvider, IViewModelsFactory viewModelsFactory, IStorageManager storageManager,
      LibraryCollection<IPTVPlaylistViewModel, TvPlaylist> libraryCollection,
      IEventAggregator eventAggregator) : base(regionProvider, viewModelsFactory, storageManager, libraryCollection, eventAggregator)
    {
    }

    public override bool ContainsNestedRegions => false;
    public override string Header { get; } = "IPTV playlists";
    public override string RegionName { get; protected set; } = RegionNames.LibraryContentRegion;
  }
}
