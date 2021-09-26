using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using VCore.WPF.Managers;
using VPlayer.AudioStorage.InfoDownloader.LRC;
using VPlayer.AudioStorage.InfoDownloader.LRC.Domain;
using VPLayer.Domain.Contracts.CloudService.Providers;

namespace VPlayer.AudioStorage.InfoDownloader.Clients.PCloud
{
  public class PCloudLRCFile : LRCFile
  {
    public long Id { get; set; }
    public PCloudLRCFile(List<LRCLyricLine> lines) : base(lines)
    {
    }

  }

  public class PCloudLyricsProvider : LrcProvider<PCloudLRCFile>
  {
    private readonly ICloudService cloudService;
    private readonly IWindowManager windowManager;
    private bool wasPCloudInitilized = false;
    private long lyricsFolderId = 1302392188;
    private List<PCloudClient.Domain.FolderInfo> artistsFolders = new List<PCloudClient.Domain.FolderInfo>();

    private string lrcExtension = ".lrc";

    public PCloudLyricsProvider(ICloudService cloudService, IWindowManager windowManager)
    {
      this.cloudService = cloudService ?? throw new ArgumentNullException(nameof(cloudService));
      this.windowManager = windowManager ?? throw new ArgumentNullException(nameof(windowManager));
    }

    private async Task<bool> InitilizePCloud()
    {
      if (!wasPCloudInitilized)
      {
        artistsFolders = (await cloudService.GetFoldersAsync(lyricsFolderId))?.ToList();

        if (artistsFolders != null)
        {
          wasPCloudInitilized = true;
        }
      }

      return wasPCloudInitilized;
    }

    #region UpdateOrCreateFile

    private async Task<bool> UpdateOrCreateFile(string songName, string albumName, string artistName, string content, string fileExtension)
    {
      if (await InitilizePCloud())
      {
        var artist = artistsFolders?.SingleOrDefault(x => GetNormalizedName(x.name) == GetNormalizedName(artistName));

        if (artist != null)
        {
          var albums = (await cloudService.GetFoldersAsync(artist.id)).ToList();

          var album = albums.SingleOrDefault(x => GetNormalizedName(x.name) == GetNormalizedName(albumName));

          if (album != null)
          {
            var songs = (await cloudService.GetFilesAsync(album.id)).ToList();
            var fileName = GetFileName(artistName, songName).ToLower();

            var existingSongs =  songs.Where(x => x.name.Replace(fileExtension, null).ToLower() == fileName).ToList();

            if (existingSongs.Count > 1)
            {
              Application.Current.Dispatcher.Invoke(() =>
              {
                windowManager.ShowErrorPrompt($"Multiple ({existingSongs.Count}) LRC files with same name {album.name}\\{fileName}");
              });
            }
            else
            {
              var existingSong = existingSongs.SingleOrDefault();

              if (existingSong != null)
              {
                return await cloudService.WriteToFile(content, existingSong.id);
              }
              else
              {
                return await cloudService.CreateFileAndWrite(GetFileName(artistName, songName) + fileExtension, content, album.id);
              }
            }
          }
          else
          {
            var newAlbum = await cloudService.CreateFolder(GetPathValidName(albumName), artist.id);

            if (newAlbum != null)
            {
              return await cloudService.CreateFileAndWrite(GetFileName(artistName, songName) + fileExtension, content, newAlbum.id);
            }
          }
        }
        else if (!string.IsNullOrEmpty(artistName))
        {
          var newArtistFolder = await cloudService.CreateFolder(GetPathValidName(artistName), lyricsFolderId);

          artistsFolders?.Add(newArtistFolder);

          if (!string.IsNullOrEmpty(albumName) && newArtistFolder != null)
          {
            var newAlbum = await cloudService.CreateFolder(GetPathValidName(albumName), newArtistFolder.id);

            if (newAlbum != null)
            {
              return await cloudService.CreateFileAndWrite(GetFileName(artistName, songName) + fileExtension, content, newAlbum.id);
            }
          }
        }
      }

      return false;
    }

    #endregion

