using System.ComponentModel;

namespace VPlayer.AudioStorage.DomainClasses.IPTV
{
  public enum TVSourceType
  {
    [Description("Source")]
    Source,
    [Description("MP3U")]
    M3U,
    [Description("IPTVStalker")]
    IPTVStalker
  }
}