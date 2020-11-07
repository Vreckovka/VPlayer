using Prism.Regions;
using VCore.Modularity.Navigation;
using VCore.Standard.Factories.ViewModels;
using VCore.Standard.Factories.Views;
using VPlayer.Core.Modularity.Regions;
using VPlayer.Core.ViewModels.Albums;
using VPlayer.Core.ViewModels.Artists;
using VPlayer.Library.ViewModels.AlbumsViewModels;
using VPlayer.Library.ViewModels.ArtistsViewModels;

namespace VPlayer.Library.Modularity
{
  public class VPlayerLibraryRegionProvider : VPlayerRegionProvider
  {
    #region Constructors

    public VPlayerLibraryRegionProvider(
      IRegionManager regionManager,
      IViewFactory viewFactory,
      IViewModelsFactory viewModelsFactory,
      INavigationProvider navigationProvider) : base(regionManager, viewFactory, viewModelsFactory, navigationProvider)
    {
    }

    #endregion Constructors

    #region Methods

    public override void ShowAlbumDetail(AlbumViewModel albumViewModel, string regionName = null)
    {
      var asd = viewModelsFactory.Create<AlbumDetailViewModel>(albumViewModel, regionName);

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