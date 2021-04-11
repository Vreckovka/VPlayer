using System;
using UPnP.Common;
using UPnP.Utils;

namespace UPnP.Device
{
    public class OtherDevice
    {
        #region Constructors

        public OtherDevice()
        {
        }

        public OtherDevice(DeviceDescription deviceDescription, Uri deviceDescriptionUrl)
        {
            this.DeviceDescription = deviceDescription;

            if (string.IsNullOrEmpty(this.DeviceDescription.Device.PresentationURL))
            {
                this.DeviceDescription.Device.PresentationURL = deviceDescriptionUrl.Authority;
                if (this.DeviceDescription.Device.PresentationURL.IndexOf("http://") == -1)
                    this.DeviceDescription.Device.PresentationURL = "http://" + this.DeviceDescription.Device.PresentationURL;
            }
            this.PresentationURL = this.DeviceDescription.Device.PresentationURL + Parser.UseSlash(this.DeviceDescription.Device.PresentationURL);

            SetDefaultIconUrl();
            this.Self = this;
        }

        #endregion

        #region Public properties

        public string PresentationURL { get; private set; }
        public string DefaultIconUrl { get; private set; }
        public OtherDevice Self { get; private set; }

        public DeviceDescription DeviceDescription { get; private set; }

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
