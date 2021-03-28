using System;
using Prism.Regions;
using VCore.Modularity.Navigation;
using VCore.Modularity.RegionProviders;
using VCore.Standard.Factories.ViewModels;
using VCore.Standard.Factories.Views;
using VPlayer.Core.Modularity.Regions;
using VPlayer.Core.ViewModels.Albums;
using VPlayer.Core.ViewModels.Artists;
using VPlayer.Core.ViewModels.TvShows;
using VPlayer.Library.ViewModels.AlbumsViewModels;
using VPlayer.Library.ViewModels.ArtistsViewModels;
using VPlayer.Library.ViewModels.TvShows;

namespace VPlayer.Library.Modularity
{
  public class VPlayerLibraryRegionProvider : RegionProvider, IVPlayerRegionProvider
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

    #region ShowAlbumDetail

    public void ShowAlbumDetail(AlbumViewModel albumViewModel)
    {
      var albumDetailViewModel = viewModelsFactory.Create<AlbumDetailViewModel>(albumViewModel);

      albumDetailViewModel.IsActive = true;
    }

    #endregion

    #region ShowArtistDetail

    public void ShowArtistDetail(ArtistViewModel artistViewModel)
    {
      var artistDetailViewModel = viewModelsFactory.Create<ArtistDetailViewModel>(artistViewModel);

      artistDetailViewModel.IsActive = true;
    }

    #endregion

    #region ShowTvShowDetail

    public void ShowTvShowDetail(TvShowViewModel tvShowViewModel)
    {
      var tvShowDetailViewModel = viewModelsFactory.Create<TvShowDetailViewModel>(tvShowViewModel);

      tvShowDetailViewModel.IsActive = true;
    }

    #endregion

    #endregion
  }
}