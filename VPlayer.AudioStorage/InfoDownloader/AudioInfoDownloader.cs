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
using System.Globalization;
using System.Text.RegularExpressions;
using Logger;
using VCore.Helpers;
using VPlayer.AudioStorage.InfoDownloader.Clients.Chartlyrics;
using VPlayer.AudioStorage.InfoDownloader.LRC;
using VPlayer.AudioStorage.InfoDownloader.LRC.Domain;
using VCore;
using VCore.Standard;
using VCore.WPF.Controls.StatusMessage;
using VPlayer.Core.Managers.Status;

namespace VPlayer.AudioStorage.InfoDownloader
{
  /// <summary>
  /// Class for getting info about audio file (music)
  /// </summary>
  public class AudioInfoDownloader : IInitializable
  {
    private readonly ILogger logger;
    private readonly IStatusManager statusManager;

    #region Fields

    private static SemaphoreSlim musibrainzAPISempathore = new SemaphoreSlim(1, 1);
    private string api = "eJSq8XSeYR";

    private string exceedMsg = "Your requests are exceeding the " +
                               "allowable rate limit. " +
                               "Please see http://wiki.musicbrainz.org/XMLWebService " +
                               "for more information.";

    private string meta = "recordings+releases+tracks";
    private string[] supportedItems = new string[] { "*.mp3", "*.flac", ".mp4a", "*.ogg", "*.wav" };

    #endregion Fields

    #region Constructors

    public AudioInfoDownloader(ILogger logger, IStatusManager statusManager)
    {
      this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
      this.statusManager = statusManager ?? throw new ArgumentNullException(nameof(statusManager));
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

    public Task UpdateItem(dynamic item)
    {
      return UpdateItem(item);
    }

    private Task UpdateItem(DomainClasses.Artist artist)
    {
      return Task.Run(async () =>
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
          logger.Log(Logger.MessageType.Error, ex.Message);
        }
      });
    }

