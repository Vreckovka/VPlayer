using Hqub.MusicBrainz.API;
using Hqub.MusicBrainz.API.Entities;
using Newtonsoft.Json.Linq;
using Ninject;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Reactive.Subjects;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using unirest_net.http;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.InfoDownloader.Models;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Artist = Hqub.MusicBrainz.API.Entities.Artist;
using System.Drawing;
using System.Drawing.Imaging;
using System.Net.Http;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Windows.Media;
using Windows.ApplicationModel.Contacts;
using Windows.System;
using VCore.Helpers;
using VPlayer.AudioStorage.InfoDownloader.Clients.Chartlyrics;
using Xml2CSharp;

namespace VPlayer.AudioStorage.InfoDownloader
{
  /// <summary>
  /// Class for getting info about audio file (music)
  /// </summary>
  public class AudioInfoDownloader : IInitializable
  {
    #region Fields

    private static SemaphoreSlim musibrainzAPISempathore = new SemaphoreSlim(1, 1);
    private string api = "eJSq8XSeYR";

    private string exceedMsg = "Your requests are exceeding the " +
                               "allowable rate limit. " +
                               "Please see http://wiki.musicbrainz.org/XMLWebService " +
                               "for more information.";

    private string meta = "recordings+releases+tracks";
    private string[] supportedItems = new string[] { "*.mp3", "*.flac" };

    #endregion Fields

    #region Constructors

    public AudioInfoDownloader()
    {
    }

    #endregion Constructors

    #region Properties

    public ReplaySubject<object> ItemUpdated { get; } = new ReplaySubject<object>(10);

    #endregion Properties

    #region Events

    public event EventHandler<List<AlbumCover>> CoversDownloaded;

    public event EventHandler<List<AudioInfo>> SubdirectoryLoaded;

    #endregion Events

    #region UpdateItem

    public void UpdateItem(dynamic item)
    {
      UpdateItem(item);
    }

    private void UpdateItem(DomainClasses.Artist artist)
    {
      Task.Run(async () =>
      {
        try
        {
          var updateArtist = await UpdateArtist(artist.Name);

          if (updateArtist != null)
          {
            updateArtist.Id = artist.Id;
          }

          ItemUpdated.OnNext(artist);
        }
        catch (Exception ex)
        {
          Logger.Logger.Instance.Log(Logger.MessageType.Error, ex.Message);
        }
      });
    }

    private void UpdateItem(Album album)
    {
      Task.Run(async () =>
      {
        try
        {


          var updateAlbum = await UpdateAlbum(album);

          if (updateAlbum != null)
          {
            updateAlbum.Id = album.Id;

            updateAlbum.InfoDownloadStatus = InfoDownloadStatus.Downloaded;

            ItemUpdated.OnNext(updateAlbum);
          }
          else
          {
            album.InfoDownloadStatus = InfoDownloadStatus.UnableToFind;
            ItemUpdated.OnNext(album);
          }
        }
        catch (Exception ex)
        {
          Logger.Logger.Instance.Log(Logger.MessageType.Error, ex.Message);
        }
      });
    }

    #endregion UpdateItem

    #region Info methods

    /// <summary>
    /// Gets audio info using file info method, if fails uses fingerprint method
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public AudioInfo GetAudioInfo(string path)
    {
      AudioInfo audioInfo = null;

      audioInfo = GetAudioInfoByWindowsAsync(path);
      //GetMediaInfoo();

      if (audioInfo == null || (audioInfo.Artist == null && audioInfo.Album == null && audioInfo.Artist == "" && audioInfo.Album == ""))
      {
        Console.WriteLine(path + " NIC");
        //var fingerPrintAudioInfo = await Task.Run(() => GetAudioInfoByFingerPrint(path));

        //if (fingerPrintAudioInfo != null)
        //{
        //  Logger.Logger.Instance.Log(Logger.MessageType.Success, $"Audio info was gotten by fingerprint {path}");
        //  return fingerPrintAudioInfo;
        //}
      }

      return audioInfo;
    }

    private Semaphore semaphore = new Semaphore(15, 15);

