using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Drawing;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Hqub.MusicBrainz.API;
using Hqub.MusicBrainz.API.Entities;
using Hqub.MusicBrainz.API.Entities.Collections;
using unirest_net;
using unirest_net.http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VPlayer.LocalMusicDatabase;

namespace VPlayer.Other.AudioInfoDownloader
{
    public class AudioInfoDownloader
    {
        private static string api = "eJSq8XSeYR";
        private static string meta = "recordings+releases+tracks";

        static AudioInfoDownloader()
        {

        }

        #region FingerPrint methods

        /// <summary>
        /// Return Song by audio fingerPrint
        /// Returns song string []
        /// 0 - Title,
        /// 1 - ArtistMBID
        /// 2 - Album title
        /// </summary>
        /// <param name="trackPath"></param>
        /// <returns></returns>
        public static async Task<AudioInfo> GetTrackInfoByFingerPrint(string trackPath, AudioInfo pAudioInfo = null)
        {
            Task<AudioInfo> fpcalTask = new Task<AudioInfo>(() => RunFpcalc(trackPath));
            fpcalTask.Start();

            var audioInfo = await fpcalTask;

            if (audioInfo != null)
            {
                var query = $"https://api.acoustid.org/v2/lookup?" +
                            $"client={api}&" +
                            $"meta={meta}&" +
                            $"duration={audioInfo.Duration}&" +
                            $"fingerprint={audioInfo.FingerPrint}";


                HttpResponse<string> response = Unirest.get(query).asJson<string>();

                if (response.Code != 200)
                {
                    throw new Exception($"Response returns {response.Code}");
                }

                dynamic jObject = JObject.Parse(response.Body);
                var recordings = jObject.results[0].recordings;


                //Sometimes returns more then 1 recording
                dynamic bestRecording = null;
                var bestRecordingByDuration = GetBestMatch(recordings, audioInfo.Duration.ToString());

                if (pAudioInfo != null)
                {
                    bestRecording = GetBestMatch(recordings, pAudioInfo);
                }
                else
                {
                    bestRecording = bestRecordingByDuration;
                }

                var release = bestRecording.releases[0];

                if (!DatabaseManager.IsArtistInTheDatabase(bestRecording.artists[0]))
                {
                    await DatabaseManager.AddAllInfoOfArtistToDatabase(release.artists[0]);
                }


                return new AudioInfo()
                {
                    Title = bestRecording.title.ToString(),
                    ArtistMBID = bestRecording.artists[0].id.ToString(),
                    Album = release.title.ToString()
                };

            }

            return null;
        }

