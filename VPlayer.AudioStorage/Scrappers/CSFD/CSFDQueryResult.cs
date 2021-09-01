using System.Collections.Generic;
using System.Linq;
using VPlayer.AudioStorage.Scrappers.CSFD.Domain;

namespace VPlayer.AudioStorage.Scrappers.CSFD
{
  public class CSFDQueryResult
  {
    public IEnumerable<CSFDItem> Movies { get; set; }
    public IEnumerable<CSFDItem> TvShows { get; set; }

    #region AllItems

    private IEnumerable<CSFDItem> allItems;
    public IEnumerable<CSFDItem> AllItems
    {
      get
      {
        if (allItems == null)
        {
          allItems = GetAllItems();
        }

        return allItems;
      }
    }

    #endregion

    private List<CSFDItem> GetAllItems()
    {
      List<CSFDItem> items = null;

      if (Movies != null && TvShows != null)
      {
        items = Movies.Concat(TvShows).ToList();
      }
      else if (Movies != null)
      {
        items = Movies.ToList();
      }
      else if (TvShows != null)
      {
        items = TvShows.ToList();
      }

      return items;
    }
  }
}