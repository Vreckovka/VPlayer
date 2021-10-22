using System.Collections.Generic;
using VCore.Standard.Modularity.Interfaces;
using VCore.WPF.Design;
using VPlayer.Core.Modularity.Regions;
using VPlayer.Home.Desing.ViewModels;
using VPlayer.Home.Views.Music.Albums;

namespace VPlayer.Home.Desing
{
  public class LibraryRegionDesingProvider : DesignTimeViewsProvider
  {
    protected override IEnumerable<IView> OnGetViewForRegion(string regionName)
    {
      if (regionName == RegionNames.HomeContentRegion)
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
