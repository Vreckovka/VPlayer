using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Hqub.MusicBrainz.API.Entities;
using Newtonsoft.Json.Linq;


namespace VPlayer.Other
{
    public static class Extensions
    {
      

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

        //public static List<Song> CreateSongs(this List<Track> tracks, LocalMusicDatabase.Album album)
        //{
        //    List<Song> songs = new List<Song>();
        //    foreach (var track in tracks)
        //    {
        //        songs.Add(new Song()
        //        {
        //            Album = album,
        //            Name = track.Recording.Title,
        //            Length = (int)track.Recording.Length
        //        });
        //    }
        //    return songs;
        //}

        //public static XDocument ToXDocument(this XmlDocument document)
        //{
        //    return document.ToXDocument(LoadOptions.None);
        //}

        public static XDocument ToXDocument(this XmlDocument document, LoadOptions options)
        {
            using (XmlNodeReader reader = new XmlNodeReader(document))
            {
                return XDocument.Load(reader, options);
            }
        }

    }
}
