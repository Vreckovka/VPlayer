using VCore.Standard.Factories.ViewModels;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.DomainClasses.Video;
using VPlayer.Core.ViewModels.TvShows;

namespace VPlayer.Core.Factories
{
  public interface IVPlayerViewModelsFactory : IViewModelsFactory
  {
    TvShowEpisodeInPlaylistViewModel CreateTvShowEpisodeInPlayList(VideoItem videoItem, TvShowEpisode tvShowEpisode);
  }
}