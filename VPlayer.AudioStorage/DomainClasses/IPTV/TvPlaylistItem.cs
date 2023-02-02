using System;
using System.ComponentModel.DataAnnotations.Schema;
using VCore.Standard;
using VCore.Standard.Modularity.Interfaces;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.DomainClasses.IPTV;
using VPlayer.Core.ViewModels;
using VPLayer.Domain.Contracts.IPTV;

namespace VPlayer.IPTV.ViewModels
{
  [Serializable]
  public class TvItem : DomainEntity, ITvItem, IUpdateable<TvItem>
  {
    public string Source { get; set; }
    public string Name { get; set; }
    public bool IsFavorite { get; set; }

    public void Update(TvItem other)
    {
      throw new System.NotImplementedException();
    }

  
  }


  public class TvPlaylistItem : DomainEntity, IItemInPlaylist<TvItem>, IUpdateable<TvPlaylistItem>, INamedEntity
  {
    [ForeignKey(nameof(ReferencedItem))]
    public int IdReferencedItem { get; set; }
    public TvItem ReferencedItem { get; set; }
    public int OrderInPlaylist { get; set; }
    public string Name { get; set; }


    public void Update(TvPlaylistItem other)
    {
      throw new System.NotImplementedException();
    }
  }
}