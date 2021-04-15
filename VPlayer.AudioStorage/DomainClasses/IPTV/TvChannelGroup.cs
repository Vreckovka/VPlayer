using System.Collections.Generic;

namespace VPlayer.AudioStorage.DomainClasses.IPTV
{
  public class TvChannelGroup : DomainEntity, INamedEntity, IUpdateable<TvChannelGroup>
  {
    public string Name { get; set; }

    public List<TvChannelGroupItem> TvChannels { get; set; }
    public void Update(TvChannelGroup other)
    {
      Name = other.Name;

      if (other.TvChannels != null)
      {
        TvChannels = other.TvChannels;
      }
    }
  }
}