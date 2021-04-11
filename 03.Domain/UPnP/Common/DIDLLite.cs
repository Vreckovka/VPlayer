using System.Collections.Generic;
using System.Xml.Serialization;

namespace UPnP.Common
{
    [XmlRoot("DIDL-Lite", Namespace = "urn:schemas-upnp-org:metadata-1-0/DIDL-Lite/")]
    public class DIDLLite
    {
        [XmlElement("container")]
        public List<Container> Containers { get; set; }
        [XmlElement("item")]
        public List<Item> Items { get; set; }
    }

    public partial class Container
    {
        [XmlAttribute("persistentID", Namespace = "http://www.pv.com/pvns/")]
        public string PersistentID { get; set; }
        [XmlAttribute("searchable")]
        public int Searchable { get; set; }
        [XmlAttribute("childCount")]
        public int ChildCount { get; set; }
        [XmlAttribute("restricted")]
        public int Restricted { get; set; }
        [XmlAttribute("parentID")]
        public string ParentID { get; set; }
        [XmlAttribute("id")]
        public string Id { get; set; }

        [XmlElement("title", Namespace = "http://purl.org/dc/elements/1.1/")]
        public string Title { get; set; }
        [XmlElement("childCountContainer", Namespace = "http://www.pv.com/pvns/")]
        public int ChildCountContainer { get; set; }
        [XmlElement("modificationTime", Namespace = "http://www.pv.com/pvns/")]
        public string ModificationTime { get; set; }
        [XmlElement("lastUpdated", Namespace = "http://www.pv.com/pvns/")]
        public string LastUpdated { get; set; }
        [XmlElement("containerContent", Namespace = "http://www.pv.com/pvns/")]
        public string ContainerContent { get; set; }
        [XmlElement("class", Namespace = "urn:schemas-upnp-org:metadata-1-0/upnp/")]
        public string Class { get; set; }
    }

    public partial class Item
    {
        [XmlAttribute("restricted")]
        public int Restricted { get; set; }
        [XmlAttribute("parentID")]
        public string ParentID { get; set; }
        [XmlAttribute("refID")]
        public string RefId { get; set; }
        [XmlAttribute("id")]
        public string Id { get; set; }

        [XmlElement("title", Namespace = "http://purl.org/dc/elements/1.1/")]
        public string Title { get; set; }
        [XmlElement("date", Namespace = "http://purl.org/dc/elements/1.1/")]
        public string Date { get; set; }
        [XmlElement("genre", Namespace = "urn:schemas-upnp-org:metadata-1-0/upnp/")]
        public string Genre { get; set; }
        [XmlElement("album", Namespace = "urn:schemas-upnp-org:metadata-1-0/upnp/")]
        public string Album { get; set; }
        [XmlElement("albumArtUri", Namespace = "urn:schemas-upnp-org:metadata-1-0/upnp/")]
        public AlbumArtUriData AlbumArtUri { get; set; }
        [XmlElement("extension", Namespace = "http://www.pv.com/pvns/")]
        public string Extension { get; set; }
        [XmlElement("modificationTime", Namespace = "http://www.pv.com/pvns/")]
        public string ModificationTime { get; set; }
        [XmlElement("addedTime", Namespace = "http://www.pv.com/pvns/")]
        public string AddedTime { get; set; }
        [XmlElement("lastUpdated", Namespace = "http://www.pv.com/pvns/")]
        public string LastUpdated { get; set; }
        [XmlElement("album_crosslink", Namespace = "http://www.pv.com/pvns/")]
        public string AlbumCrosslink { get; set; }
        [XmlElement("date_crosslink", Namespace = "http://www.pv.com/pvns/")]
        public string DateCrosslink { get; set; }
        [XmlElement("bookmark", Namespace = "http://www.pv.com/pvns/")]
        public string Bookmark { get; set; }
        [XmlElement("res")]
        public List<ResData> Res { get; set; }
        [XmlElement("class", Namespace = "urn:schemas-upnp-org:metadata-1-0/upnp/")]
        public string Class { get; set; }
    }

    public partial class AlbumArtUriData
    {
        [XmlAttribute("profileID", Namespace = "urn:schema-dlna-org:metadata-1-0/")]
        public string ProfileId { get; set; }
        [XmlElement("albumArtUri", Namespace = "urn:schemas-upnp-org:metadata-1-0/upnp/")]
        public string AlbumArtUri { get; set; }
    }

    public partial class ResData
    {
        [XmlText]
        public string Value { get; set; }
        [XmlAttribute("protocolInfo")]
        public string ProtocolInfo { get; set; }
        [XmlAttribute("resolution")]
        public string Resolution { get; set; }
        [XmlAttribute("size")]
        public string Size { get; set; }
        [XmlAttribute("duration")]
        public string Duration { get; set; }
    }
}
