using System.Collections.Generic;
using VPlayer.Core.ViewModels.TvShow;

namespace VPlayer.Core.Events
{
  public class PlayTvShowEventData : PlayItemEventData<TvShowEpisodeInPlaylistViewModel>
  {
    public PlayTvShowEventData(IEnumerable<TvShowEpisodeInPlaylistViewModel> items, EventAction eventAction, object model) : base(items, eventAction, model)
    {
    }

    public PlayTvShowEventData(IEnumerable<TvShowEpisodeInPlaylistViewModel> items, EventAction eventAction, bool? isShufle,
      bool? isRepeat,
      float? setPostion,
      object model) : base(items, eventAction, isShufle, isRepeat, setPostion, model)
    {
    }
  }
}