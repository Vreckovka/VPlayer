using System.ComponentModel.DataAnnotations.Schema;

namespace VPlayer.AudioStorage.DomainClasses.IPTV
{
  public class TvChannel : DomainEntity, INamedEntity
  {
    public string Name { get; set; }

    [ForeignKey(nameof(TvSource))]
    public int IdTvSource { get; set; }
    public TvSource TvSource { get; set; }


    public string Url { get; set; }
  }
}