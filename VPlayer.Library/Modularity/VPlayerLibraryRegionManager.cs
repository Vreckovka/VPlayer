using Prism.Regions;
using VCore.Annotations;
using VCore.Factories;
using VCore.Factories.Views;
using VCore.Modularity.RegionProviders;
using VPlayer.Core.Modularity.Regions;
using VPlayer.Core.ViewModels;
using VPlayer.Core.ViewModels.Artists;
using VPlayer.Library.ViewModels.AlbumsViewModels;
using VPlayer.Library.ViewModels.LibraryViewModels.ArtistsViewModels;

namespace VPlayer.Library.Modularity
{
  public partial class VPlayerLibraryRegionManager : VPlayerRegionManager
  {
    public VPlayerLibraryRegionManager(
      IRegionManager regionManager,
      IViewFactory viewFactory, [NotNull]
      IViewModelsFactory viewModelsFactory) : base(regionManager, viewFactory, viewModelsFactory)
    {
    }

    public override void ShowAlbumDetail(AlbumViewModel albumViewModel)
    {
      var asd = viewModelsFactory.Create<AlbumDetailViewModel>(albumViewModel);
      asd.IsActive = true;
    }

    public override void ShowArtistDetail(ArtistViewModel artistViewModel)
    {
      var asd = viewModelsFactory.Create<ArtistDetailViewModel>(artistViewModel);
      asd.IsActive = true;
    }
  }
}
