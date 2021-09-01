using System.Threading.Tasks;
using VPlayer.AudioStorage.Scrappers.CSFD.Domain;

namespace VPlayer.AudioStorage.Scrappers.CSFD
{
  public interface ICSFDWebsiteScrapper
  {
    CSFDTVShow LoadTvShow(string url);

    CSFDTVShowSeason LoadTvShowSeason(string url);
    Task<CSFDQueryResult> FindItems(string name);
    Task<CSFDItem> GetBestFind(string name, int? year = null, bool onlySingleItem = false);
  }
}