using Prism.Events;
using System.Collections.Generic;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.Core.ViewModels;

namespace VPlayer.Core.Events
{
  public enum DeleteType
  {
    SingleFromPlaylist,
    AlbumFromPlaylist
  }
  public class RemoveFromPlaylistEventArgs
  {
    public DeleteType DeleteType { get; set; }
    public List<SongInPlayList> SongsToDelete { get; set; }
  }

  public class RemoveFromPlaylistEvent : PubSubEvent<RemoveFromPlaylistEventArgs>
  {

  }
}