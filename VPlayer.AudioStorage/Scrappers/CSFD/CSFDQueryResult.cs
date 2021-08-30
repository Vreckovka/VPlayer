using System.Collections.Generic;
using VPlayer.AudioStorage.Scrappers.CSFD.Domain;

namespace VPlayer.AudioStorage.Scrappers.CSFD
{
  public class CSFDQueryResult
  {
    public IEnumerable<CSFDItem> Movies { get; set; }
    public IEnumerable<CSFDItem> TvShows { get; set; }
  }
}