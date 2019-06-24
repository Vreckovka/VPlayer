using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
//using Hqub.MusicBrainz.API.Entities;
using Newtonsoft.Json.Linq;
//using VPlayer.LocalMusicDatabase;

namespace VPlayer.Other
{
    public static class Extensions
    {
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
    }

    public static class Levenshtein
    {
        /// <summary>
        /// Convert strings to lowercase before comparing.
        /// </summary>
        public static bool IrgnoreCase = true;

        /// <summary>
        /// Compute the similarity of two strings using the Levenshtein distance.
        /// </summary>
        /// <param name="s">The first string.</param>
        /// <param name="t">The second string.</param>
        /// <returns>A floating point value between 0.0 and 1.0</returns>
        public static float Similarity(string s, string t)
        {
            int maxLen = Math.Max(s.Length, t.Length);

            if (maxLen == 0)
            {
                return 1.0f;
            }

            if (IrgnoreCase)
            {
                s = s.ToLowerInvariant();
                t = t.ToLowerInvariant();
            }

            float dis = LevenshteinDistance(s, t);

            return 1.0f - dis / maxLen;
        }

        public static int LevenshteinDistance(this string s, string t)
        {
            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1]; // matrix
            int cost = 0;

            if (n == 0) return m;
            if (m == 0) return n;

            // Initialize.
            for (int i = 0; i <= n; d[i, 0] = i++) ;
            for (int j = 0; j <= m; d[0, j] = j++) ;

            // Find min distance.
            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    cost = (t.Substring(j - 1, 1) == s.Substring(i - 1, 1) ? 0 : 1);
                    d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
                }
            }

            return d[n, m];
        }
    }
}
