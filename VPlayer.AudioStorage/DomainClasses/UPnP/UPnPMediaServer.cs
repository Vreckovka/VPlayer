using System;
using System.Text;

namespace VPlayer.AudioStorage.DomainClasses.UPnP
{
  public class UPnPMediaServer : DomainEntity
  {
    public string AliasURL { get; set; }
    public string PresentationURL { get; set; }
    public string DefaultIconUrl { get; set; }
    public bool OnlineServer { get; set; }
    public Uri ContentDirectoryControlUrl { get; set; }
    public UPnPDevice UPnPDevice { get; set; }
  }

  public class UPnPService : DomainEntity
  {
    public string ServiceType { get; set; }
    public string ServiceId { get; set; }
    public string SCPDURL { get; set; }
    public string EventSubURL { get; set; }
    public string ControlURL { get; set; }
  }
 }
