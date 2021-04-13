using System.Collections.Generic;

namespace VPlayer.AudioStorage.DomainClasses.IPTV
{
  public class TvChannelGroup : DomainEntity, INamedEntity
  {
    public string Name { get; set; }

    public List<TvChannel> TVChannels { get; set; }
  }
}