    private Task UpdateItem(Album album)
    {
      return Task.Run(async () =>
      {
        try
        {
          var updateAlbum = await UpdateAlbum(album);

          if (updateAlbum != null)
          {
            updateAlbum.Id = album.Id;
            updateAlbum.InfoDownloadStatus = InfoDownloadStatus.Downloaded;
            album.InfoDownloadStatus = InfoDownloadStatus.Downloaded;

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
          logger.Log(Logger.MessageType.Error, ex.Message);
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

      if (audioInfo == null ||
          (audioInfo.Artist == null &&
           audioInfo.Album == null &&
           audioInfo.Artist == "" &&
           audioInfo.Album == ""))
      {
        var fingerPrintAudioInfo = GetAudioInfoByFingerPrint(path);

        if (fingerPrintAudioInfo != null)
        {
          logger.Log(Logger.MessageType.Success, $"Audio info was gotten by fingerprint {path}");
          return fingerPrintAudioInfo;
        }
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
        logger.Log(Logger.MessageType.Error, ex.Message);
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
              logger.Log(Logger.MessageType.Warning,
                $"Album cover was not found {MBID} trying again, response code {response.Code}");
              return await GetAlbumFrontCoversUrls(MBID, cancellationToken);
            }

            logger.Log(Logger.MessageType.Warning,
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
            logger.Log(Logger.MessageType.Warning,
              $"Album cover was not found {MBID}");
            return null;
          }

          OnCoversDownloaded(covers);
          return covers;
        }
        catch (Exception ex)
        {
          logger.Log(ex);
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

          if (newArtist == null)
          {
            return null;
          }

          if (album.Artist.MusicBrainzId == null)
          {
            newArtist = await UpdateArtist(album.Artist.Name);

            if (newArtist == null)
            {
              logger.Log(Logger.MessageType.Warning,
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

          logger.Log(Logger.MessageType.Success, "Album covers was found");
          return covers;
        }
        catch (Exception ex)
        {
          logger.Log(Logger.MessageType.Error, ex.Message);
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
            logger.Log(Logger.MessageType.Error, ex.Message);
            return null;
          }
        }
      });
    }

    #region UpdateAlbum

    public async Task<Album> UpdateAlbum(Album album)
    {
      try
      {
        var albumName = album.Name;

        var statusMessage = new StatusMessageViewModel(4)
        {
          Message = $"Getting album info ({album.Name})",
          Status = StatusType.Processing
        };

        statusManager.UpdateMessage(statusMessage);

        // The album known sometimes as The Black Album is already in this database,
        // it is correctly named simply Metallica. This is because The Black Album is only a fan-composed name
        if (albumName.ToLower().Contains("black album") && album.Artist.Name.ToLower() == "metallica")
        {
          albumName = "Metallica";
        }

        await musibrainzAPISempathore.WaitAsync();


        album.InfoDownloadStatus = InfoDownloadStatus.Downloading;
        ItemUpdated.OnNext(album);
        logger.Log(Logger.MessageType.Inform, $"Downloading album info {album.Name}");

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
            logger.Log(Logger.MessageType.Warning,
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
          logger.Log(Logger.MessageType.Warning, "Artist and album is null or empty");
          return null;
        }

        if (!string.IsNullOrEmpty(artistMbid))
          query.Add("arid", artistMbid);

        if (!string.IsNullOrEmpty(albumName))
          query.Add("release", albumName);

        query.Add("type", "album");
        query.Add("status", "official");

        // Search for a release by title.

        // var textWriter = Console.Out;
        // Console.SetOut(TextWriter.Null);

        var releases = await client.Releases.SearchAsync(query);

        // Console.SetOut(textWriter);

        if (releases.Count != 0)
        {
          // Get the oldest release (remember to sort out items with no date set).
          release = releases.Items.Where(r => r.Date != null && IsCompactDisc(r)).OrderBy(r => r.Date)
            .FirstOrDefault();

          if (release == null)
          {
            logger.Log(Logger.MessageType.Warning,
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
              .OrderByDescending(x => x.Name.Similarity(album.Name)).First();

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
                  .OrderByDescending(x => x.Name.Similarity(name)).First().Name;
              }
              else if (albumsLow.Any(x => albumLow.Contains(x)))
              {
                var name = albumsLow.First(x => albumLow.Contains(x));
                bestAlbum.Name = _actualArtist.Value
                  .OrderByDescending(x => x.Name.Similarity(name)).First().Name;
              }
              else
              {
                logger.Log(Logger.MessageType.Warning,
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
              logger.Log(Logger.MessageType.Warning,
                $"Album info was not downloaded {album.Name} - {artistName}");
              return null;
            }
          }
          else
          {
            logger.Log(Logger.MessageType.Warning,
              $"Album info was not downloaded {album.Name} - {artistName}");
            return null;
          }
        }

        statusMessage.Message = $"Finding cover for ({release.Title})";

        statusManager.UpdateMessageAndIncreaseProcessCount(statusMessage);

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

            logger.Log(Logger.MessageType.Warning,
              $"Album info was download without cover {album.Name} - {artistName}");

            statusMessage.Message = $"Cover not found";
            statusMessage.Status = StatusType.Done;

            statusManager.UpdateMessageAndIncreaseProcessCount(statusMessage);

            return album;
          }
        }

        statusMessage.Message = $"Downloading cover ({release.Title})";
        statusManager.UpdateMessageAndIncreaseProcessCount(statusMessage);

        var coverPath = GetAlbumCoverImagePath(album);

        if (!File.Exists(coverPath))
        {
          byte[] albumCoverBlob = await GetAlbumFrontConverBLOB(release.Id, albumCoverUrl);

          coverPath = SaveAlbumCover(album, albumCoverBlob);

          logger.Log(Logger.MessageType.Success, $"Album info was succesfully downloaded {album.Name} - {artistName}");


          statusMessage.Message = $"Album COVER successfuly DOWNLOADED";
          statusManager.UpdateMessageAndIncreaseProcessCount(statusMessage);
        }
        else
        {
          statusMessage.Message = $"Album COVER was retrevied from CACHE";
          statusManager.UpdateMessageAndIncreaseProcessCount(statusMessage);
        }

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
          NormalizedName = album.NormalizedName
        };

        statusMessage.Message = $"Album successfuly updated";
        statusManager.UpdateMessageAndIncreaseProcessCount(statusMessage);

        return album;
      }
      catch (Exception ex)
      {
        if (ex.Message == exceedMsg)
        {
          logger.Log(Logger.MessageType.Warning, "Requests are exceeding, trying again");

          musibrainzAPISempathore.Release();

          Album newAlbum = null;

          await Task.Run(async () => { newAlbum = await UpdateAlbum(album); });

          apiExeed = true;
          return newAlbum;
        }
        else
        {
          logger.Log(Logger.MessageType.Error, ex.Message);
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

    #endregion

    #region GetDefaultPicturesPath

    public static string GetDefaultPicturesPath()
    {
      var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VPlayer/Pictures");
      if (!Directory.Exists(directory))
      {
        Directory.CreateDirectory(directory);
      }

      return directory;
    }

    #endregion

    #region GetAlbumCoverImagePath

    public static string GetAlbumCoverImagePath(Album album)
    {
      string path = null;

      if (!string.IsNullOrEmpty(album.Artist.Name))
      {
        path = $"Albums\\{album.Artist.Name}\\{album.Name}\\{album.Id}";
      }
      else
      {
        path = $"Albums\\{album.Name}\\{album.Id}";
      }

      var directory = Path.Combine(GetDefaultPicturesPath(), path);

      return Path.Combine(directory, $"frontConver.jpg");
    }

    #endregion

    #region SaveAlbumCover

    private string SaveAlbumCover(Album album, byte[] blob)
    {
      MemoryStream ms = new MemoryStream(blob);
      Image i = Image.FromStream(ms);


      var finalPath = GetAlbumCoverImagePath(album);

      finalPath.EnsureDirectoryExists();

      if (File.Exists(finalPath))
      {
        File.Delete(finalPath);
      }

      i.Save(finalPath, ImageFormat.Jpeg);

      return finalPath;
    }

    #endregion

    #region UpdateArtist

    public async Task<DomainClasses.Artist> UpdateArtist(string artistName)
    {
      try
      {
        var statusMessage = new StatusMessageViewModel(2)
        {
          Message = $"Waiting for Musibrainz API ({artistName})",
          Status = StatusType.Starting
        };

        statusManager.UpdateMessage(statusMessage);

        await musibrainzAPISempathore.WaitAsync();

        // Make sure that TLS 1.2 is available.
        ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;


        var client = new MusicBrainzClient();

        statusMessage.Status = StatusType.Processing;
        statusMessage.Message = $"Searching... ({artistName})";
        statusManager.UpdateMessageAndIncreaseProcessCount(statusMessage);

        //API rate limit
        Thread.Sleep(1000);

        var artists = await client.Artists.SearchAsync(artistName.Quote());

        Artist artist = null;

        if (artists.Items.Any(x => x.Score == 100))
          artist = artists.Items.OrderByDescending(a => a.Score).First();
        else if (artists.Items.Count > 0)
          artist = artists.Items.OrderByDescending(a => a.Name.Similarity(artistName)).First();
        else
        {
          logger.Log(Logger.MessageType.Warning, $"Artist's data was not find {artistName}");
          return null;
        }

        logger.Log(Logger.MessageType.Success, $"Artist's data was updated {artistName}");
        //artist = await Artist.GetAsync(artist.Id, "artist-rels", "url-rels");

        if (artist != null)
        {
          statusMessage.Message = $"Found ({artist?.Name})";
          statusManager.UpdateMessageAndIncreaseProcessCount(statusMessage);

          return new DomainClasses.Artist()
          {
            Name = artist.Name,
            MusicBrainzId = artist.Id,
          };
        }
        else
        {
          statusMessage.Status = StatusType.Failed;
          statusMessage.Message = $"NOT FOUND ({artist?.Name})";
          statusManager.UpdateMessageAndIncreaseProcessCount(statusMessage);

          return null;
        }

      }
      catch (Exception ex)
      {
        logger.Log(Logger.MessageType.Error, ex.Message);
        return null;
      }
      finally
      {
        musibrainzAPISempathore.Release();
      }
    }

    #endregion

    #region GetAlbumFrontConverBLOB

    /// <summary>
    /// Returns byte[] for album front image
    /// </summary>
    /// <param name="MBID"></param>
    /// <returns></returns>,
    private async Task<byte[]> GetAlbumFrontConverBLOB(string MBID, string url = null)
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
                logger.Log(Logger.MessageType.Warning, $"Album cover was not found {MBID} trying again, response code {response.Code}");
                return await GetAlbumFrontConverBLOB(MBID, url);
              }
              else
              {
                logger.Log(Logger.MessageType.Warning,
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
          logger.Log(Logger.MessageType.Error, ex.Message);
          return null;
        }
      });
    }

    #endregion

    #region MyRegion

    /// <summary>
    /// Returns URL for album front image
    /// </summary>
    /// <param name="MBID"></param>
    /// <returns></returns>,
    private async Task<string> GetAlbumFrontConverURL(string MBID)
    {
      return await Task.Run(async () =>
      {
        try
        {
          string apiURL = "http://coverartarchive.org//release/" + $"{MBID}";

          HttpResponse<string> response = await Task.Run(() =>
          {
            try
            {
              return Unirest.get(apiURL).asJson<string>();
            }
            catch (Exception ex)
            {
              return null;
            }
          });

          if (response == null)
          {
            return null;
          }

          if (response.Code != 200)
          {
            if (response.Code == 502)
            {
              logger.Log(Logger.MessageType.Warning,
                $"Album cover was not found {MBID} trying again, response code {response.Code}");
              return await GetAlbumFrontConverURL(MBID);
            }

            logger.Log(Logger.MessageType.Warning,
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
          logger.Log(Logger.MessageType.Error, ex.Message);
          return null;
        }
      });
    }

    #endregion
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

      return r.Media[0].Format == "CD" || r.Media[0].Format == "Digital Media";
    }

    /// <summary>
    /// Returns true if album is official
    /// </summary>
    /// <param name="g"></param>
    /// <returns></returns>
    private bool IsOffical(ReleaseGroup g)
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
        logger.Log(Logger.MessageType.Error, ex.Message);
        return false;
      }
    }

