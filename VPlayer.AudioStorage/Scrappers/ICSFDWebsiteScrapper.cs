using System.Threading.Tasks;

namespace VPlayer.AudioStorage.Parsers
{
  public interface ICSFDWebsiteScrapper
  {
    CSFDTVShow LoadTvShow(string url);
  }
}