using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Hqub.MusicBrainz.API;
using Hqub.MusicBrainz.API.Entities;
using Hqub.MusicBrainz.API.Entities.Collections;
using MultiMusicPlayer.LocalMusicDatabase;
using unirest_net;
using unirest_net.http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MultiMusicPlayer.Other
{

    public class AudioInfoDownloader
    {
        private static LocalMusicDbContext _localMusicDbContext;
        private static string api = "eJSq8XSeYR";
        private static string meta = "recordings";

        static AudioInfoDownloader()
        {
            _localMusicDbContext = new LocalMusicDbContext();
        }

        private static void AudioInfoDownloader_FingerPrintGenerated(object sender, string e)
        {
            var query = $"https://api.acoustid.org/v2/lookup?" +
                        $"client={api}&" +
                        $"meta={meta}&" +
                        $"duration=428&" +
                        $"fingerprint={e}";

            string apiURL = query;
            HttpResponse<string> response = Unirest.get(apiURL).asJson<string>();

            dynamic stuff = JObject.Parse(response.Body);
            var asdas = stuff.results[0];
        }

        #region FingerPrint methods

        /// <summary>
        /// Return Song by audio fingerPrint
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static async Task<Song> GetTrackInfoByFingerPrint(string trackPath)
        {
            Task<string[]> fpcalTask = new Task<string[]>(() => RunFpcalc(trackPath));
            fpcalTask.Start();
            var fingerPrint = await fpcalTask;




            Song song = new Song();
            return song;
        }

        /// <summary>
        /// Run fpcal process for getting audio fingerprint
        /// </summary>
        /// <param name="trackPath"></param>
        /// <returns>Fingerprint of file</returns>
        private static string[] RunFpcalc(string trackPath)
        {
            string fpacl = "C:\\Users\\Roman Pecho\\Desktop\\chromaprint-fpcalc-1.4.3-windows-x86_64\\fpcalc.exe";

            Process process = new Process();
            process.StartInfo.FileName = fpacl;
            process.StartInfo.Arguments = $"\"{trackPath}\""; 
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.Start();
            //* Read the output (or the error)
            string output = process.StandardOutput.ReadToEnd();
            Console.WriteLine(output);
            string err = process.StandardError.ReadToEnd();
            Console.WriteLine(err);
            process.WaitForExit();

            var fingerPrint = output.Split('\n');


            return new string[1];
        }

        #endregion

        public static async Task<Album> UpdateDatabase(string artistName, string albumName)
        {
            Album album = null;
            var albumDb = (from x in _localMusicDbContext.Albums
                           where x.Artist.Name == artistName
                           where x.Name == albumName
                           select x).FirstOrDefault();

            if (albumDb == null)
            {
                Thread.Sleep(1500);

                var artist = await UpdateArtist(artistName);

                // Build an advanced query to search for the release.
                var query = new QueryParameters<Release>()
                {
                    {"arid", artist.MusicBrainzId},
                    {"release", albumName},
                    {"type", "album"},
                    {"status", "official"}
                };

                // Search for a release by title.
                var releases = await Release.SearchAsync(query);

                Release release = null;

                if (releases.Count > 0)
                    // Get the oldest release (remember to sort out items with no date set).
                    release = releases.Items.Where(r => r.Date != null && IsCompactDisc(r)).OrderBy(r => r.Date)
                        .First();


                if (release != null)
                {
                    // Get detailed information of the release, including recordings.
                    release = await Release.GetAsync(release.Id, "recordings", "url-rels");

                    // Get the medium associated with the release.
                    var medium = release.Media.First();

                    album = new Album()
                    {
                        AlbumFrontCoverURI = GetAlbumFrontConverURL(releases.Items),
                        Artist = artist,
                        Name = release.Title,
                        ReleaseDate = release.Date,
                    };

                    var songs = CreateSongs(medium.Tracks, album);
                    album.Songs = songs;

                    _localMusicDbContext.Albums.Add(album);
                    await _localMusicDbContext.SaveChangesAsync();
                }
            }

            return album;
        }
        public static async Task SetDiskLocationToSong(string song, string album, string artist, string path)
        {
            var songDb = (from x in _localMusicDbContext.Songs
                          join y in _localMusicDbContext.Albums on x.Album equals y
                          join z in _localMusicDbContext.Artists on y.Artist equals z
                          where x.Name == song
                          where y.Name == album
                          where z.Name == artist
                          select x).FirstOrDefault();

            if (songDb != null)
            {
                songDb.DiskLocation = path;
                await _localMusicDbContext.SaveChangesAsync();
            }
        }
        private static List<Song> CreateSongs(List<Track> tracks, LocalMusicDatabase.Album album)
        {
            List<Song> songs = new List<Song>();
            foreach (var track in tracks)
            {
                songs.Add(new Song()
                {
                    Album = album,
                    Name = track.Recording.Title,
                    Length = (int)track.Recording.Length
                });
            }

            return songs;
        }
        private static string GetAlbumFrontConverURL(List<Release> MBIDList)
        {
            string album = null;
            foreach (var MBID in MBIDList)
            {
                try
                {
                    string apiURL = "http://coverartarchive.org//release/" + $"{MBID.Id}";
                    HttpResponse<string> response = Unirest.get(apiURL).asJson<string>();

                    dynamic stuff = JObject.Parse(response.Body);
                    return stuff.images[0].image;
                }
                catch (Exception)
                {
                }
            }

            return album;
        }
        private static async Task<LocalDatabase.Artist> UpdateArtist(string artistName)
        {
            var artist = (from x in _localMusicDbContext.Artists where x.Name == artistName select x).FirstOrDefault();

            if (artist == null)
            {
                var artists = await Artist.SearchAsync(artistName.Quote(), 20);

                var artistBrainz = artists.Items.OrderByDescending(a => Levenshtein.Similarity(a.Name, artistName)).First();

                artist = new LocalDatabase.Artist()
                {
                    Name = artistBrainz.Name,
                    MusicBrainzId = artistBrainz.Id
                };

                _localMusicDbContext.Artists.Add(artist);
                _localMusicDbContext.SaveChanges();
            }

            return artist;
        }

        private static bool IsCompactDisc(Release r)
        {
            if (r.Media == null || r.Media.Count == 0)
            {
                return false;
            }

            return r.Media[0].Format == "CD";
        }

        /// <summary>
        /// Returns URL for album front image
        /// </summary>
        /// <param name="MBID"></param>
        /// <returns></returns>
        private static string GetAlbumFrontConverURL(string MBID)
        {
            try
            {
                string apiURL = "http://coverartarchive.org//release/" + $"{MBID}";
                HttpResponse<string> response = Unirest.get(apiURL).asJson<string>();

                dynamic stuff = JObject.Parse(response.Body);
                return stuff.images[0].image;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Returns MBID of album
        /// </summary>
        private static string GetAlbumMBID(string artist, string album)
        {
            var m_strFilePath = "http://musicbrainz.org/ws/2/release/?query=artist:" +
                                $"{artist}" +
                                "%20AND%20" +
                                $"title:{album}";
            string xmlStr;
            using (var wc = new WebClient())
            {
                //Need meaningful user-agent string.
                wc.Headers.Add("user-agent", "Hqub.MusicBrainz/2.0");
                xmlStr = wc.DownloadString(m_strFilePath);
            }

            XDocument xDocument = XDocument.Parse(xmlStr);
            var desc = (xDocument.Descendants(XName.Get("release", @"http://musicbrainz.org/ns/mmd-2.0#"))).ToList();

            if (desc.Count > 0)
                return desc[0].FirstAttribute.Value;
            return "";
        }

    }
}
