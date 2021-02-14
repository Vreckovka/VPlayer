using System.Collections.Generic;

namespace VPlayer.AudioStorage.DomainClasses
{
  public class TvShowPlaylist : Playlist, IPlaylist<PlaylistTvShowEpisode>
  {
    public virtual List<PlaylistTvShowEpisode> PlaylistItems { get; set; }
  }
}