    #endregion 

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
          audioInfos.Add(GetAudioInfoByFingerPrint(file.FullName));
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
    private JToken GetBestDurationMatch(JArray matchings, string fileDuration)
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

            return durations.OrderByDescending(a => a.Value.Similarity(fileDuration)).First().Key;
          }
        }

        return null;
      }
      catch (Exception ex)
      {
        logger.Log(Logger.MessageType.Error, ex.Message);
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

            return candidates.OrderByDescending(a => a.Value.Similarity(audioInfo.Artist + audioInfo.Album))
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

            return candidates.OrderByDescending(a => a.Value.Similarity(audioInfo.Artist))
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

            return candidates.OrderByDescending(a => a.Value.Similarity(audioInfo.Album))
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
    public AudioInfo GetAudioInfoByFingerPrint(string path, AudioInfo pAudioInfo = null)
    {
      try
      {
        var audioInfo = RunFpcalc(path);

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
            logger.Log(Logger.MessageType.Error,
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
          logger.Log(Logger.MessageType.Warning, $"Song was not identified {path}");
          return null;
        }
      }
      catch (Exception ex)
      {
        logger.Log(Logger.MessageType.Error, ex.Message);
      }

      return null;
    }

    /// <summary>
    /// Run fpcal process for getting audio fingerprint
    /// </summary>
    /// <param name="trackPath"></param>
    /// <returns>Fingerprint of file</returns>
    private AudioInfo RunFpcalc(string trackPath)
    {
      string fpacl = Environment.CurrentDirectory + "\\ChromaPrint\\fpcalc.exe";

      Process process = new Process();

      process.StartInfo.FileName = fpacl;
      process.StartInfo.Arguments = $"\"{trackPath}\"";
      process.StartInfo.UseShellExecute = false;
      process.StartInfo.RedirectStandardOutput = true;
      process.StartInfo.RedirectStandardError = true;
      process.StartInfo.CreateNoWindow = true;

      process.Start();
      //* Read the output (or the error)
      string output = process.StandardOutput.ReadToEnd();
      string err = process.StandardError.ReadToEnd();

      if (err != "")
      {
        logger.Log(Logger.MessageType.Error, err);
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

    #endregion FingerPrint methods

    #region Windows info methods

    #region GetAudioInfoByWindowsAsync

    /// <summary>
    /// Returns audio info from file info
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public AudioInfo GetAudioInfoByWindowsAsync(string path)
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
        StorageFile file = AsyncHelpers.RunSync(() =>
        {
          try
          {
            return StorageFile.GetFileFromPathAsync(path).AsTask();
          }
          catch (Exception ex)
          {
            logger.Log(Logger.MessageType.Error, ex.Message);
            return null;
          }
        });

        MusicProperties musicProperties = AsyncHelpers.RunSync(() =>
        {
          try
          {
            return file.Properties.GetMusicPropertiesAsync().AsTask();
          }
          catch (Exception ex)
          {
            logger.Log(Logger.MessageType.Error, ex.Message);
            return null;
          }
        });

        return musicProperties;
      }
      catch (Exception ex)
      {
        logger.Log(Logger.MessageType.Error, ex.Message);
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

    #region GetClearName

    public static string GetClearName(string sourceString)
    {
      if (string.IsNullOrEmpty(sourceString))
      {
        return sourceString;
      }

      var lowerString = sourceString.ToLower();

      var replaced = lowerString.Replace("(retail)", null).Replace("(limited edition)", null).Replace("&", "and");

      replaced = Regex.Replace(replaced, @"\(.*\)", "");
      replaced = Regex.Replace(replaced, @"\[.*\]", "");
      replaced = Regex.Replace(replaced, @"\s+", " ");

      CultureInfo cultureInfo = Thread.CurrentThread.CurrentCulture;
      TextInfo textInfo = cultureInfo.TextInfo;

      return textInfo.ToTitleCase(replaced);
    }

    #endregion


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

    public async Task<bool> UpdateSongLyricsAsync(string artistName, string songName, Song song)
    {
      var chartClient = new ChartLyricsClient(logger);

      var updatedSong = await chartClient.UpdateSongLyrics(artistName, songName, song);

      if (updatedSong != null)
        ItemUpdated.OnNext(updatedSong);

      return updatedSong != null;

    }

    #region TryGetLRCLyricsAsync

    public Task<ILRCFile> TryGetLRCLyricsAsync<TClient>(TClient client, Song song, string artistName, string albumName) where TClient : ILrcProvider
    {
      return Task.Run(async () =>
      {
        var lrcFile = await client.TryGetLrcAsync(song.Name, artistName, albumName);

        if (lrcFile != null)
        {
          lrcFile.Title = song.Name;
          lrcFile.Artist = artistName;
          lrcFile.Album = albumName;

          song.LRCLyrics = (int)client.LRCProvider + ";" + lrcFile.GetString();

          ItemUpdated.OnNext(song);
        }

        return lrcFile;
      });
    }

    #endregion

    #endregion 


  }
}
