using Prism.Regions;
using VCore.Factories;
using VCore.Factories.Views;
using VCore.Modularity.Navigation;
using VPlayer.Core.Modularity.Regions;
using VPlayer.Core.ViewModels.Artists;
using VPlayer.Library.ViewModels.AlbumsViewModels;
using VPlayer.Library.ViewModels.LibraryViewModels.ArtistsViewModels;

namespace VPlayer.Library.Modularity
{
  public partial class VPlayerLibraryRegionManager : VPlayerRegionManager
  {
    #region Constructors

    public VPlayerLibraryRegionManager(
      IRegionManager regionManager,
      IViewFactory viewFactory,
      IViewModelsFactory viewModelsFactory,
      INavigationProvider navigationProvider) : base(regionManager, viewFactory, viewModelsFactory, navigationProvider)
    {
    }

    #endregion Constructors

    #region Methods

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

    #endregion Methods
  }
}