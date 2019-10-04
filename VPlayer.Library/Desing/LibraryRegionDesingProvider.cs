using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using VCore.Design;
using VCore.Modularity.Interfaces;
using VPlayer.Core.Modularity.Regions;
using VPlayer.Library.Desing.ViewModels;
using VPlayer.Library.Views;

namespace VPlayer.Library.Desing
{
  public class LibraryRegionDesingProvider : DesignTimeViewsProvider
  {
    protected override IEnumerable<IView> OnGetViewForRegion(string regionName)
    {
      if (regionName == RegionNames.LibraryContentRegion)
      {
        var list = new List<IView>();

        var albumsView = new AlbumsView();
        albumsView.DataContext = new AlbumsDesignViewModel();

        list.Add(albumsView);

        return list;
      }

      return null;
    }
  }
}
