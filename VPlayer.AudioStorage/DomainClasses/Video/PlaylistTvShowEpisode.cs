using System.ComponentModel.DataAnnotations.Schema;
using VPlayer.AudioStorage.DomainClasses.Video;

namespace VPlayer.AudioStorage.DomainClasses
{
  public class PlaylistTvShowEpisode : DomainEntity
  {
    public int OrderInPlaylist { get; set; }

    [ForeignKey(nameof(TvShowEpisode))]
    public int IdTvShowEpisode { get; set; }
    public TvShowEpisode TvShowEpisode { get; set; }

    public TvShow TvShow { get; set; }
  }
}