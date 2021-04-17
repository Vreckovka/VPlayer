using System.ComponentModel.DataAnnotations.Schema;
using VCore.Standard;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.DomainClasses.IPTV;
using VPlayer.Core.ViewModels;
using VPLayer.Domain.Contracts.IPTV;

namespace VPlayer.IPTV.ViewModels
{
  public class TvItem : DomainEntity , IPlayableModel, IUpdateable<TvItem>
  {
    public string Source { get; set; }
    public string Name { get; set; }
    public bool IsFavorite { get; set; }
    public void Update(TvItem other)
    {
      throw new System.NotImplementedException();
    }
  }


  public class TvPlaylistItem : DomainEntity, IItemInPlaylist<TvItem>, IUpdateable<TvPlaylistItem>
  {
    [ForeignKey(nameof(ReferencedItem))]
    public int IdReferencedItem { get; set; }
    public TvItem ReferencedItem { get; set; }
    public string Name { get; set; }


    public void Update(TvPlaylistItem other)
    {
      throw new System.NotImplementedException();
    }
  }
}