using System;
using System.IO;
using System.Linq;
using VPlayer.AudioStorage.InfoDownloader.LRC.Domain;

namespace VPlayer.AudioStorage.InfoDownloader.LRC.Clients
{
  public class LocalLrcLyricsProvider : LRCLyricsProvider
  {
    private readonly string basePath;

    public LocalLrcLyricsProvider(string basePath)
    {
      this.basePath = basePath ?? throw new ArgumentNullException(nameof(basePath));
    }

    #region FindLrcFile

    protected override string FindLrcFile(string songName, string artistName, string albumName)
    {
      var directoryPath = basePath;

      if (!string.IsNullOrEmpty(artistName))
      {
        directoryPath = Path.Combine(directoryPath, artistName);
      }

      if (!string.IsNullOrEmpty(albumName))
      {
        directoryPath = Path.Combine(directoryPath, albumName);
      }

      if (!string.IsNullOrEmpty(artistName) && Directory.Exists(directoryPath))
      {
        var directory = new DirectoryInfo(directoryPath);

        var filesInDir = directory.GetFiles($"*{GetFileName(artistName, songName)}*.lrc").ToList();

        if (filesInDir.Count == 1)
        {
          return filesInDir?.FirstOrDefault()?.FullName;
        }
        else if (filesInDir.Count == 0)
        {
          return null;
        }
        else
          throw new Exception("More files were found '" + songName + "' in '" + directory.FullName + "'");
      }

      return null;
    }



    #endregion

    #region LrcFileExists

    protected override bool LrcFileExists(string lrcFile)
    {
      return File.Exists(lrcFile);
    }

    #endregion

    #region TryGetLrc

    public override LRCFile TryGetLrc(string songName, string artistName, string albumName)
    {
      var lrcFilePath = FindLrcFile(songName, artistName, albumName);

      LRCFile lRCFile = null;

      if (lrcFilePath != null)
      {
        lRCFile = ParseLRCFile(lrcFilePath);
      }

      return lRCFile;
    } 

    #endregion
  }
}
