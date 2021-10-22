using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using VCore.WPF.LRC;
using VCore.WPF.LRC.Domain;

namespace VPlayer.AudioStorage.InfoDownloader.LRC.Clients
{
  public class LocalLrcProvider : LrcProvider<FileInfo>
  {
    private readonly string basePath;

    public LocalLrcProvider(string basePath)
    {
      this.basePath = basePath ?? throw new ArgumentNullException(nameof(basePath));
    }

    #region ParseLRCFile

    public LRCFile ParseLRCFile(string lrcFilePath)
    {
      var parser = new LRCParser();

      var lrcFile = parser.Parse(lrcFilePath);

      return lrcFile;
    }

    #endregion

    #region FindFile

    protected override Task<FileInfo> GetFile(string songName, string artistName, string albumName, string extension)
    {
      return Task.Run(() =>
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

          var fileName = GetFileName(artistName, songName);

          if (fileName != null)
          {
            var filesInDir = directory.GetFiles($"*{fileName}*.{extension}").ToList();

            if (filesInDir.Count == 1)
            {
              return filesInDir?.FirstOrDefault();
            }
            else if (filesInDir.Count == 0)
            {
              return null;
            }
            else
              throw new Exception("More files were found '" + songName + "' in '" + directory.FullName + "'");
          }
        }

        return null;
      });
    }

    public override LRCProviders LRCProvider => LRCProviders.Local;

    #endregion

    #region GetLinesLrcFileAsync

    protected override Task<KeyValuePair<string[],ILRCFile>> GetLinesLrcFileAsync(string songName, string artistName, string albumName)
    {
      return Task.Run<KeyValuePair<string[], ILRCFile>>(async () =>
      {
        var lrcFile = (await GetFile(songName, artistName, albumName,".lrc"))?.FullName;

        if (!string.IsNullOrEmpty(lrcFile))
        {
          return new KeyValuePair<string[], ILRCFile>(File.ReadAllLines(lrcFile),new LRCFile(null));
        }

        return new KeyValuePair<string[], ILRCFile>(null,null);
      });
    }

    #endregion

    public override Task<bool> Update(ILRCFile lRCFile)
    {
      throw new NotImplementedException();
    }
  }
}
