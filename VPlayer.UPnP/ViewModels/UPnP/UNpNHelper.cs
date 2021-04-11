using System.Linq;
using UPnP.Common;
using VPlayer.AudioStorage.DomainClasses.UPnP;

namespace VPlayer.UPnP.ViewModels.UPnP
{
  public static class UNpNHelper
  {
    #region GetDevice

    public static Device GetDevice(this UPnPDevice uPnPDevice)
    {
      var device = new Device()
      {
        DeviceTypeText = uPnPDevice.DeviceTypeText,
        FriendlyName = uPnPDevice.FriendlyName,
        Manufacturer = uPnPDevice.Manufacturer,
        ManufacturerURL = uPnPDevice.ManufacturerURL,
        ModelDescription = uPnPDevice.ModelDescription,
        ModelName = uPnPDevice.ModelName,
        ModelNumber = uPnPDevice.ModelNumber,
        ModelURL = uPnPDevice.ModelURL,
        PresentationURL = uPnPDevice.PresentationURL,
        SerialNumber = uPnPDevice.SerialNumber,
        UDN = uPnPDevice.UDN,
        UPC = uPnPDevice.UPC,
        ServiceList = uPnPDevice.Services.Select(x => x.GetService()).ToArray()
      };

      return device;
    }

    #endregion

    #region GetService

    public static Service GetService(this UPnPService uPnPService)
    {
      var device = new Service()
      {
        ControlURL = uPnPService.ControlURL,
        EventSubURL = uPnPService.EventSubURL,
        SCPDURL = uPnPService.SCPDURL,
        ServiceId = uPnPService.ServiceId,
        ServiceType = uPnPService.ServiceType
      };

      return device;
    }

    #endregion

    #region GetDeviceDbEntity

    public static UPnPDevice GetDeviceDbEntity(this Device pDevice)
    {
      var device = new UPnPDevice()
      {
        DeviceTypeText = pDevice.DeviceTypeText,
        FriendlyName = pDevice.FriendlyName,
        Manufacturer = pDevice.Manufacturer,
        ManufacturerURL = pDevice.ManufacturerURL,
        ModelDescription = pDevice.ModelDescription,
        ModelName = pDevice.ModelName,
        ModelNumber = pDevice.ModelNumber,
        ModelURL = pDevice.ModelURL,
        PresentationURL = pDevice.PresentationURL,
        SerialNumber = pDevice.SerialNumber,
        UDN = pDevice.UDN,
        UPC = pDevice.UPC,
        Services = pDevice.ServiceList.Select(x => x.GetServiceDbEntity()).ToList()
      };

      return device;
    }

    #endregion

    #region GetServiceDbEntity

    public static UPnPService GetServiceDbEntity(this Service service)
    {
      var device = new UPnPService()
      {
        ControlURL = service.ControlURL,
        EventSubURL = service.EventSubURL,
        SCPDURL = service.SCPDURL,
        ServiceId = service.ServiceId,
        ServiceType = service.ServiceType
      };

      return device;
    }

    #endregion
  }
}