using System;
using VCore.ViewModels;
using Vlc.DotNet.Wpf;

namespace VPlayer.Core.ViewModels
{
  public interface IPlayableRegionViewModel : IRegionViewModel
  {
    void Play();
    void PlayPause();
    void PlayNext(int? songIndex, bool forcePlay = false);
    void PlayPrevious();
    void Stop();
    bool IsPlaying { get; }
    bool CanPlay { get; }
    VlcControl VlcControl { get; }

    IObservable<int> ActualItemChanged { get; }

  }
}