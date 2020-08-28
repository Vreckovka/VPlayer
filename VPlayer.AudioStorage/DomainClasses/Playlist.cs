using System.Collections.Generic;
using System.ComponentModel;
using Windows.Foundation.Metadata;

namespace VPlayer.AudioStorage.DomainClasses
{
  public class Playlist : DomainEntity, INamedEntity
  {
    public virtual List<PlaylistSong> PlaylistSongs { get; set; }
    public string Name { get; set; }
    public long? SongsInPlaylitsHashCode { get; set; }
    public int? SongCount { get; set; }
  }
}