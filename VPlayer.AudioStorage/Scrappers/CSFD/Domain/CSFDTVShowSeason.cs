using System.Collections.Generic;

namespace VPlayer.AudioStorage.Scrappers.CSFD.Domain
{
  public class CSFDTVShowSeason : CSFDItem
  {
    public int SeasonNumber { get; set; }

    public List<CSFDTVShowSeasonEpisode> SeasonEpisodes { get; set; }
  }
}