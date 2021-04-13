using System.Collections.Generic;

namespace VPlayer.AudioStorage.DomainClasses.IPTV
{
  public class TvPlaylist : DomainEntity
  {
    public string Name { get; set; }
    public List<TvChannelGroup> TvChannelGroups { get; set; }
  }
}