    public void GetAudioInfosFromDirectory(string directoryPath, bool getSubDirectories = false)
    {
      try
      {

        //semaphore.WaitOne();

        List<AudioInfo> audioInfos = new List<AudioInfo>();

        List<Task> tasks = new List<Task>();

        DirectoryInfo d = new DirectoryInfo(directoryPath);

        FileInfo[] files = supportedItems.SelectMany(ext => d.GetFiles(ext)).ToArray();

        if (getSubDirectories)
        {
          var subDirectories = Directory.GetDirectories(directoryPath);

          foreach (var directory in subDirectories)
          {
            GetAudioInfosFromDirectory(directory, true);
            //var task = GetAudioInfosFromDirectory(directory, true);
            //tasks.Add(task);
          }
        }

        foreach (FileInfo file in files)
        {
          audioInfos.Add(GetAudioInfo(file.FullName));
        }

        OnSubdirectoryLoaded(audioInfos);

        foreach (var task in tasks)
        {
          task.Start();
        }


        Task.WaitAll(tasks.ToArray());
      }
      catch (Exception ex)
      {
        Logger.Logger.Instance.Log(Logger.MessageType.Error, ex.Message);
      }
      finally
      {
        //semaphore.Release();
      }
    }

    #endregion Info methods

    #region MusicBrainz Lookups

    private KeyValuePair<string, List<Album>> _actualArtist;

    /// <summary>
    /// Updates information about album
    /// </summary>
    /// <param name="album"></param>
    /// <returns></returns>
    private bool apiExeed;

    /// <summary>
    /// Returns all avaible album covers
    /// </summary>
    /// <param name="MBID"></param>
    /// <returns></returns>
    public Task<List<AlbumCover>> GetAlbumFrontCoversUrls(string MBID, CancellationToken cancellationToken)
    {
      return Task.Run<List<AlbumCover>>(async () =>
      {
        try
        {
          List<AlbumCover> covers = new List<AlbumCover>();
          string apiURL = "http://coverartarchive.org//release/" + $"{MBID}";
          HttpResponse<string> response = await Task.Run(() => Unirest.get(apiURL).asJson<string>());

          if (response.Code != 200)
          {
            if (response.Code == 502)
            {
              Logger.Logger.Instance.Log(Logger.MessageType.Warning,
                $"Album cover was not found {MBID} trying again, response code {response.Code}");
              return await GetAlbumFrontCoversUrls(MBID, cancellationToken);
            }

            Logger.Logger.Instance.Log(Logger.MessageType.Warning,
              $"Album cover was not found {MBID} resposne code {response.Code}");
            return null;
          }

          dynamic stuff = JObject.Parse(response.Body);

          if (stuff.images != null)
          {
            foreach (var image in stuff.images)
            {
              foreach (var thumbnail in image.thumbnails)
              {
                var foo = thumbnail.ToString().Split('\"');

                covers.Add(new AlbumCover()
                {
                  Mbid = MBID,
                  Type = foo[1],
                  Url = foo[3],
                });
              }

              var imageImage = image.image;
              if (imageImage != null)
              {
                covers.Add(new AlbumCover()
                {
                  Mbid = MBID,
                  Type = image.type,
                  Url = imageImage,
                });
              }
            }
          }
          else
          {
            Logger.Logger.Instance.Log(Logger.MessageType.Warning,
              $"Album cover was not found {MBID}");
            return null;
          }

          OnCoversDownloaded(covers);
          return covers;
        }
        catch (Exception ex)
        {
          Logger.Logger.Instance.Log(ex);
          return null;
        }
      }, cancellationToken);
    }

    public Task<List<AlbumCover>> GetAlbumFrontCoversUrls(Album album, CancellationToken cancellationToken)
    {
      return Task.Run(async () =>
      {
        try
        {
          List<AlbumCover> covers = new List<AlbumCover>();

          DomainClasses.Artist newArtist = album.Artist;

          if (album.Artist.MusicBrainzId == null)
          {
            newArtist = await UpdateArtist(album.Artist.Name);

            if (newArtist == null)
            {
              Logger.Logger.Instance.Log(Logger.MessageType.Warning,
                "Unable to get album covers, artist was not found");
              return null;
            }
            else;

            //Task.Run(() => AudioStorage.StorageManager.GetStorage().UpdateArtist(newArtist));
          }

          // Build an advanced query to search for the release.
          var query = new QueryParameters<Release>()
          {
            {"arid", newArtist.MusicBrainzId},
            {"release", album.Name},
            {"type", "album"},
            {"status", "official"}
          };

          // Search for a release by title.
          var releases = await Release.SearchAsync(query);

          foreach (var realease in releases.Items)
          {
            var newCovers = await GetAlbumFrontCoversUrls(realease.Id, cancellationToken);
            if (newCovers != null)
            {
              covers.AddRange(newCovers);
            }
          }

          Logger.Logger.Instance.Log(Logger.MessageType.Success, "Album covers was found");
          return covers;
        }
        catch (Exception ex)
        {
          Logger.Logger.Instance.Log(Logger.MessageType.Error, ex.Message);
          return null;
        }
      }, cancellationToken);
    }

