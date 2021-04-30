using System.Threading.Tasks;

namespace VPlayer.Library.ViewModels.TvShows
{
  public interface ITvShowScrapper
  {
    Task UpdateTvShowFromCsfd(int tvShowId, string csfUrl);
    Task UpdateTvShowSeasonFromCsfd(int tvShowSeasonId, string csfUrl);

    Task UpdateTvShowName(int tvShowId, string name);

    Task UpdateTvShowCsfdUrl(int tvShowId, string url);
  }
}