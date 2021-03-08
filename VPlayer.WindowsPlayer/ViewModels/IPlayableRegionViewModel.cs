using System;
using VCore.ViewModels;
using Vlc.DotNet.Wpf;

namespace VPlayer.Core.ViewModels
{
  public interface IPlayableRegionViewModel : IRegionViewModel
  {
    void Play();
    void PlayPause();
    void SetItemAndPlay(int? songIndex, bool forcePlay = false);
    void PlayPrevious();
    void PlayNext();
    void Pause();
    bool IsPlaying { get; set; }
    bool CanPlay { get; }
    VlcControl VlcControl { get; }
    void SeekForward(int seekSize);
    void SeekBackward(int seekSize);
    IObservable<int> ActualItemChanged { get; }
    void SetVolume(int pVolume);
  }
}