    public Task<List<Album>> GetArtistsAlbums(string artistMbid)
    {
      return Task<List<Album>>.Run(async () =>
      {
        try
        {
          // Make sure that TLS 1.2 is available.
          ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

          // Get path for local file cache.
          var location = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

          var client = new MusicBrainzClient()
          {

          };

          List<Album> albums = new List<Album>();
          var groups = await client.ReleaseGroups.BrowseAsync("artist", artistMbid, 100, 0);

          foreach (var item in groups.Items.Where(g => IsOffical(g)).OrderBy(g => g.FirstReleaseDate))
          {
            Album album = new Album()
            {
              MusicBrainzId = item.Id,
              Name = item.Title,
            };

            albums.Add(album);
          }

          return albums;
        }
        catch (Exception ex)
        {
          if (ex.Message == exceedMsg)
          {
            return await GetArtistsAlbums(artistMbid);
          }
          else
          {
            Logger.Logger.Instance.Log(Logger.MessageType.Error, ex.Message);
            return null;
          }
        }
      });
    }

    public async Task<Album> UpdateAlbum(Album album)
    {
      try
      {
        var albumName = album.Name;

        // The album known sometimes as The Black Album is already in this database,
        // it is correctly named simply Metallica. This is because The Black Album is only a fan-composed name
        if (albumName.ToLower().Contains("black album") && album.Artist.Name.ToLower() == "metallica")
        {
          albumName = "Metallica";
        }

        await musibrainzAPISempathore.WaitAsync();



        album.InfoDownloadStatus = InfoDownloadStatus.Downloading;
        ItemUpdated.OnNext(album);
        Logger.Logger.Instance.Log(Logger.MessageType.Inform, $"Downloading album info {album.Name}");

        string artistMbid = "";
        string artistName = "";
        apiExeed = false;

        //Musicbrainz API client need its (not tested if not)
        ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

        var client = new MusicBrainzClient();

        artistName = album.Artist.Name;

        if (!string.IsNullOrEmpty(artistName))
        {
          // Search for an artist by name.
          var artists = await client.Artists.SearchAsync(artistName.Quote());
          var artist = artists.Items.FirstOrDefault();

          if (artist == null)
          {
            Logger.Logger.Instance.Log(Logger.MessageType.Warning,
              $"Album info was not downloaded {album.Name} - {artistName}");
            return null;
          }
          else
          {
            artistMbid = artist.Id;
          }
        }
        else if (!string.IsNullOrEmpty(album.Artist.MusicBrainzId))
          artistMbid = album.Artist.MusicBrainzId;


        Release release = null;
        Album bestAlbum = null;

        // Build an advanced query to search for the release.
        var query = new QueryParameters<Release>()
        {
          {"arid", artistMbid},
          {"release", albumName},
          {"type", "album"},
          {"status", "official"}
        };

        if (string.IsNullOrEmpty(artistMbid) && string.IsNullOrEmpty(albumName))
        {
          Logger.Logger.Instance.Log(Logger.MessageType.Warning, "Artist and album is null or empty");
          return null;
        }

        if (!string.IsNullOrEmpty(artistMbid))
          query.Add("arid", artistMbid);

        if (!string.IsNullOrEmpty(albumName))
          query.Add("release", albumName);

        query.Add("type", "album");
        query.Add("status", "official");

        // Search for a release by title.
        var releases = await client.Releases.SearchAsync(query);

        if (releases.Count != 0)
        {
          // Get the oldest release (remember to sort out items with no date set).
          release = releases.Items.Where(r => r.Date != null && IsCompactDisc(r)).OrderBy(r => r.Date)
            .FirstOrDefault();

          if (release == null)
          {
            Logger.Logger.Instance.Log(Logger.MessageType.Warning,
              $"Album info was not downloaded {album.Name} - {artistName}");
            return null;
          }

          // Get detailed information of the release, including recordings.
          release = await client.Releases.GetAsync(release.Id, "recordings", "url-rels");

          //// Get the medium associated with the release.
          //var medium = release.Media.First();

          //release = await Release.GetAsync(release.Id, "recordings", "url-rels");
        }
        else
        {
          if (_actualArtist.Key == null)
          {
            _actualArtist = new KeyValuePair<string, List<Album>>(artistMbid, await GetArtistsAlbums(artistMbid));
          }
          else if (_actualArtist.Key != artistMbid)
          {
            _actualArtist = new KeyValuePair<string, List<Album>>(artistMbid, await GetArtistsAlbums(artistMbid));
          }

          if (_actualArtist.Value != null && _actualArtist.Value.Count != 0)
          {
            bestAlbum = _actualArtist.Value
              .OrderByDescending(x => Levenshtein.Similarity(x.Name, album.Name)).First();

            if (bestAlbum.Name.LevenshteinDistance(album.Name) > album.Name.Length / 3)
            {
              var split = album.Name.Split('(');
              var splittedName = split[0];

              var albumsLow = _actualArtist.Value
                .Select(x => x.Name.ToLower().Replace(" ", "").Replace(".", "").Replace(",", "")).ToList();
              var albumLow = albumName.ToLower().Replace(" ", "").Replace(".", "").Replace(",", "");

              if (_actualArtist.Value.Any(x =>
                x.Name.LevenshteinDistance(albumName) < (splittedName.Length / 3)))
              {
                bestAlbum.Name = _actualArtist.Value
                  .Where(x => x.Name.LevenshteinDistance(albumName) < (splittedName.Length / 3))
                  .Select(x => x.Name).First();
              }
              else if (albumsLow.Any(x => x.Contains(albumLow)))
              {
                var name = albumsLow
                  .Where(x => x.Contains(albumLow))
                  .Select(x => x).First();
                bestAlbum.Name = _actualArtist.Value
                  .OrderByDescending(x => Levenshtein.Similarity(x.Name, name)).First().Name;
              }
              else if (albumsLow.Any(x => albumLow.Contains(x)))
              {
                var name = albumsLow.First(x => albumLow.Contains(x));
                bestAlbum.Name = _actualArtist.Value
                  .OrderByDescending(x => Levenshtein.Similarity(x.Name, name)).First().Name;
              }
              else
              {
                Logger.Logger.Instance.Log(Logger.MessageType.Warning,
                  $"Unable to find album info {album.Name}");

                return null;
              }
            }

            query = new QueryParameters<Release>()
            {
              {"arid", artistMbid},
              {"release", bestAlbum.Name},
              {"type", "album"},
              {"status", "official"}
            };

            releases = await client.Releases.SearchAsync(query);

            release = releases.Items.Where(r => r.Date != null && IsCompactDisc(r)).OrderBy(r => r.Date)
              .FirstOrDefault();

            if (release == null)
            {
              Logger.Logger.Instance.Log(Logger.MessageType.Warning,
                $"Album info was not downloaded {album.Name} - {artistName}");
              return null;
            }
          }
          else
          {
            Logger.Logger.Instance.Log(Logger.MessageType.Warning,
              $"Album info was not downloaded {album.Name} - {artistName}");
            return null;
          }
        }

        string albumCoverUrl = await GetAlbumFrontConverURL(release.Id);

        if (albumCoverUrl == null)
        {
          int releaseIndex = 0;

          while (albumCoverUrl == null)
          {
            if (releaseIndex < releases.Items.Count - 1)
              albumCoverUrl = await GetAlbumFrontConverURL(releases.Items[releaseIndex].Id);
            else
              break;

            releaseIndex++;
          }

          if (albumCoverUrl == null)
          {
            album = new Album()
            {
              Id = album.Id,
              Artist = album.Artist,
              Name = release.Title,
              MusicBrainzId = release.Id,
              ReleaseDate = release.Date,
              Songs = new List<Song>()
            };

            Logger.Logger.Instance.Log(Logger.MessageType.Warning,
              $"Album info was download without cover {album.Name} - {artistName}");

            return album;
          }
        }

        byte[] albumCoverBlob = await GetAlbumFrontConverBLOB(release.Id, albumCoverUrl);

        var coverPath = SaveAlbumCover(album, albumCoverBlob);

        albumName = release.Title;

        // The album known sometimes as The Black Album is already in this database,
        // it is correctly named simply Metallica. This is because The Black Album is only a fan-composed name
        if (album.Name.ToLower().Contains("black album") && album.Artist.Name.ToLower() == "metallica")
          albumName = "Metallica (The Black Album)";

        if (bestAlbum != null)
        {
          albumName = bestAlbum.Name;
        }

        album = new Album()
        {
          Id = album.Id,
          Artist = album.Artist,
          Name = albumName,
          MusicBrainzId = release.Id,
          ReleaseDate = release.Date,
          AlbumFrontCoverURI = albumCoverUrl,
          AlbumFrontCoverFilePath = coverPath,
        };

        Logger.Logger.Instance.Log(Logger.MessageType.Success, $"Album info was succesfully downloaded {album.Name} - {artistName}");

        return album;
      }
      catch (Exception ex)
      {
        if (ex.Message == exceedMsg)
        {
          Logger.Logger.Instance.Log(Logger.MessageType.Warning, "Requests are exceeding, trying again");

          musibrainzAPISempathore.Release();

          Album newAlbum = null;

          await Task.Run(async () => { newAlbum = await UpdateAlbum(album); });

          apiExeed = true;
          return newAlbum;
        }
        else
        {
          Logger.Logger.Instance.Log(Logger.MessageType.Error, ex.Message);
          return null;
        }
      }
      finally
      {
        //API rate limit
        Thread.Sleep(1000);

        if (!apiExeed)
          musibrainzAPISempathore.Release();
      }
    }

