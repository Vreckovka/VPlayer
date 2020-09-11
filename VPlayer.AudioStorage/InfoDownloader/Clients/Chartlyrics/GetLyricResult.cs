/* 
 Licensed under the Apache License, Version 2.0

 http://www.apache.org/licenses/LICENSE-2.0
 */
using System;
using System.Xml.Serialization;
using System.Collections.Generic;
namespace Xml2CSharp
{
  [XmlRoot(ElementName = "GetLyricResult", Namespace = "http://api.chartlyrics.com/")]
  public class GetLyricResult
  {
    [XmlElement(ElementName = "TrackId", Namespace = "http://api.chartlyrics.com/")]
    public string TrackId { get; set; }
    [XmlElement(ElementName = "LyricChecksum", Namespace = "http://api.chartlyrics.com/")]
    public string LyricChecksum { get; set; }
    [XmlElement(ElementName = "LyricId", Namespace = "http://api.chartlyrics.com/")]
    public string LyricId { get; set; }
    [XmlElement(ElementName = "LyricSong", Namespace = "http://api.chartlyrics.com/")]
    public string LyricSong { get; set; }
    [XmlElement(ElementName = "LyricArtist", Namespace = "http://api.chartlyrics.com/")]
    public string LyricArtist { get; set; }
    [XmlElement(ElementName = "LyricUrl", Namespace = "http://api.chartlyrics.com/")]
    public string LyricUrl { get; set; }
    [XmlElement(ElementName = "LyricCovertArtUrl", Namespace = "http://api.chartlyrics.com/")]
    public string LyricCovertArtUrl { get; set; }
    [XmlElement(ElementName = "LyricRank", Namespace = "http://api.chartlyrics.com/")]
    public string LyricRank { get; set; }
    [XmlElement(ElementName = "LyricCorrectUrl", Namespace = "http://api.chartlyrics.com/")]
    public string LyricCorrectUrl { get; set; }
    [XmlElement(ElementName = "Lyric", Namespace = "http://api.chartlyrics.com/")]
    public string Lyric { get; set; }
    [XmlAttribute(AttributeName = "xsd", Namespace = "http://www.w3.org/2000/xmlns/")]
    public string Xsd { get; set; }
    [XmlAttribute(AttributeName = "xsi", Namespace = "http://www.w3.org/2000/xmlns/")]
    public string Xsi { get; set; }
    [XmlAttribute(AttributeName = "xmlns")]
    public string Xmlns { get; set; }
  }
}

