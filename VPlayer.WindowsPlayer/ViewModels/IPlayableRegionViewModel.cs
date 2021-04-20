using System;
using System.Threading.Tasks;
using LibVLCSharp.Shared;
using VCore.ViewModels;
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
    Task Play();
    void PlayPause();
    void SetItemAndPlay(int? songIndex, bool forcePlay = false, bool onlyItemSet = false);
    void PlayPrevious();
    void PlayNext();
    void Pause();
    bool IsPlaying { get; set; }
    bool IsSelectedToPlay { get; set; }
    bool CanPlay { get; }
    IPlayer MediaPlayer { get; }
    IObservable<int> ActualItemChanged { get; }
    void SetVolume(int pVolume);
  }
}