    public static string GetDefaultPicturesPath()
    {
      var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VPlayer/Pictures");
      if (!Directory.Exists(directory))
      {
        Directory.CreateDirectory(directory);
      }

      return directory;
    }

    private string SaveAlbumCover(Album album, byte[] blob)
    {
      MemoryStream ms = new MemoryStream(blob);
      Image i = Image.FromStream(ms);

      var directory = Path.Combine(GetDefaultPicturesPath(), $"Albums/{album.Id}");
      var finalPath = Path.Combine(directory, "frontConver.jpg");


      if (!Directory.Exists(directory))
      {
        Directory.CreateDirectory(directory);
      }

      i.Save(finalPath, ImageFormat.Jpeg);

      return finalPath;
    }

    public async Task<DomainClasses.Artist> UpdateArtist(string artistName)
    {
      try
      {
        await musibrainzAPISempathore.WaitAsync();

        // Make sure that TLS 1.2 is available.
        ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

        // Get path for local file cache.
        var location = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        var client = new MusicBrainzClient()
        {

        };

        //API rate limit
        Thread.Sleep(1000);

        var artists = await client.Artists.SearchAsync(artistName.Quote());

        Artist artist = null;

        if (artists.Items.Any(x => x.Score == 100))
          artist = artists.Items.OrderByDescending(a => a.Score).First();
        else if (artists.Items.Count > 0)
          artist = artists.Items.OrderByDescending(a => Levenshtein.Similarity(a.Name, artistName)).First();
        else
        {
          Logger.Logger.Instance.Log(Logger.MessageType.Warning, $"Artist's data was not find {artistName}");
          return null;
        }

        Logger.Logger.Instance.Log(Logger.MessageType.Success, $"Artist's data was updated {artistName}");
        //artist = await Artist.GetAsync(artist.Id, "artist-rels", "url-rels");

        return new DomainClasses.Artist()
        {
          Name = artist.Name,
          MusicBrainzId = artist.Id,
        };
      }
      catch (Exception ex)
      {
        Logger.Logger.Instance.Log(Logger.MessageType.Error, ex.Message);
        return null;
      }
      finally
      {
        musibrainzAPISempathore.Release();
      }
    }