    #region Update

    private SemaphoreSlim updateSemaphore = new SemaphoreSlim(1, 1);
    public override async Task<bool> Update(ILRCFile lRCFile)
    {
      try
      {
        await updateSemaphore.WaitAsync();

        if (lRCFile == null || string.IsNullOrEmpty(lRCFile.Title))
        {
          return false;
        }

        return await UpdateOrCreateFile(lRCFile.Title, lRCFile.Album, lRCFile.Artist, lRCFile.GetString(), lrcExtension);
      }
      finally
      {
        updateSemaphore.Release();
      }
    }

    #endregion

    #region GetLinesLrcFileAsync

    protected override async Task<KeyValuePair<string[], ILRCFile>> GetLinesLrcFileAsync(string songName, string artistName, string albumName)
    {
      var lyricsFile = await GetFile(songName, artistName, albumName, lrcExtension);

      if (lyricsFile != null)
      {
        var stream = await cloudService.ReadFile(lyricsFile.Id);

        if (stream != null)
        {
          var stringValue = Encoding.UTF8.GetString(stream.ToArray()).Replace("\r\n", "\n").Replace("\r", "");

          var lines = stringValue.Split('\n');

          var lRCFile = new PCloudLRCFile(null)
          {
            Id = lyricsFile.Id
          };

          return new KeyValuePair<string[], ILRCFile>(lines, lRCFile);
        }
      }

      return new KeyValuePair<string[], ILRCFile>(null, null);
    }

    #endregion

    #region GetFile

    protected override async Task<PCloudLRCFile> GetFile(string songName, string artistName, string albumName, string extesion)
    {
      if (await InitilizePCloud())
      {
        var artist = artistsFolders.SingleOrDefault(x => GetNormalizedName(x.name) == GetNormalizedName(artistName));

        if (artist != null)
        {
          var albums = (await cloudService.GetFoldersAsync(artist.id)).ToList();

          var album = albums.SingleOrDefault(x => GetNormalizedName(x.name) == GetNormalizedName(albumName));

          if (album != null)
          {
            var songs = (await cloudService.GetFilesAsync(album.id)).ToList();

            var fileName = (GetFileName(artistName, songName) + extesion).ToLower();

            var existingSongs = songs.Where(x => x.name.ToLower() == fileName).ToList();

            if (existingSongs.Count > 1)
            {
              Application.Current.Dispatcher.Invoke(() =>
              {
                windowManager.ShowErrorPrompt($"Multiple ({existingSongs.Count}) LRC files with same name {album.name}\\{fileName}");
              });
            }
            else if(existingSongs.Count > 0)
            {
              var existingSong = existingSongs.SingleOrDefault();

              if (existingSong != null)
              {
                return new PCloudLRCFile(null)
                {
                  Id = existingSong.id
                };
              }
            }
          }
        }
      }

      return null;
    }

    #endregion

    #region GetNormalizedName

    private string GetNormalizedName(string input)
    {
      if (string.IsNullOrEmpty(input))
      {
        return input;
      }

      Regex rgx = new Regex("[^a-zA-Z0-9]");

      return rgx.Replace(input.ToLower(), "");
    }

    #endregion

    #region GetTextLyrics

    public async Task<string> GetTextLyrics(string songName, string albumName, string artistName)
    {
      if (await InitilizePCloud())
      {
        var file = await GetFile(songName, artistName, albumName, ".txt");

        if (file != null)
        {
          var stream = await cloudService.ReadFile(file.Id);

          if (stream != null)
          {
            var stringValue = Encoding.UTF8.GetString(stream.ToArray()).Replace("\r\n", "\n").Replace("\r", "");

            return stringValue;
          }
        }
      }

      return null;
    }

    #endregion

    #region SaveTextLyrics

    public Task<bool> SaveTextLyrics(string songName, string albumName, string artistName, string lyrics)
    {
      return UpdateOrCreateFile(songName, albumName, artistName, lyrics, ".txt");
    }

    #endregion

    public override LRCProviders LRCProvider { get; } = LRCProviders.PCloud;
  }
}