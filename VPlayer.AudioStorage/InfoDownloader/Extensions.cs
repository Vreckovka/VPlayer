using Newtonsoft.Json.Linq;
using System;
using System.Xml;
using System.Xml.Linq;

//using Hqub.MusicBrainz.API.Entities;

//using VPlayer.LocalMusicDatabase;

namespace VPlayer.AudioStorage.InfoDownloader
{
  public static class Extensions
  {
    #region Methods

    public static string GetDate(this JObject jObject)
    {
      dynamic obj = jObject;

      if (obj.day.ToString() != "" && obj.month.ToString() != "" && obj.year.ToString() != "")
        return $"{obj.day.ToString()}.{obj.month.ToString()}.{obj.year.ToString()}";
      else if (obj.month.ToString() != "" && obj.year.ToString() != "")
      {
        return $"{obj.month.ToString()}.{obj.year.ToString()}";
      }
      else if (obj.day.ToString() != "" && obj.year.ToString() != "")
      {
        return $"{obj.day.ToString()}.{obj.year.ToString()}";
      }

      return null;
    }

    public static string Quote(this string s)
    {
      if (string.IsNullOrEmpty(s))
      {
        return "";
      }

      if (s.IndexOf(' ') < 0)
      {
        return s;
      }

      return "\"" + s + "\"";
    }

    public static XDocument ToXDocument(this XmlDocument document)
    {
      return document.ToXDocument(LoadOptions.None);
    }

    public static XDocument ToXDocument(this XmlDocument document, LoadOptions options)
    {
      using (XmlNodeReader reader = new XmlNodeReader(document))
      {
        return XDocument.Load(reader, options);
      }
    }

    #endregion Methods
  }
}