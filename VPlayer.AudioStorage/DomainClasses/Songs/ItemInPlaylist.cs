using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace VPlayer.AudioStorage.DomainClasses.Songs
{
  [Serializable]
  public class ItemInPlaylist<TItem> : DomainEntity,  IItemInPlaylist<TItem>
  {
    [ForeignKey(nameof(ReferencedItem))]
    public int IdReferencedItem { get; set; }
    public TItem ReferencedItem { get; set; }
    public int OrderInPlaylist { get; set; }

    public void Update(IItemInPlaylist<TItem> other)
    {
      OrderInPlaylist = other.OrderInPlaylist;
      ReferencedItem = other.ReferencedItem;
      IdReferencedItem = other.IdReferencedItem;
    }
  }
}