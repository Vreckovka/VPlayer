using System.ComponentModel.DataAnnotations.Schema;

namespace VPlayer.AudioStorage.DomainClasses
{
  public interface IItemInPlaylist<TItem> : IEntity
  {
    [ForeignKey(nameof(ReferencedItem))]
    public int IdReferencedItem { get; set; }
    public TItem ReferencedItem { get; set; }

    public int OrderInPlaylist { get; set; }
  }
}