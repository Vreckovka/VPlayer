using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VPlayer.Core.Design;
using VPlayer.Core.Design.ViewModels;

namespace VPlayer.Library.Desing.ViewModels
{
  public class AlbumsDesignViewModel
  {
    public AlbumsDesignViewModel()
    {
      View = new List<AlbumDesignViewModel>();

      foreach (var album in DesingDatabase.Instance.Albums)
      {
        View.Add(new AlbumDesignViewModel(album));
      }

      View[1].IsPlaying = true;
    }

    public List<AlbumDesignViewModel> View { get; set; } 
  }
}
