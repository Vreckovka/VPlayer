using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Hqub.MusicBrainz.API;
using Hqub.MusicBrainz.API.Entities;
using Newtonsoft.Json.Linq;
using unirest_net.http;
using VPlayer.AudioStorage;
using VPlayer.AudioStorage.Interfaces;
using VPlayer.AudioStorage.Models;
using VPlayer.Other;
using Artist = Hqub.MusicBrainz.API.Entities.Artist;
using Extensions = VPlayer.Other.Extensions;



namespace VPlayer.AudioInfoDownloader
{
    /// <summary>
    /// Class for getting info about audio file (music)
    /// </summary>
    public class AudioInfoDownloader
    {

        private string api = "eJSq8XSeYR";
        private string meta = "recordings+releases+tracks";

        private static AudioInfoDownloader _instance;
        public static AudioInfoDownloader Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new AudioInfoDownloader();
                }

                return _instance;
            }
        }

        public AudioInfoDownloader()
        {
            StorageManager.AlbumStored += AudioInfoDownloader_AlbumStored;
        }

        private void AudioInfoDownloader_AlbumStored(object sender, Album e)
        {
            Task.Run(async () =>
            {
                var album = await UpdateAlbum(e);

                using (IStorage storage = StorageManager.GetStorage())
                {
                    await storage.UpdateAlbum(album);
                }
            });
        }

        #region Info methods

        /// <summary>
        /// Gets audio info using file info method, if fails uses fingerprint method
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public async Task<AudioInfo> GetAudioInfo(string path)
        {
            AudioInfo audioInfo = null;

            Logger.Logger.Instance.Log(typeof(AudioInfoDownloader), $"Trying get info by from windows {path}");
            audioInfo = await GetAudioInfoByWindowsAsync(path);

            if (audioInfo != null)
                return audioInfo;
            else
                return await GetAudioInfoByFingerPrint(path);
        }

        #endregion

        #region FingerPrint methods

        /// <summary>
        /// Return audio info by fingerPrint
        /// <param name="path"></param>
        /// </summary>
        private async Task<AudioInfo> GetAudioInfoByFingerPrint(string path, AudioInfo pAudioInfo = null)
        {
            try
            {
                Logger.Logger.Instance.Log(typeof(AudioInfoDownloader), $"Getting info by fingerprint {path}");

                Task<AudioInfo> fpcalTask = new Task<AudioInfo>(() => RunFpcalc(path));
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
                        Logger.Logger.Instance.Log(typeof(AudioInfoDownloader), $"Response returns {response.Code} {path}");
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
                    AudioInfo newAudioInfo = new AudioInfo();


                    if (bestRecording.artists.Count > 0)
                    {
                        newAudioInfo = new AudioInfo()
                        {
                            Title = bestRecording.title.ToString(),
                            ArtistMbid = bestRecording.artists[0].id.ToString(),
                            Album = release.title.ToString(),
                            Artist = bestRecording.artists[0].name.ToString(),
                            DiskLocation = path
                        };
                    }
                    else
                    {
                        newAudioInfo = new AudioInfo()
                        {
                            Title = bestRecording.title.ToString(),
                            Album = release.title.ToString(),
                        };
                    }

                    Logger.Logger.Instance.Log(typeof(AudioInfoDownloader), $"Info was gotten by fingerprint {path} [{newAudioInfo}]");

                    return newAudioInfo;
                }
            }
            catch (Exception ex)
            {
                Logger.Logger.Instance.Log(typeof(AudioInfoDownloader), ex.Message);
            }

            return null;
        }

        /// <summary>
        /// Returns list of audioinfos from windows directory
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public async Task<List<AudioInfo>> GetAudioInfosByFingerprintDirectoryAsync(string path, bool subDirectories = false)
        {
            List<AudioInfo> audioInfos = new List<AudioInfo>();

            if (!subDirectories)
            {
                DirectoryInfo d = new DirectoryInfo(path); //Assuming Test is your Folder
                FileInfo[] Files = d.GetFiles("*.mp3"); //Getting Text files

                foreach (FileInfo file in Files)
                {
                    audioInfos.Add(await GetAudioInfoByFingerPrint(file.FullName));
                }
            }
            else
            {
                foreach (var directory in GetSubDirectories(path))
                {
                    audioInfos.AddRange(await GetAudioInfosByFingerprintDirectoryAsync(directory));
                }
            }

            return audioInfos;
        }

        public async Task<AudioStorage.Models.Artist> UpdateArtist(string artistName)
        {
            try
            {
                Logger.Logger.Instance.Log(typeof(AudioInfoDownloader), $"Updating artist's data {artistName}");

                var artists = await Artist.SearchAsync(artistName.Quote());

                Artist artist = null;

                if (artists.Items.Any(x => x.Score == 100))
                    artist = artists.Items.OrderByDescending(a => a.Score).First();
                else
                    artist = artists.Items.OrderByDescending(a => Extensions.Levenshtein.Similarity(a.Name, artistName)).First();


                Logger.Logger.Instance.Log(typeof(AudioInfoDownloader), $"Artist's data was updated {artistName}");


                //artist = await Artist.GetAsync(artist.Id, "artist-rels", "url-rels");

                return new AudioStorage.Models.Artist()
                {
                    Name = artist.Name,
                    MusicBrainzId = artist.Id
                };
            }
            catch (Exception ex)
            {
                Logger.Logger.Instance.Log(typeof(AudioInfoDownloader), ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Updates information about album
        /// </summary>
        /// <param name="album"></param>
        /// <returns></returns>
        public async Task<Album> UpdateAlbum(Album album)
        {
            var artistName = album.Artist.Name;
            try
            {
                Logger.Logger.Instance.Log(typeof(AudioInfoDownloader), $"Starting download album info {album.Name} - {album.Artist.Name}");

                // Search for an artist by name.
                var artists = await Artist.SearchAsync(album.Artist.Name.Quote());
                var artist = artists.Items.First();
                Release release;

                // Build an advanced query to search for the release.
                var query = new QueryParameters<Release>()
                {
                    { "arid", artist.Id },
                    { "release", album.Name },
                    { "type", "album" },
                    { "status", "official" }
                };

                // Search for a release by title.
                var releases = await Release.SearchAsync(query);

                if (releases.Count != 0)
                {
                    // Get the oldest release (remember to sort out items with no date set).
                    release = releases.Items.Where(r => r.Date != null && IsCompactDisc(r)).OrderBy(r => r.Date)
                        .First();

                    // Get detailed information of the release, including recordings.
                    release = await Release.GetAsync(release.Id, "recordings", "url-rels");

                    //// Get the medium associated with the release.
                    //var medium = release.Media.First();

                    //release = await Release.GetAsync(release.Id, "recordings", "url-rels");
                    
                }
                else
                {
                    Logger.Logger.Instance.Log(typeof(AudioInfoDownloader), $"Fails to find info about {album.Name} trying another method");
                    var albums = await GetArtistsAlbums(artist);

                    var bestAlbum = albums.OrderByDescending(x => Extensions.Levenshtein.Similarity(x.Name, album.Name))
                        .First();

                    query = new QueryParameters<Release>()
                    {
                        { "arid", artist.Id },
                        { "release", bestAlbum.Name },
                        { "type", "album" },
                        { "status", "official" }
                    };

                    releases = await Release.SearchAsync(query);
                        
                    release = releases.Items.Where(r => r.Date != null && IsCompactDisc(r)).OrderBy(r => r.Date)
                        .First();

                }

             
                album = new Album()
                {
                    AlbumId = album.AlbumId,
                    Name = release.Title,
                    MusicBrainzId = release.Id,
                    ReleaseDate = release.Date,
                    AlbumFrontCoverURI = await GetAlbumFrontConverURL(release.Id),
                    AlbumFrontCoverBLOB = await GetAlbumFrontConverBLOB(release.Id),
                };

                Logger.Logger.Instance.Log(typeof(AudioInfoDownloader),
                    $"Album info was succesfully download {album.Name} - {artistName}");
                
                return album;

            }
            catch (Exception ex)
            {
                Logger.Logger.Instance.Log(typeof(AudioInfoDownloader), ex.Message);
                return null;
            }
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

                    return durations.OrderByDescending(a => Extensions.Levenshtein.Similarity(a.Value, fileDuration)).First().Key;
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

                        return candidates.OrderByDescending(a => Extensions.Levenshtein.Similarity(a.Value, audioInfo.Artist + audioInfo.Album))
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

                        return candidates.OrderByDescending(a => Extensions.Levenshtein.Similarity(a.Value, audioInfo.Artist))
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

                        return candidates.OrderByDescending(a => Extensions.Levenshtein.Similarity(a.Value, audioInfo.Album))
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


        public static async Task<List<Album>> GetArtistsAlbums(Artist artist)
        {
            List<Album> albums = new List<Album>();
            var groups = await ReleaseGroup.BrowseAsync("artist", artist.Id, 100, 0);

            foreach (var item in groups.Items.Where(g => IsOffical(g)).OrderBy(g => g.FirstReleaseDate))
            {
                Album album = new Album()
                {
                    MusicBrainzId = item.Id,
                    Name = item.Title,
                };

                albums.Add(album);

                if (album.Name != null && artist.Name != null)
                    Logger.Logger.Instance.Log(typeof(AudioInfoDownloader), $"Downloaded album {album.Name} of artists {artist.Name}");
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

        //public static async Task<Medium> LookUpMedium(LocalMusicDatabase.Artist artist, Album album)
        //{
        //    // Build an advanced query to search for the release.
        //    var query = new QueryParameters<Release>()
        //            {
        //                {"arid", artist.MusicBrainzId},
        //                {"release", album.Name},
        //                {"type", "album"},
        //                {"status", "official"}
        //            };

        //    // Search for a release by title.
        //    var releases = await Release.SearchAsync(query);

        //    Release release = null;

        //    if (releases.Count > 0)
        //        // Get the oldest release (remember to sort out items with no date set).
        //        release = releases.Items.Where(r => r.Date != null && IsCompactDisc(r)).OrderBy(r => r.Date)
        //            .First();


        //    if (release != null)
        //    {
        //        // Get detailed information of the release, including recordings.
        //        release = await Release.GetAsync(release.Id, "recordings", "url-rels");

        //        // Get the medium associated with the release.
        //        var medium = release.Media.First();

        //        return medium;
        //    }

        //    return null;
        //}

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
        private static Task<string> GetAlbumFrontConverURL(string MBID)
        {
            return Task.Run(() =>
                {
                    try
                    {
                        string apiURL = "http://coverartarchive.org//release/" + $"{MBID}";
                        HttpResponse<string> response = Unirest.get(apiURL).asJson<string>();

                        dynamic stuff = JObject.Parse(response.Body);

                        var converUrl = stuff.images[0].thumbnails.small;
                        if (converUrl == null)
                            converUrl = stuff.images[0].image;

                        return (string)converUrl.ToString();
                    }
                    catch (Exception ex)
                    {
                        Logger.Logger.Instance.Log(typeof(AudioInfoDownloader), ex.Message);
                        return null;
                    }
                }
            );
        }

        /// <summary>
        /// Returns byte[] for album front image
        /// </summary>
        /// <param name="MBID"></param>
        /// <returns></returns>,
        private static Task<byte[]> GetAlbumFrontConverBLOB(string MBID, string url = null)
        {
            return Task.Run(() =>
                {
                    try
                    {
                        var webClient = new WebClient();
                        if (url == null)
                        {
                            string apiURL = "http://coverartarchive.org//release/" + $"{MBID}";
                            HttpResponse<string> response = Unirest.get(apiURL).asJson<string>();

                            dynamic stuff = JObject.Parse(response.Body);

                            var converUrl = stuff.images[0].thumbnails.small;
                            if (converUrl == null)
                                converUrl = stuff.images[0].image;

                            return (byte[])webClient.DownloadData(converUrl.ToString());
                        }
                        else
                        {
                            return webClient.DownloadData(url);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Logger.Instance.Log(typeof(AudioInfoDownloader), ex.Message);
                        return null;
                    }
                }
            );
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

        /// <summary>
        /// Returns audio info from file info
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private async Task<AudioInfo> GetAudioInfoByWindowsAsync(string path)
        {
            Logger.Logger.Instance.Log(typeof(AudioInfoDownloader), $"Getting info from windows {path}");
            var musicProp = await GetAudioWindowsPropertiesAsync(path);

            if ((musicProp.Artist != "" && musicProp.Album != "") || (musicProp.AlbumArtist != "" && musicProp.Album != ""))
            {
                var newAudioInfo = new AudioInfo()
                {
                    Album = musicProp.Album,
                    Artist = musicProp.Artist != "" ? musicProp.Artist : musicProp?.AlbumArtist,
                    Title = musicProp.Title,
                    DiskLocation = path
                };

                Logger.Logger.Instance.Log(typeof(AudioInfoDownloader), $"Info was gotten by windows {path} [{newAudioInfo}]");

                return newAudioInfo;
            }


            Logger.Logger.Instance.Log(typeof(AudioInfoDownloader), $"Info was not gotten fully {path}");
            return null;
        }

        /// <summary>
        /// Getting song info from windows 10
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private async Task<MusicProperties> GetAudioWindowsPropertiesAsync(string path)
        {
            try
            {
                StorageFile file = await StorageFile.GetFileFromPathAsync(path);
                MusicProperties musicProperties = await file.Properties.GetMusicPropertiesAsync();

                return musicProperties;
            }
            catch (Exception ex)
            {
                Logger.Logger.Instance.Log(typeof(AudioInfoDownloader), ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Returns list of audioinfos from windows directory
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public async Task<List<AudioInfo>> GetAudioInfosFromWindowsDirectoryAsync(string path, bool subDirectories = false)
        {
            List<AudioInfo> audioInfos = new List<AudioInfo>();

            if (!subDirectories)
            {
                DirectoryInfo d = new DirectoryInfo(path);
                FileInfo[] Files = d.GetFiles("*.mp3");

                foreach (FileInfo file in Files)
                {
                    audioInfos.Add(await GetAudioInfoByWindowsAsync(file.FullName));
                }
            }
            else
            {
                foreach (var directory in GetSubDirectories(path))
                {
                    audioInfos.AddRange(await GetAudioInfosFromWindowsDirectoryAsync(directory));
                }
            }

            return audioInfos;
        }

        #endregion

        #region Directory methods
        public List<string> GetSubDirectories(string rootDirectory)
        {
            List<string> directories = new List<string>();
            string[] subdirectoryEntries = Directory.GetDirectories(rootDirectory);

            foreach (string subdirectory in subdirectoryEntries)
            {
                LoadSubDirs(subdirectory, ref directories);
            }

            return directories;

        }

        private void LoadSubDirs(string dir, ref List<string> directories)
        {
            directories.Add(dir);

            string[] subdirectoryEntries = Directory.GetDirectories(dir);
            foreach (string subdirectory in subdirectoryEntries)
            {
                LoadSubDirs(subdirectory, ref directories);
            }

        }
        #endregion
    }
}
