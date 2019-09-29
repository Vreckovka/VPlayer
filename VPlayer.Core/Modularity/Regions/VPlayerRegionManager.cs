using Prism.Regions;
using VCore.Factories;
using VCore.Factories.Views;
using VCore.Modularity.Navigation;
using VCore.Modularity.RegionProviders;
using VPlayer.Core.ViewModels.Artists;
using VPlayer.Library.ViewModels.AlbumsViewModels;

namespace VPlayer.Core.Modularity.Regions
{
  public interface IVPlayerRegionManager : IRegionProvider
  {
    #region Methods

    void ShowAlbumDetail(AlbumViewModel albumViewModel);

    void ShowArtistDetail(ArtistViewModel albumViewModel);

    #endregion Methods
  }

  public class VPlayerRegionManager : BaseRegionProvider, IVPlayerRegionManager
  {
    #region Constructors

    public VPlayerRegionManager(
      IRegionManager regionManager,
      IViewFactory viewFactory,
      IViewModelsFactory viewModelsFactory,
      INavigationProvider navigationProvider) : base(regionManager, viewFactory, viewModelsFactory, navigationProvider)
    {
    }

    #endregion Constructors

    #region Methods

    public virtual void ShowAlbumDetail(AlbumViewModel albumViewModel)
    {
    }

    public virtual void ShowArtistDetail(ArtistViewModel albumViewModel)
    {
    }

    #endregion Methods
  }
}