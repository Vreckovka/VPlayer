using System.Collections.Generic;
using Prism.Events;
using VPlayer.AudioStorage.Models;

namespace VPlayer.Core.Events
{
    public class PlayArtistEvent : PubSubEvent<IEnumerable<Song>>
    {
    }
}