    /// <summary>
    /// Returns byte[] for album front image
    /// </summary>
    /// <param name="MBID"></param>
    /// <returns></returns>,
    private static async Task<byte[]> GetAlbumFrontConverBLOB(string MBID, string url = null)
    {
      return await Task.Run(async () =>
      {
        try
        {
          var webClient = new WebClient();
          if (url == null)
          {
            string apiURL = "http://coverartarchive.org//release/" + $"{MBID}";
            HttpResponse<string> response = await Task.Run(() => Unirest.get(apiURL).asJson<string>());

            if (response.Code != 200)
            {
              if (response.Code == 502)
              {
                Logger.Logger.Instance.Log(Logger.MessageType.Warning,
                  $"Album cover was not found {MBID} trying again, response code {response.Code}");
                return await GetAlbumFrontConverBLOB(MBID, url);
              }
              else
              {
                Logger.Logger.Instance.Log(Logger.MessageType.Warning,
                  $"Album cover was not found {MBID} resposne code {response.Code}");
                return null;
              }
            }

            dynamic stuff = JObject.Parse(response.Body);

            var converUrl = stuff.images[0].thumbnails.small;
            if (converUrl == null)
              converUrl = stuff.images[0].image;

            return (byte[])webClient.DownloadData(converUrl.ToString());
          }
          else
          {
            try
            {
              return await webClient.DownloadDataTaskAsync(url);
            }
            catch (Exception ex)
            {
              return await GetAlbumFrontConverBLOB(MBID, url);
            }
          }
        }
        catch (Exception ex)
        {
          Logger.Logger.Instance.Log(Logger.MessageType.Error, ex.Message);
          return null;
        }
      });
    }

