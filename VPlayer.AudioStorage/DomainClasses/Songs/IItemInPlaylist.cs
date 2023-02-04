using System.ComponentModel.DataAnnotations.Schema;
using VCore.Standard.Modularity.Interfaces;

namespace VPlayer.AudioStorage.DomainClasses
{
  public interface IItemInPlaylist<TItem> : IEntity, IUpdateable<IItemInPlaylist<TItem>>
  {
    [ForeignKey(nameof(ReferencedItem))]
    public int IdReferencedItem { get; set; }
    public TItem ReferencedItem { get; set; }

    public int OrderInPlaylist { get; set; }
  }
}