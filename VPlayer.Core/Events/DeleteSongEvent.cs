using Prism.Events;
using System.Collections.Generic;
using VPlayer.AudioStorage.DomainClasses;

namespace VPlayer.Core.Events
{
  public enum DeleteType
  {
    Database,
    SingleFromPlaylist,
    AlbumFromPlaylist
  }
  public class DeleteEventArgs
  {
    public DeleteType DeleteType { get; set; }
    public List<Song> SongsToDelete { get; set; }
  }

  public class DeleteSongEvent : PubSubEvent<DeleteEventArgs>
  {

  }
}