    /// <summary>
    /// Returns URL for album front image
    /// </summary>
    /// <param name="MBID"></param>
    /// <returns></returns>,
    private static async Task<string> GetAlbumFrontConverURL(string MBID)
    {
      return await Task.Run(async () =>
      {
        try
        {
          string apiURL = "http://coverartarchive.org//release/" + $"{MBID}";
          HttpResponse<string> response = await Task.Run(() => Unirest.get(apiURL).asJson<string>());

          if (response.Code != 200)
          {
            if (response.Code == 502)
            {
              Logger.Logger.Instance.Log(Logger.MessageType.Warning,
                $"Album cover was not found {MBID} trying again, response code {response.Code}");
              return await GetAlbumFrontConverURL(MBID);
            }

            Logger.Logger.Instance.Log(Logger.MessageType.Warning,
              $"Album cover was not found {MBID} resposne code {response.Code}");
            return null;
          }

          dynamic stuff = JObject.Parse(response.Body);

          var converUrl = stuff.images[0].image;

          if (converUrl == null)
            converUrl = stuff.images[0].thumbnails.small;

          return (string)converUrl.ToString();
        }
        catch (Exception ex)
        {
          Logger.Logger.Instance.Log(Logger.MessageType.Error, ex.Message);
          return null;
        }
      });
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
    /// Returns true if album is official
    /// </summary>
    /// <param name="g"></param>
    /// <returns></returns>
    private static bool IsOffical(ReleaseGroup g)
    {
      try
      {
        if (g.FirstReleaseDate == "")
          return false;
        if (g.PrimaryType != null)
        {
          return g.PrimaryType.Equals("album", StringComparison.OrdinalIgnoreCase)
                 && g.SecondaryTypes.Count == 0
                 && !string.IsNullOrEmpty(g.FirstReleaseDate);
        }

        return false;
      }
      catch (Exception ex)
      {
        Logger.Logger.Instance.Log(Logger.MessageType.Error, ex.Message);
        return false;
      }
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

    #endregion MusicBrainz Lookups

    #region FingerPrint methods

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
        FileInfo[] Files = supportedItems.SelectMany(ext => d.GetFiles(ext)).ToArray(); //Getting Text files

        foreach (FileInfo file in Files)
        {
          audioInfos.Add(await GetAudioInfoByFingerPrint(file.FullName));
        }
      }
      else
      {
        foreach (var directory in await GetSubDirectories(path))
        {
          audioInfos.AddRange(await GetAudioInfosByFingerprintDirectoryAsync(directory));
        }
      }

      return audioInfos;
    }

    /// <summary>
    /// Gets audio with similar duration as file
    /// </summary>
    /// <param name="matchings"></param>
    /// <param name="fileDuration"></param>
    /// <returns></returns>
    private static JToken GetBestDurationMatch(JArray matchings, string fileDuration)
    {
      try
      {
        if (matchings != null)
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
        }

        return null;
      }
      catch (Exception ex)
      {
        Logger.Logger.Instance.Log(Logger.MessageType.Error, ex.Message);
        return null;
      }
    }

