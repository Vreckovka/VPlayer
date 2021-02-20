using System.Collections.Generic;
using Prism.Regions;
using VCore.Modularity.Navigation;
using VCore.Modularity.RegionProviders;
using VCore.Standard.Factories.ViewModels;
using VCore.Standard.Factories.Views;
using VPlayer.Core.ViewModels.Albums;
using VPlayer.Core.ViewModels.Artists;

namespace VPlayer.Core.Modularity.Regions
{
  public interface IVPlayerRegionProvider : IRegionProvider
  {
    #region Methods


    void ShowAlbumDetail(AlbumViewModel albumViewModel);

    void ShowArtistDetail(ArtistViewModel albumViewModel);

    #endregion 
  }

  public class VPlayerRegionProvider : RegionProvider, IVPlayerRegionProvider
  {
   

    #region Constructors

    public VPlayerRegionProvider(
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