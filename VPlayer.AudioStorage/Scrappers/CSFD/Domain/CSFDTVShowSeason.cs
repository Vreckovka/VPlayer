using System.Collections.Generic;

namespace VPlayer.AudioStorage.Scrappers.CSFD.Domain
{
  public class CSFDTVShowSeason
  {
    public string Name { get; set; }
    public string SeasonUrl { get; set; }

    public List<CSFDTVShowSeasonEpisode> SeasonEpisodes { get; set; }
  }
}