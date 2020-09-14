using VPlayer.AudioStorage.InfoDownloader.LRC.Domain;

namespace VPlayer.AudioStorage.InfoDownloader.LRC
{
  public interface ILrcLyricsProvider
  {
    string GetFileName(string artistName, string songName);
    LRCFile ParseLRCFile(string lrcFilePath);
    LRCFile TryGetLrc(string songName, string artistName, string albumName);

  }
}