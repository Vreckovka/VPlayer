using System;
using Prism.Regions;
using VCore.Annotations;
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
      [NotNull] IRegionManager regionManager,
      IViewFactory viewFactory,
      IViewModelsFactory viewModelsFactory,
      INavigationProvider navigationProvider) : base(regionManager, viewFactory, viewModelsFactory, navigationProvider)
    {
    }

    #endregion Constructors

    #region Methods

    public override void ShowAlbumDetail(AlbumViewModel albumViewModel, string regionName = null)
    {
      var albumDetailViewModel = viewModelsFactory.Create<AlbumDetailViewModel>(albumViewModel);

      albumDetailViewModel.RegionManager = RegionManager;

      albumDetailViewModel.IsActive = true;
    }

    public override void ShowArtistDetail(ArtistViewModel artistViewModel)
    {
      var asd = viewModelsFactory.Create<ArtistDetailViewModel>(artistViewModel);

      asd.IsActive = true;
    }

    #endregion Methods
  }
}