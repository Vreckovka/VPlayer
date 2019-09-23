using System.Collections.Generic;
using Prism.Events;
using VPlayer.Core.DomainClasses;

namespace VPlayer.Core.Events
{
    public class PlaySongsEvent : PubSubEvent<IEnumerable<Song>>
    {
    }
}
