using System;
using System.Linq;
using System.Threading.Tasks;
using Ninject;
using VPlayer.AudioStorage.InfoDownloader.LRC.Domain;

namespace VPlayer.AudioStorage.InfoDownloader.LRC
{
  public enum LRCProviders
  {
    NotIdentified,
    Google,
    Local
  }

  public abstract class LrcProvider<TFileOutput> : ILrcProvider, IInitializable
  {
    #region ParseLRCFile

    public LRCFile ParseLRCFile(string[] lines)
    {
      var parser = new LRCParser();

      var lrcFile = parser.Parse(lines.ToList());

      if (lrcFile == null)
      {
        throw new Exception("Could not parse");
      }

      return lrcFile;
    }

    #endregion

    #region GetFileName

    public string GetFileName(string artistName, string songName)
    {
      return $"{artistName} - {songName}";
    }

    #endregion

    #region TryGetLrcAsync

    public async Task<LRCFile> TryGetLrcAsync(string songName, string artistName, string albumName)
    {
      var lrcFilePath = await GetLinesLrcFileAsync(songName, artistName, albumName);

      LRCFile lRCFile = null;

      if (lrcFilePath != null)
      {
        lRCFile = ParseLRCFile(lrcFilePath);

        if (lRCFile == null && LrcFileExists(songName, artistName, albumName))
        {
          throw new Exception("FAILED TO PARSE " + songName + " " + artistName + " " + albumName);
        }
      }

      return lRCFile;
    }

    #endregion

    #region LrcFileExists

    protected bool LrcFileExists(string songName, string artistName, string albumName)
    {
      var fileName = GetFile(songName, artistName, albumName);

      if (fileName != null)
      {
        return true;
      }

      return false;
    }

    #endregion

    protected abstract Task<string[]> GetLinesLrcFileAsync(string songName, string artistName, string albumName);
    protected abstract TFileOutput GetFile(string songName, string artistName, string albumName);

    public abstract LRCProviders LRCProvider { get; }

    public void Initialize()
    {
    }
  }
}