using VCore.Standard.Factories.ViewModels;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.DomainClasses.Video;
using VPlayer.Core.ViewModels;
using VPlayer.Core.ViewModels.TvShows;
using VPLayer.Domain.Contracts.IPTV;
using VPlayer.IPTV.ViewModels;

namespace VPlayer.Core.Factories
{
  public interface IVPlayerViewModelsFactory : IViewModelsFactory
  {
    TvShowEpisodeInPlaylistViewModel CreateTvShowEpisodeInPlayList(VideoItem videoItem, TvShowEpisode tvShowEpisode);

    SongInPlayListViewModel CreateSongInPlayListViewModel(SoundItem soundItem, Song song);

    TvItemInPlaylistItemViewModel CreateTvItemInPlaylistItemViewModel(TvItem model, ITvPlayableItem tvPlayableItem);
  }
}