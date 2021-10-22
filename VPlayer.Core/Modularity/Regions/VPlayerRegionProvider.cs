using System.Collections.Generic;
using Prism.Regions;
using VCore.Standard.Factories.ViewModels;
using VCore.Standard.Factories.Views;
using VCore.WPF.Modularity.RegionProviders;
using VPlayer.Core.ViewModels.Albums;
using VPlayer.Core.ViewModels.Artists;
using VPlayer.Core.ViewModels.TvShows;

namespace VPlayer.Core.Modularity.Regions
{
  public interface IVPlayerRegionProvider : IRegionProvider
  {
    #region Methods

    void ShowAlbumDetail(AlbumViewModel albumViewModel);

    void ShowArtistDetail(ArtistViewModel albumViewModel);

    void ShowTvShowDetail(TvShowViewModel tvShowViewModel);

    #endregion
  }
}