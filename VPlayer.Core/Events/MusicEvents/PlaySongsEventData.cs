using System.Collections.Generic;
using VPlayer.Core.ViewModels;

namespace VPlayer.Core.Events
{
  public class PlaySongsEventData : PlayItemEventData<SongInPlayList>
  {
    public PlaySongsEventData(IEnumerable<SongInPlayList> items, EventAction eventAction, object model) : base(items, eventAction, model)
    {
    }

    public PlaySongsEventData(IEnumerable<SongInPlayList> items, EventAction eventAction, bool? isShufle,
      bool? isRepeat,
      float? setPostion,
      object model) : base(items, eventAction, isShufle, isRepeat, setPostion, model)
    {
    }
  }
}