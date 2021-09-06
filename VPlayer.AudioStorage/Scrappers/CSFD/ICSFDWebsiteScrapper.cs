using System.Threading.Tasks;
using VPlayer.AudioStorage.Scrappers.CSFD.Domain;

namespace VPlayer.AudioStorage.Scrappers.CSFD
{
  public interface ICSFDWebsiteScrapper
  {
    CSFDTVShow LoadTvShow(string url, int? seasonNumber = null, int? episodeNumber = null);

    CSFDTVShowSeason LoadTvShowSeason(string url);
    Task<CSFDQueryResult> FindItems(string name);
    Task<CSFDItem> GetBestFind(string name, int? year = null, bool onlySingleItem = false, string tvShowUrl = null, string tvShowName = null, int? seasonNumber = null, int? episodeNumber = null);
  }
}