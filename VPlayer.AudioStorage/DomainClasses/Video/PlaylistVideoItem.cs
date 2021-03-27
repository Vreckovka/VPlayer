using System.ComponentModel.DataAnnotations.Schema;
using VPlayer.AudioStorage.DomainClasses.Video;

namespace VPlayer.AudioStorage.DomainClasses
{
  public class PlaylistVideoItem : DomainEntity
  {
    public int OrderInPlaylist { get; set; }

    [ForeignKey(nameof(VideoItem))]
    public int IdVideoItem { get; set; }
    public VideoItem VideoItem { get; set; }

  }
}