using System.Xml.Serialization;

namespace VPlayer.AudioStorage.InfoDownloader.Clients.Chartlyrics.XMLClasses
{
  [XmlRoot(ElementName = "SearchLyricResult", Namespace = "http://api.chartlyrics.com/")]
  public class SearchLyricResult
  {
    [XmlElement(ElementName = "TrackId", Namespace = "http://api.chartlyrics.com/")]
    public string TrackId { get; set; }
    [XmlElement(ElementName = "LyricChecksum", Namespace = "http://api.chartlyrics.com/")]
    public string LyricChecksum { get; set; }
    [XmlElement(ElementName = "LyricId", Namespace = "http://api.chartlyrics.com/")]
    public string LyricId { get; set; }
    [XmlElement(ElementName = "SongUrl", Namespace = "http://api.chartlyrics.com/")]
    public string SongUrl { get; set; }
    [XmlElement(ElementName = "ArtistUrl", Namespace = "http://api.chartlyrics.com/")]
    public string ArtistUrl { get; set; }
    [XmlElement(ElementName = "Artist", Namespace = "http://api.chartlyrics.com/")]
    public string Artist { get; set; }
    [XmlElement(ElementName = "Song", Namespace = "http://api.chartlyrics.com/")]
    public string Song { get; set; }
    [XmlElement(ElementName = "SongRank", Namespace = "http://api.chartlyrics.com/")]
    public string SongRank { get; set; }
    [XmlElement(ElementName = "TrackChecksum", Namespace = "http://api.chartlyrics.com/")]
    public string TrackChecksum { get; set; }
    [XmlAttribute(AttributeName = "nil", Namespace = "http://www.w3.org/2001/XMLSchema-instance")]
    public string Nil { get; set; }
  }
}