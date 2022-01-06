using System.Threading;
using System.Threading.Tasks;
using VPlayer.AudioStorage.Scrappers.CSFD.Domain;
using VPlayer.Core.Managers.Status;

namespace VPlayer.AudioStorage.Scrappers.CSFD
{
  public interface ICSFDWebsiteScrapper
  {
    CSFDTVShow LoadTvShow(string url,
      CancellationToken cancellationToken,
      int? seasonNumber = null, 
      int? episodeNumber = null,
      StatusMessageViewModel parentMessage = null,
      string fileName = null);

    CSFDTVShowSeason LoadTvShowSeason(string url, CancellationToken cancellationToken);
    Task<CSFDQueryResult> FindItems(string name, CancellationToken cancellationToken);
    Task<CSFDItem> GetBestFind(
      string name,
      CancellationToken cancellationToken,
      int? year = null, 
      bool onlySingleItem = false, 
      string tvShowUrl = null,
      string tvShowName = null,
      int? seasonNumber = null, 
      int? episodeNumber = null,
      bool downloadSingleSeason = false);
  }
}