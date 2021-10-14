using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PCloudClient.Domain;
using VCore.WPF.Managers;
using VPlayer.AudioStorage.InfoDownloader.LRC;
using VPlayer.AudioStorage.InfoDownloader.LRC.Domain;
using VPLayer.Domain.Contracts.CloudService.Providers;

namespace VPlayer.AudioStorage.InfoDownloader.Clients.PCloud
{
  public class PCloudLyricsProvider : LrcProvider<PCloudLRCFile>
  {
    private readonly ICloudService cloudService;
    private readonly IWindowManager windowManager;
    private readonly IPCloudProvider pCloudProvider;

    private long lyricsFolderId = 1302392188;

    private string lrcExtension = ".lrc";

    public PCloudLyricsProvider(ICloudService cloudService, IWindowManager windowManager, IPCloudProvider pCloudProvider)
    {
      this.cloudService = cloudService ?? throw new ArgumentNullException(nameof(cloudService));
      this.windowManager = windowManager ?? throw new ArgumentNullException(nameof(windowManager));
      this.pCloudProvider = pCloudProvider ?? throw new ArgumentNullException(nameof(pCloudProvider));
    }

    public override LRCProviders LRCProvider { get; } = LRCProviders.PCloud;

    #region Methods

    #region UpdateOrCreateFile

    private Task<bool> UpdateOrCreateLyricsFile(string songName, string albumName, string artistName, string content, string fileExtension)
    {
      var folders = new string[] { artistName, albumName };
      var fileName = GetFileName(artistName, songName);

      if (fileName != null)
        return pCloudProvider.UpdateOrCreateFile(lyricsFolderId, folders, fileName, Encoding.UTF8.GetBytes(content), fileExtension);

      return Task.FromResult(false);
    }

    #endregion

    #region GetFile

    protected override async Task<PCloudLRCFile> GetFile(string songName, string artistName, string albumName, string extension)
    {
      var folders = new string[] { artistName, albumName };
      var fileName = GetFileName(artistName, songName);

      if (fileName != null)
      {
        var fileId = (await pCloudProvider.GetFile(lyricsFolderId, folders, fileName, extension))?.id;

        if (fileId != null)
        {
          return new PCloudLRCFile(null)
          {
            Id = fileId.Value
          };
        }
      }

      return null;
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

        return await UpdateOrCreateLyricsFile(lRCFile.Title, lRCFile.Album, lRCFile.Artist, lRCFile.GetString(), lrcExtension);
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

    #region GetTextLyrics

    public async Task<string> GetTextLyrics(string songName, string albumName, string artistName)
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

      return null;
    }

    #endregion

    #region SaveTextLyrics

    public Task<bool> SaveTextLyrics(string songName, string albumName, string artistName, string lyrics)
    {
      return UpdateOrCreateLyricsFile(songName, albumName, artistName, lyrics, ".txt");
    }

    #endregion

    #endregion

  }
}