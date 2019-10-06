using Prism.Events;
using System.Collections.Generic;
using VPlayer.Core.ViewModels;

namespace VPlayer.Core.Events
{
  public class PlaySongsEvent : PubSubEvent<IEnumerable<SongInPlayList>>
  {
  }
}