using System.Collections.Generic;

namespace VPlayer.AudioStorage.DomainClasses
{
  public class SongsPlaylist : Playlist, IPlaylist<PlaylistSong>
  {
    public virtual List<PlaylistSong> PlaylistItems { get; set; }
  }
}

