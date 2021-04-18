using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using VPlayer.IPTV.ViewModels;

namespace VPlayer.AudioStorage.DomainClasses.IPTV
{
  [Serializable]
  public class TvChannel : DomainEntity, INamedEntity
  {
    public string Name
    {
      get
      {
        return TvItem?.Name;
      }
      set
      {
        if(TvItem != null)
        {
          TvItem.Name = value;
        }
      }
    }

    [ForeignKey(nameof(TvSource))]
    public int IdTvSource { get; set; }
    public TvSource TvSource { get; set; }
    
    [ForeignKey(nameof(IdTvItem))]
    public int IdTvItem { get; set; }
    public TvItem TvItem { get; set; }

   
  }
}