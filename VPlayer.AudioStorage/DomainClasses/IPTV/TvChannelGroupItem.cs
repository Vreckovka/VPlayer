using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace VPlayer.AudioStorage.DomainClasses.IPTV
{
  [Serializable]
  public class TvChannelGroupItem : DomainEntity
  {
    [ForeignKey(nameof(TvChannel))]
    public int IdTvChannel { get; set; }
    public TvChannel TvChannel { get; set; }
  }
}