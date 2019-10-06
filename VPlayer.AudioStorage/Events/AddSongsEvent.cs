using System.Collections.Generic;
using Prism.Events;
using VPlayer.Core.ViewModels;

namespace VPlayer.Core.Events
{
  public class AddSongsEvent : PubSubEvent<IEnumerable<SongInPlayList>>
  {
  }
}