using System.Threading.Tasks;
using VPlayer.AudioStorage.InfoDownloader.LRC.Domain;

namespace VPlayer.AudioStorage.InfoDownloader.LRC
{
  public interface ILrcProvider
  {
    LRCProviders LRCProvider { get; }

    string GetFileName(string artistName, string songName);


    LRCFile ParseLRCFile(string[] lines);


    Task<LRCFile> TryGetLrcAsync(string songName, string artistName, string albumName);
  }
}