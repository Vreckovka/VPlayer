using System.Collections.Generic;
using System.Xml.Serialization;

namespace VPlayer.AudioStorage.InfoDownloader.Clients.Chartlyrics.XMLClasses
{
  [XmlRoot(ElementName = "ArrayOfSearchLyricResult", Namespace = "http://api.chartlyrics.com/")]
	public class ArrayOfSearchLyricResult
	{
		[XmlElement(ElementName = "SearchLyricResult", Namespace = "http://api.chartlyrics.com/")]
		public List<SearchLyricResult> SearchLyricResult { get; set; }
		[XmlAttribute(AttributeName = "xsd", Namespace = "http://www.w3.org/2000/xmlns/")]
		public string Xsd { get; set; }
		[XmlAttribute(AttributeName = "xsi", Namespace = "http://www.w3.org/2000/xmlns/")]
		public string Xsi { get; set; }
		[XmlAttribute(AttributeName = "xmlns")]
		public string Xmlns { get; set; }
	}
}