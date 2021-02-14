using System.ComponentModel.DataAnnotations.Schema;

namespace VPlayer.AudioStorage.DomainClasses
{
  public class PlaylistSong : DomainEntity
  {
    public int OrderInPlaylist { get; set; }

    [ForeignKey(nameof(Song))]
    public int IdSong { get; set; }
    public Song Song { get; set; }
  }
}