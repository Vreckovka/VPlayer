using System;
using System.Collections.Generic;
using System.Text;

namespace VPlayer.AudioStorage.DomainClasses.IPTV
{
  [Serializable]
  public class TvSource : DomainEntity, INamedEntity
  {
    public string Name { get; set; }
    public TVSourceType TvSourceType { get; set; }
    public string SourceConnection { get; set; }
    public List<TvChannel> TvChannels { get; set; }
  }
}
