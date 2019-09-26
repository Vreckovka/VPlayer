using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Regions;
using VCore.Annotations;
using VCore.Factories;
using VCore.Factories.Views;
using VCore.Modularity.RegionProviders;
using VPlayer.Core.ViewModels;
using VPlayer.Core.ViewModels.Artists;
using VPlayer.Library.ViewModels.AlbumsViewModels;

namespace VPlayer.Core.Modularity.Regions
{
  public interface IVPlayerRegionManager : IRegionProvider
  {
    void ShowAlbumDetail(AlbumViewModel albumViewModel);
    void ShowArtistDetail(ArtistViewModel albumViewModel);
  }

  public class VPlayerRegionManager : BaseRegionProvider, IVPlayerRegionManager
  {
    public VPlayerRegionManager(
      IRegionManager regionManager,
      IViewFactory viewFactory, [NotNull]
      IViewModelsFactory viewModelsFactory) : base(regionManager, viewFactory, viewModelsFactory)
    {
    }

    public virtual void ShowAlbumDetail(AlbumViewModel albumViewModel)
    {
    }

    public virtual void ShowArtistDetail(ArtistViewModel albumViewModel)
    {
    }
  }
}
