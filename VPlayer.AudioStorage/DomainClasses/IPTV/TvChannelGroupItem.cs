using System.ComponentModel.DataAnnotations.Schema;

namespace VPlayer.AudioStorage.DomainClasses.IPTV
{
  public class TvChannelGroupItem : DomainEntity
  {
    [ForeignKey(nameof(TvChannel))]
    public int IdTvChannel { get; set; }
    public TvChannel TvChannel { get; set; }

  }
}