using System;
using System.Collections.Generic;
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

  public class UPnPDevice : DomainEntity
  { 
    public string DeviceTypeText { get; set; }
    public string UDN { get; set; }
    public string FriendlyName { get; set; }
    public string Manufacturer { get; set; }
    public string ManufacturerURL { get; set; }
    public string ModelName { get; set; }
    public string ModelURL { get; set; }
    public string ModelDescription { get; set; }
    public string ModelNumber { get; set; }
    public string SerialNumber { get; set; }
    public string UPC { get; set; }
    public string PresentationURL { get; set; }
    public List<UPnPService> Services { get; set; }
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
