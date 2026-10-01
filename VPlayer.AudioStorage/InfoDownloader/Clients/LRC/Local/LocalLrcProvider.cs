using System;
using System.Collections.Generic;
using System.IO;
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

    public LRCFile ParseLRCFile(string lrcFilePath) => new LRCParser().Parse(lrcFilePath);

    private string GetDirectoryPath(string artistName, string albumName)
    {
      return Path.Combine(basePath, GetPathValidName(artistName) ?? "", GetPathValidName(albumName) ?? "");
    }

    protected override Task<FileInfo> GetFile(string songName, string artistName, string albumName, string extension)
    {
      return Task.Run(() =>
      {
        var fileName = GetFileName(artistName, songName);
        if (fileName == null) return null;
        var file = new FileInfo(Path.Combine(GetDirectoryPath(artistName, albumName), fileName + extension));
        return file.Exists ? file : null;
      });
    }

    public override LRCProviders LRCProvider => LRCProviders.Local;

    protected override async Task<KeyValuePair<string[], ILRCFile>> GetLinesLrcFileAsync(string songName, string artistName, string albumName)
    {
      var file = await GetFile(songName, artistName, albumName, ".lrc").ConfigureAwait(false);
      if (file == null)
        return new KeyValuePair<string[], ILRCFile>(null, null);
      var lines = await File.ReadAllLinesAsync(file.FullName).ConfigureAwait(false);
      return new KeyValuePair<string[], ILRCFile>(lines, new LRCFile(null));
    }

    public override async Task<bool> Update(ILRCFile lrcFile)
    {
      if (lrcFile?.Lines == null) return false;
      var name = GetFileName(lrcFile.Artist, lrcFile.Title);
      if (name == null) return false;
      var path = GetDirectoryPath(lrcFile.Artist, lrcFile.Album);
      Directory.CreateDirectory(path);
      await File.WriteAllTextAsync(Path.Combine(path, name + ".lrc"), lrcFile.GetString()).ConfigureAwait(false);
      return true;
    }
  }
}
