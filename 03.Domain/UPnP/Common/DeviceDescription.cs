using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;

namespace UPnP.Common
{
    [XmlRoot("root", Namespace = "urn:schemas-upnp-org:device-1-0")]
    public class DeviceDescription
    {       
        public DeviceDescription()
        {
        }

        [XmlElement("specVersion")]
        public SpecVersionType SpecVersion { get; set; }

        [XmlElement("device")]
        public Device Device { get; set; }
    }

    public class SpecVersionType
    {

        public SpecVersionType()
        {
        }

        [XmlElement("major")]
        public int Major { get; set; }
        [XmlElement("minor")]
        public int Minor { get; set; }
    }


    public class Device
    {
        public Device()
        {
        }

        [XmlElement("deviceType")]
        public string DeviceTypeText { get; set; }
        [XmlElement("UDN")]
        public string UDN { get; set; }
        [XmlElement("friendlyName")]
        public string FriendlyName { get; set; }
        [XmlElement("manufacturer")]
        public string Manufacturer { get; set; }
        [XmlElement("manufacturerURL")]
        public string ManufacturerURL { get; set; }
        [XmlElement("modelName")]
        public string ModelName { get; set; }
        [XmlElement("modelURL")]
        public string ModelURL { get; set; }
        [XmlElement("modelDescription")]
        public string ModelDescription { get; set; }
        [XmlElement("modelNumber")]
        public string ModelNumber { get; set; }
        [XmlElement("serialNumber")]
        public string SerialNumber { get; set; }
        [XmlElement("UPC")]
        public string UPC { get; set; }
        [XmlElement("presentationURL")]
        public string PresentationURL { get; set; }
        [XmlArray("iconList")]
        [XmlArrayItem("icon")]
        public Icon[] IconList { get; set; }
        [XmlArray("serviceList")]
        [XmlArrayItem("service")]
        public Service[] ServiceList { get; set; }

        [XmlIgnore]
        public List<XmlElement> Any { get; set; }
    }

    public partial class Icon
    {
        public Icon()
        {
        }

        [XmlElement("mimetype")]
        public string MimeType { get; set; }
        [XmlElement("height")]
        public int Height { get; set; }
        [XmlElement("width")]
        public int Width { get; set; }
        [XmlElement("depth")]
        public int Depth { get; set; }
        [XmlElement("url")]
        public string Url { get; set; }
    }

    public partial class Service
    {
        public Service()
        {
        }

        [XmlElement("serviceType")]
        public string ServiceType { get; set; }
        [XmlElement("serviceId")]
        public string ServiceId { get; set; }
        [XmlElement("SCPDURL")]
        public string SCPDURL { get; set; }
        [XmlElement("eventSubURL")]
        public string EventSubURL { get; set; }
        [XmlElement("controlURL")]
        public string ControlURL { get; set; }
    }
}
