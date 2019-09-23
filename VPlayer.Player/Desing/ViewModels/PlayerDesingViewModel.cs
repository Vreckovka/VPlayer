using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Events;
using VCore.Annotations;
using VCore.Modularity.RegionProviders;
using VCore.ViewModels;
using VPlayer.Core.Desing;
using VPlayer.Core.ViewModels;
using VPlayer.Player.ViewModels;

namespace VPlayer.Player.Desing.ViewModels
{
  public class PlayerDesingViewModel : ViewModel
  {
    public PlayerDesingViewModel()
    {
      PlayList = new ObservableCollection<SongInPlayList>();

      foreach (var song in DesingDatabase.Instance.Albums[0].Songs)
      {
        PlayList.Add(new SongInPlayList(song));
      }

      PlayList[2].IsPlaying = true;
      ActualSong = PlayList[2];
      ActualSong.ActualPosition = (float)0.37;

      SelectedSong = PlayList[3];
    }

    public SongInPlayList ActualSong { get; set; }
    public SongInPlayList SelectedSong { get; set; }
    public static bool IsPlaying { get; set; }
    public ObservableCollection<SongInPlayList> PlayList { get; set; }

  }
}
