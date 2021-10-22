using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using VCore.Standard.Modularity.Interfaces;
using VPlayer.IPTV.ViewModels;

namespace VPlayer.AudioStorage.DomainClasses.IPTV
{
  public class TvChannelGroup : DomainEntity, INamedEntity, IUpdateable<TvChannelGroup>
  {
    public string Name
    {
      get
      {
        return TvItem?.Name;
      }
      set
      {
        if (TvItem != null)
        {
          TvItem.Name = value;
        }
      }
    }

    [ForeignKey(nameof(IdTvItem))]
    public int IdTvItem { get; set; }
    public TvItem TvItem { get; set; }

    public List<TvChannelGroupItem> TvChannelGroupItems { get; set; }
    public void Update(TvChannelGroup other)
    {
      Name = other.Name;

      if (other.TvChannelGroupItems != null)
      {
        TvChannelGroupItems = other.TvChannelGroupItems;
      }
    }
  }
}