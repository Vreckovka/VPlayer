using System.Collections.Generic;

namespace VPlayer.AudioStorage.DomainClasses.UPnP
{
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
}