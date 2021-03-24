using System;
using System.Threading.Tasks;
using LibVLCSharp.Shared;
using VCore.ViewModels;

namespace VPlayer.Core.ViewModels
{
  public interface IPlayableRegionViewModel : IRegionViewModel
  {
    Task Play();
    void PlayPause();
    void SetItemAndPlay(int? songIndex, bool forcePlay = false);
    void PlayPrevious();
    void PlayNext();
    void Pause();
    bool IsPlaying { get; set; }
    bool IsSelectedToPlay { get; set; }
    bool CanPlay { get; }
    MediaPlayer MediaPlayer { get; }
    void SeekForward(int seekSize);
    void SeekBackward(int seekSize);
    IObservable<int> ActualItemChanged { get; }
    void SetVolume(int pVolume);
  }
}