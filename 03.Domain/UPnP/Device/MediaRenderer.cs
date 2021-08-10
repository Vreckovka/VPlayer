using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UPnP.Common;
using UPnP.Utils;

namespace UPnP.Device
{
  public class MediaRenderer : UPnPDevice
  {
    #region Constructors

    public MediaRenderer()
    {
    }

    public MediaRenderer(DeviceDescription deviceDescription, Uri deviceDescriptionUrl)
    {
      this.DeviceDescription = deviceDescription;

      if (string.IsNullOrEmpty(this.DeviceDescription.Device.PresentationURL))
      {
        this.DeviceDescription.Device.PresentationURL = deviceDescriptionUrl.Authority;
        if (this.DeviceDescription.Device.PresentationURL.IndexOf("http://") == -1)
          this.DeviceDescription.Device.PresentationURL = "http://" + this.DeviceDescription.Device.PresentationURL;
      }
      this.PresentationURL = this.DeviceDescription.Device.PresentationURL + Parser.UseSlash(this.DeviceDescription.Device.PresentationURL);
    }

    #endregion

    #region Public properties

    public string PresentationURL { get; set; }
    public string DefaultIconUrl { get; private set; }
    public MediaRenderer Self { get; private set; }
    public ConnectionManager ConnectionManager { get; private set; }
    public RenderingControl RenderingControl { get; private set; }
    public AVTransport AVTransport { get; private set; }
    public Uri ControlUrl { get; set; }

    #endregion

    #region Public functions

    #region Init

    public void Init()
    {
      if (DeviceDescription?.Device?.ServiceList != null)
      {
        foreach (Service serv in this.DeviceDescription.Device.ServiceList)
          switch (serv.ServiceType)
          {
            case ServiceTypes.RENDERINGCONTROL:
              this.RenderingControl = Deserializer.DeserializeXmlAsync<RenderingControl>(new Uri(this.PresentationURL + serv.SCPDURL.Substring(1)));
              break;
            case ServiceTypes.CONNECTIONMANAGER:
              this.ConnectionManager = Deserializer.DeserializeXmlAsync<ConnectionManager>(new Uri(this.PresentationURL + serv.SCPDURL.Substring(1)));
              break;
            case ServiceTypes.AVTRANSPORT:
              this.AVTransport = Deserializer.DeserializeXmlAsync<AVTransport>(new Uri(this.PresentationURL + serv.SCPDURL.Substring(1)));
              ControlUrl = new Uri(this.PresentationURL + serv.ControlURL.Substring(1));
              break;
          }

        SetDefaultIconUrl();
        this.Self = this;
      }
    }

    #endregion

    public async Task<ServiceAction> GetPositionInfoAsync()
    {
      if (AVTransport != null)
      {
        var actionName = "GetPositionInfo";
        var action = AVTransport.ActionList.Single(x => x.Name == actionName);


        action.SetArgumentValue("InstanceID", "0");
        await action.InvokeAsync(ServiceTypes.AVTRANSPORT, ControlUrl.AbsoluteUri);

        return action;
      }

      return null;
    }

    #endregion

    #region Private functions

    private void SetDefaultIconUrl()
    {
      try
      {
        Icon[] iconList = DeviceDescription.Device.IconList;
        if (iconList != null)
        {
          foreach (Icon ic in iconList)
          {
            if (ic.MimeType.IndexOf("png") > -1)
            {
              this.DefaultIconUrl = ic.Url;
            }
          }
          if (string.IsNullOrEmpty(this.DefaultIconUrl) && (iconList != null))
            this.DefaultIconUrl = iconList[0].Url;

          if (!string.IsNullOrEmpty(this.DefaultIconUrl))
          {
            if (this.DefaultIconUrl.Substring(0, 1) == "/")
              this.DefaultIconUrl = this.DefaultIconUrl.Substring(1);
            this.DefaultIconUrl = this.PresentationURL + this.DefaultIconUrl;
          }
        }
      }
      catch
      {
      }
    }

    #endregion
  }
}
