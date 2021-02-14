using System.ComponentModel.DataAnnotations.Schema;

namespace VPlayer.AudioStorage.DomainClasses
{
  public class PlaylistTvShowEpisode : DomainEntity
  {
    public int OrderInPlaylist { get; set; }

    [ForeignKey(nameof(TvShowEpisode))]
    public int IdTvShowEpisode { get; set; }
    public TvShowEpisode TvShowEpisode { get; set; }
  }
}