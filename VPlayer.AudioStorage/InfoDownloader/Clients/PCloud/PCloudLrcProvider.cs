using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using VCore.Standard;
using VCore.WPF.ViewModels.WindowsFiles;
using VPlayer.AudioStorage.InfoDownloader.LRC.Domain;
using VPLayer.Domain.Contracts.CloudService.Providers;

namespace VPlayer.AudioStorage.InfoDownloader.LRC.Clients.Google
{
  public class PCloudLRCFile : LRCFile
  {
    public long Id { get; set; }
    public PCloudLRCFile(List<LRCLyricLine> lines) : base(lines)
    {
    }

  }

  public class PCloudLrcProvider : LrcProvider<PCloudLRCFile>
  {
    private readonly ICloudService cloudService;
    private bool wasPCloudInitilized = false;
    private long lyricsFolderId = 1302392188;
    private List<PCloudClient.Domain.FolderInfo> artistsFolders = new List<PCloudClient.Domain.FolderInfo>();
    private string fileExtension = ".lrc";

    public PCloudLrcProvider(ICloudService cloudService)
    {
      this.cloudService = cloudService ?? throw new ArgumentNullException(nameof(cloudService));
    }

    private async Task<bool> InitilizePCloud()
    {
      if (!wasPCloudInitilized)
      {
        artistsFolders = (await cloudService.GetFoldersAsync(lyricsFolderId)).ToList();

        if (artistsFolders != null)
        {
          wasPCloudInitilized = true;
        }
      }

      return wasPCloudInitilized;
    }

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

        if (await InitilizePCloud())
        {
          var artist = artistsFolders.SingleOrDefault(x => GetNormalizedName(x.name) == GetNormalizedName(lRCFile.Artist));

          if (artist != null)
          {
            var albums = (await cloudService.GetFoldersAsync(artist.id)).ToList();

            var album = albums.SingleOrDefault(x => GetNormalizedName(x.name) == GetNormalizedName(lRCFile.Album));

            if (album != null)
            {
              var songs = (await cloudService.GetFilesAsync(album.id)).ToList();

              var existingSong = songs.SingleOrDefault(x => x.name.Replace(fileExtension, null) == 
                                                            GetFileName(lRCFile.Artist, lRCFile.Title));

              if (existingSong != null)
              {
                return await cloudService.WriteToFile(lRCFile.GetString(), existingSong.id);
              }
              else
              {
                return await cloudService.CreateFileAndWrite(GetFileName(lRCFile.Artist,lRCFile.Title) + fileExtension, lRCFile.GetString(), album.id);
              }
            }
            else
            {
              var newAlbum = await cloudService.CreateFolder(lRCFile.Album, artist.id);

              if (newAlbum != null)
              {
                return await cloudService.CreateFileAndWrite(GetFileName(lRCFile.Artist, lRCFile.Title)  + fileExtension, lRCFile.GetString(), newAlbum.id);
              }
            }
          }
          else if (!string.IsNullOrEmpty(lRCFile.Artist))
          {
            var newArtistFolder = await cloudService.CreateFolder(lRCFile.Artist, lyricsFolderId);

            artistsFolders.Add(newArtistFolder);

            if (!string.IsNullOrEmpty(lRCFile.Album) && newArtistFolder != null)
            {
              var newAlbum = await cloudService.CreateFolder(lRCFile.Album, newArtistFolder.id);

              if (newAlbum != null)
              {
                return await cloudService.CreateFileAndWrite(GetFileName(lRCFile.Artist, lRCFile.Title)  + fileExtension, lRCFile.GetString(), newAlbum.id);
              }
            }
          }
        }

        return false;
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
      var lyricsFile = await GetFile(songName, artistName, albumName);

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

    protected override async Task<PCloudLRCFile> GetFile(string songName, string artistName, string albumName)
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

            var existingSong = songs.SingleOrDefault(x => x.name.Replace(fileExtension, null) == GetFileName(artistName,songName));

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

      return null;
    }

    #endregion

    private string GetNormalizedName(string input)
    {
      Regex rgx = new Regex("[^a-zA-Z0-9]");

      return rgx.Replace(input.ToLower(), "");
    }

    public override LRCProviders LRCProvider { get; } = LRCProviders.PCloud;
  }
}