    /// <summary>
    /// Gets audio with similar duration as file
    /// </summary>
    /// <param name="matchings"></param>
    /// <param name="fileDuration"></param>
    /// <returns></returns>
    private static JToken GetBestDurationMatch(JArray matchings, AudioInfo audioInfo)
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
    /// Return audio info by fingerPrint
    /// <param name="path"></param>
    /// </summary>
    private Task<AudioInfo> GetAudioInfoByFingerPrint(string path, AudioInfo pAudioInfo = null)
    {
      return Task.Run<AudioInfo>(async () =>
      {
        try
        {
          var audioInfo = await RunFpcalc(path);

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
              Logger.Logger.Instance.Log(Logger.MessageType.Error,
                $"Response returns {response.Code} {path}");
              throw new Exception($"Response returns {response.Code}");
            }

            dynamic jObject = JObject.Parse(response.Body);
            if (jObject.results.Count != 0)
            {
              var recordings = jObject.results[0].recordings;

              //Sometimes returns more then 1 recording
              dynamic bestRecording = null;
              var bestRecordingByDuration =
                GetBestDurationMatch(recordings, audioInfo.Duration.ToString());

              if (pAudioInfo != null)
              {
                bestRecording = GetBestDurationMatch(recordings, pAudioInfo);
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
                  DiskLocation = path,
                  Duration = audioInfo.Duration
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

              return newAudioInfo;
            }
          }
          else
          {
            Logger.Logger.Instance.Log(Logger.MessageType.Warning, $"Song was not identified {path}");
            return null;
          }
        }
        catch (Exception ex)
        {
          Logger.Logger.Instance.Log(Logger.MessageType.Error, ex.Message);
        }

        return null;
      });
    }

    /// <summary>
    /// Run fpcal process for getting audio fingerprint
    /// </summary>
    /// <param name="trackPath"></param>
    /// <returns>Fingerprint of file</returns>
    private async Task<AudioInfo> RunFpcalc(string trackPath)
    {
      return await Task.Run(() =>
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
          Logger.Logger.Instance.Log(Logger.MessageType.Error, err);
          return null;
        }

        process.WaitForExit();

        var outputs = output.Split('\n');

        return new AudioInfo()
        {
          FingerPrint = outputs[1].Replace("FINGERPRINT=", ""),
          Duration = Convert.ToInt32(outputs[0].Replace("\r", "").Replace("DURATION=", ""))
        };
      });
    }

    #endregion FingerPrint methods

    #region Windows info methods

    private object mediaInfoBatton = new object();

    private void GetMediaInfoo()
    {
      lock (mediaInfoBatton)
      {
        byte[] b = new byte[128];
        string sTitle;
        string sSinger;
        string sAlbum;
        string sYear;
        string sComm;

        FileStream fs = new FileStream("D:\\Downloads\\Creedence Clearwater Revival - Chronicle Vol. 1 & 2 [FLAC] vtwin88cube\\Creedence Clearwater Revival - Chronicle The 20 Greatest Hits (1991) [FLAC]\\01.-Susie Q.flac", FileMode.Open);
        fs.Seek(-128, SeekOrigin.End);
        fs.Read(b, 0, 128);
        bool isSet = false;
        String sFlag = System.Text.Encoding.Default.GetString(b, 0, 3);
        if (sFlag.CompareTo("TAG") == 0)
        {
          System.Console.WriteLine("Tag   is   setted! ");
          isSet = true;
        }

        if (isSet)
        {
          //get   title   of   song;
          sTitle = System.Text.Encoding.Default.GetString(b, 3, 30);
          System.Console.WriteLine("Title: " + sTitle);
          //get   singer;
          sSinger = System.Text.Encoding.Default.GetString(b, 33, 30);
          System.Console.WriteLine("Singer: " + sSinger);
          //get   album;
          sAlbum = System.Text.Encoding.Default.GetString(b, 63, 30);
          System.Console.WriteLine("Album: " + sAlbum);
          //get   Year   of   publish;
          sYear = System.Text.Encoding.Default.GetString(b, 93, 4);
          System.Console.WriteLine("Year: " + sYear);
          //get   Comment;
          sComm = System.Text.Encoding.Default.GetString(b, 97, 30);
          System.Console.WriteLine("Comment: " + sComm);
        }

        System.Console.WriteLine("Any   key   to   exit! ");
        System.Console.Read();
      }
    }

    #region GetAudioInfoByWindowsAsync

    /// <summary>
    /// Returns audio info from file info
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    private AudioInfo GetAudioInfoByWindowsAsync(string path)
    {
      var musicProp = GetAudioWindowsPropertiesAsync(path);

      if (musicProp != null)
      {
        var newAudioInfo = new AudioInfo()
        {
          Album = musicProp.Album,
          Artist = musicProp.Artist != "" ? musicProp.Artist : musicProp?.AlbumArtist,
          Title = musicProp.Title,
          DiskLocation = path,
          Duration = (int)musicProp.Duration.TotalSeconds
        };

        return newAudioInfo;
      }


      return null;
    }

    #endregion GetAudioInfoByWindowsAsync

    #region GetAudioWindowsPropertiesAsync

    /// <summary>
    /// Getting song info from windows 10
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    private MusicProperties GetAudioWindowsPropertiesAsync(string path)
    {
      try
      {
        StorageFile file = AsyncHelpers.RunSync(() => StorageFile.GetFileFromPathAsync(path).AsTask());
        MusicProperties musicProperties = AsyncHelpers.RunSync(() => file.Properties.GetMusicPropertiesAsync().AsTask());

        return musicProperties;
      }
      catch (Exception ex)
      {
        Logger.Logger.Instance.Log(Logger.MessageType.Error, ex.Message);
        return null;
      }
    }

    #endregion GetAudioWindowsPropertiesAsync

    #endregion Windows info methods

    #region Directory methods

    private async Task<List<string>> GetSubDirectories(string rootDirectory)
    {
      return await Task.Run(() =>
      {
        List<string> directories = new List<string>();
        string[] subdirectoryEntries = Directory.GetDirectories(rootDirectory);

        foreach (string subdirectory in subdirectoryEntries)
        {
          LoadSubDirs(subdirectory, ref directories);
        }

        return directories;
      });
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

    #endregion Directory methods

    #region Methods

    public void Initialize()
    {

    }

    protected virtual void OnCoversDownloaded(List<AlbumCover> e)
    {
      CoversDownloaded?.Invoke(this, e);
    }

    protected virtual void OnSubdirectoryLoaded(List<AudioInfo> e)
    {
      SubdirectoryLoaded?.Invoke(this, e);
    }


    #endregion Methods

    public async void UpdateSongLyrics(string artistName, string songName, Song song)
    {
      var url = $"http://api.chartlyrics.com/apiv1.asmx/SearchLyric?artist={artistName}&song={songName}";

      var client = new HttpClient();
      var resutl = await client.GetStringAsync(url);

      var xmlNode = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n\n";

      var validResult = resutl.Substring(xmlNode.Length - 1, resutl.Length - xmlNode.Length) + ">";

      XmlDocument doc = new XmlDocument();
      doc.LoadXml(validResult);

      ArrayOfSearchLyricResult obj;

      using (TextReader textReader = new StringReader(doc.OuterXml))
      {
        using (XmlTextReader reader = new XmlTextReader(textReader))
        {
          XmlSerializer serializer = new XmlSerializer(typeof(ArrayOfSearchLyricResult));
          obj = (ArrayOfSearchLyricResult)serializer.Deserialize(reader);
        }
      }


      var searchLyricResult = obj.SearchLyricResult.Where(x => !string.IsNullOrEmpty(x.Artist) && !string.IsNullOrEmpty(x.Song))
        .OrderBy(x => x.Artist.LevenshteinDistance(artistName) + x.Song.LevenshteinDistance(songName));

      var bestResult = searchLyricResult.FirstOrDefault();

      if (bestResult != null && bestResult.Artist.Similarity(artistName, true) > 0.9 && bestResult.Song.Similarity(songName, true) > 0.9)
      {

        var lyricsUrl = $"http://api.chartlyrics.com/apiv1.asmx/GetLyric?lyricId={bestResult.LyricId}&lyricCheckSum={bestResult.LyricChecksum}";

        var lyrics = await GetAttributes(lyricsUrl);

        if (!string.IsNullOrEmpty(lyrics))
        {
          song.Chartlyrics_Lyric = lyrics;
          song.Chartlyrics_LyricCheckSum = bestResult.LyricChecksum;
          song.Chartlyrics_LyricId = bestResult.LyricId;

          ItemUpdated.OnNext(song);
        }
      }
    }

    private async Task<string> GetAttributes(string url)
    {
      var client = new HttpClient();
      var resutl = await client.GetStringAsync(url);

      var xmlNode = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n\n";

      var validResult = resutl.Substring(xmlNode.Length - 1, resutl.Length - xmlNode.Length) + ">";

      XmlDocument doc = new XmlDocument();
      doc.LoadXml(validResult);

      GetLyricResult obj;

      using (TextReader textReader = new StringReader(doc.OuterXml))
      {
        using (XmlTextReader reader = new XmlTextReader(textReader))
        {
          XmlSerializer serializer = new XmlSerializer(typeof(GetLyricResult));
          obj = (GetLyricResult)serializer.Deserialize(reader);
        }
      }

      return obj.Lyric;
    }
  }
}