        /// <summary>
        /// Gets audio with similar duration as file
        /// </summary>
        /// <param name="matchings"></param>
        /// <param name="fileDuration"></param>
        /// <returns></returns>
        private static JToken GetBestMatch(JArray matchings, string fileDuration)
        {
            try
            {
                if (matchings.Count == 1)
                {
                    return matchings[0];
                }
                else if (matchings.Count > 1)
                {

                    List<KeyValuePair<JToken, string>> durations = new List<KeyValuePair<JToken, string>>();

                    for (int i = 0; i < matchings.Count; i++)
                    {
                        dynamic asd = matchings[i];
                        var duration = asd.duration;

                        if (duration != null)
                            durations.Add(new KeyValuePair<JToken, string>(matchings[i], duration.ToString()));
                    }

                    return durations.OrderByDescending(a => Levenshtein.Similarity(a.Value, fileDuration)).First().Key;
                }

                return null;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private static JToken GetBestMatch(JArray matchings, AudioInfo audioInfo)
        {
            try
            {
                if (matchings.Count == 1)
                {
                    return matchings[0];
                }

                if (matchings.Count > 1)
                {
                    if (audioInfo.Artist != null && audioInfo.Album != null)
                    {
                        List<KeyValuePair<JToken, string>> candidates = new List<KeyValuePair<JToken, string>>();
                        for (int i = 0; i < matchings.Count; i++)
                        {
                            dynamic jToken = matchings[i];
                            dynamic property = null;
                            dynamic property1 = null;

                            if (jToken.artists != null)
                                property = jToken.artists[0].name;

                            if (property != null)
                                property = property.ToString();

                            if (jToken.releases != null)
                                property1 = jToken.releases[0].title;

                            if (property1 != null)
                            {
                                property1 = property1.ToString();
                            }

                            if (property != null && property1 != null)
                                property += property1;

                            if (property != null)
                                candidates.Add(new KeyValuePair<JToken, string>(matchings[i], property.ToString()));
                        }

                        return candidates.OrderByDescending(a => Levenshtein.Similarity(a.Value, audioInfo.Artist + audioInfo.Album))
                            .First().Key;
                    }
                    else if (audioInfo.Artist != null)
                    {

                        List<KeyValuePair<JToken, string>> candidates = new List<KeyValuePair<JToken, string>>();

                        for (int i = 0; i < matchings.Count; i++)
                        {
                            dynamic jToken = matchings[i];
                            var property = jToken.artists[0].name;

                            if (property != null)
                                candidates.Add(new KeyValuePair<JToken, string>(matchings[i], property.ToString()));
                        }

                        return candidates.OrderByDescending(a => Levenshtein.Similarity(a.Value, audioInfo.Artist))
                            .First().Key;
                    }
                    else if (audioInfo.Album != null)
                    {
                        List<KeyValuePair<JToken, string>> candidates = new List<KeyValuePair<JToken, string>>();

                        for (int i = 0; i < matchings.Count; i++)
                        {
                            dynamic jToken = matchings[i];
                            var property = jToken.releases[0].title.ToString();

                            if (property != null)
                                candidates.Add(new KeyValuePair<JToken, string>(matchings[i], property.ToString()));
                        }

                        return candidates.OrderByDescending(a => Levenshtein.Similarity(a.Value, audioInfo.Album))
                            .First().Key;
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return null;
        }

        /// <summary>
        /// Run fpcal process for getting audio fingerprint
        /// </summary>
        /// <param name="trackPath"></param>
        /// <returns>Fingerprint of file</returns>
        private static AudioInfo RunFpcalc(string trackPath)
        {
            string fpacl = Environment.CurrentDirectory + "\\ChromaPrint\\fpcalc.exe";

            Process process = new Process();
            process.StartInfo.FileName = fpacl;
            process.StartInfo.Arguments = $"\"{trackPath}\"";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.Start();
            //* Read the output (or the error)
            string output = process.StandardOutput.ReadToEnd();
            string err = process.StandardError.ReadToEnd();

            if (err != "")
            {
                Console.WriteLine(err);
                return null;
            }

            process.WaitForExit();

            var outputs = output.Split('\n');

            return new AudioInfo()
            {
                FingerPrint = outputs[1].Replace("FINGERPRINT=", ""),
                Duration = Convert.ToInt32(outputs[0].Replace("\r", "").Replace("DURATION=", ""))
            };
        }

        #endregion

        #region MusicBrainz Lookups

        public static async Task<List<Album>> GetArtistsAlbums(LocalMusicDatabase.Artist artist)
        {

            List<Album> albums = new List<Album>();
            var groups = await ReleaseGroup.BrowseAsync("artist", artist.MusicBrainzId, 100, 0);

            foreach (var item in groups.Items.Where(g => IsOffical(g)).OrderBy(g => g.FirstReleaseDate))
            {
                var MBID = GetAlbumMBID(artist.Name, item.Title);
                Album album = new Album()
                {
                    AlbumFrontCoverURI = GetAlbumFrontConverURL(MBID),
                    Artist = artist,
                    MusicBrainzId = MBID,
                    Name = item.Title,
                    ReleaseDate = item.FirstReleaseDate,
                };

                album.Songs = (await LookUpMedium(artist, album))?.Tracks.CreateSongs(album);

                albums.Add(album);
                //Maximum rate for API
                Thread.Sleep(1200);

                if (album.Name != null && artist.Name != null)
                    Console.WriteLine($"Downloaded album {album.Name} of artists {artist.Name}");
            }

            return albums;
        }

        /// <summary>
        /// Returns true if album is official
        /// </summary>
        /// <param name="g"></param>
        /// <returns></returns>
        static bool IsOffical(ReleaseGroup g)
        {

            if (g.FirstReleaseDate == "")
                return false;
            return g.PrimaryType.Equals("album", StringComparison.OrdinalIgnoreCase)
                   && g.SecondaryTypes.Count == 0
                   && !string.IsNullOrEmpty(g.FirstReleaseDate);
        }

        public static async Task<Medium> LookUpMedium(LocalMusicDatabase.Artist artist, Album album)
        {

            // Build an advanced query to search for the release.
            var query = new QueryParameters<Release>()
                    {
                        {"arid", artist.MusicBrainzId},
                        {"release", album.Name},
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

                return medium;
            }

            return null;
        }

        /// <summary>
        /// Define is release is Copact disc
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
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
        /// <returns></returns>,
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
        #endregion

        #region FileLookups

        public static async Task<AudioInfo> TryGetSongInfoFromFile(string file)
        {
            var musicProp = await GetMusicProperties(file);

            if ((musicProp.Artist != "" && musicProp.Album != "") || (musicProp.AlbumArtist != "" && musicProp.Album != ""))
                return new AudioInfo()
                {
                    Album = musicProp.Album,
                    Artist = musicProp.Artist != "" ? musicProp.Artist : musicProp?.AlbumArtist,
                    Title = musicProp.Title
                };


            return null;
        }

        /// <summary>
        /// Getting song info from windows
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private static async Task<MusicProperties> GetMusicProperties(string path)
        {
            StorageFile file = await StorageFile.GetFileFromPathAsync(path);
            MusicProperties musicProperties = await file.Properties.GetMusicPropertiesAsync();

            return musicProperties;
        }

        #endregion
    }
}
