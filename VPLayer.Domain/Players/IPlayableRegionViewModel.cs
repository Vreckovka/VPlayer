using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using VCore.ItemsCollections;
using VCore.Standard.ViewModels.TreeView;
using VCore.WPF.ViewModels;
using VPlayer.WindowsPlayer.Players;

namespace VPlayer.Core.ViewModels
{
  public interface IFilePlayableRegionViewModel : IPlayableRegionViewModel
  {
    void SeekForward(int seekSize = 50);
    void SeekBackward(int seekSize = 50);
    void SetMediaPosition(float position);
  }

  public interface IPlayableRegionViewModel : IRegionViewModel
  {
  
    bool IsPlaying { get; set; }
    bool IsSelectedToPlay { get; set; }
    bool CanPlay { get; }
    IPlayer MediaPlayer { get; }
    string ActualSearch { get; set; }
    IObservable<int> ActualItemChanged { get; }
    IObservable<int> OnVolumeChanged { get; }
    public bool PlayNextItemOnEndReached { get; set; }
    public int Volume { get; set; }



    void SetVolumeWihtoutNotification(int pVolume);
    void SetVolumeAndRaiseNotification(int pVolume);
    IEnumerable<string> GetAllItemsSources();
    Task ClearPlaylist();
    Task Play();
    void PlayPause();
    void SetItemAndPlay(int? itemIndex, bool forcePlay = false, bool onlyItemSet = false);
    void PlayPrevious();
    void PlayNext();
    void Pause();
  }
}