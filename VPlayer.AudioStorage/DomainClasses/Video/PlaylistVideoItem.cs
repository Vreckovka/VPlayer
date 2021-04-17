using System.ComponentModel.DataAnnotations.Schema;
using VPlayer.AudioStorage.DomainClasses.Video;

namespace VPlayer.AudioStorage.DomainClasses
{
  public class PlaylistVideoItem : DomainEntity, IItemInPlaylist<VideoItem>
  {
    public int OrderInPlaylist { get; set; }

    [ForeignKey(nameof(ReferencedItem))]
    public int IdReferencedItem { get; set; }
    public VideoItem ReferencedItem { get; set; }

  }
}