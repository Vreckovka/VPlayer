using System.Collections.Generic;
using Prism.Events;
using VPlayer.Core.DomainClasses;
using VPlayer.Core.ViewModels;

namespace VPlayer.Core.Events
{
  public class PlaySongsEvent : PubSubEvent<IEnumerable<SongInPlayList>>
  {
  }
}
