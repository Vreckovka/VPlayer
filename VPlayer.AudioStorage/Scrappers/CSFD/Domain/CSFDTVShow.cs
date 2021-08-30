using System.Collections.Generic;

namespace VPlayer.AudioStorage.Scrappers.CSFD.Domain
{
  public class CSFDTVShow : CSFDItem
  {
    public List<CSFDTVShowSeason> Seasons { get; set; }
  }
}