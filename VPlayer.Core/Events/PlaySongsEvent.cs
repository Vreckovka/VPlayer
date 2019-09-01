using System.Collections.Generic;
using Prism.Events;
using VPlayer.AudioStorage.Models;

namespace VPlayer.Core.Events
{
    public class PlaySongsEvent : PubSubEvent<IEnumerable<Song>>
    {
    }
}
