﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using VCore.Modularity.Interfaces;
using VCore.Modularity.RegionProviders;
using VCore.ViewModels;

namespace VPlayer.Core.ViewModels
{
  public abstract class PlayableRegionViewModel<TView> : RegionViewModel<TView>, IPlayableRegionViewModel
    where TView : class, IView
  {
    public PlayableRegionViewModel(IRegionProvider regionProvider) : base(regionProvider)
    {
    }

    public abstract void Play();
    public abstract void Pause();
    public abstract void PlayNext(int? songIndex);
    public abstract void PlayPrevious();
    public abstract void Stop();
    public abstract bool IsPlaying { get; protected set; }
    public Subject<bool> IsPlayingSubject { get; } = new Subject<bool>();
    public abstract bool CanPlay { get; }
  }

  public interface IPlayableRegionViewModel
  {
    void Play();
    void Pause();
    void PlayNext(int? songIndex);
    void PlayPrevious();
    void Stop();
    bool IsPlaying { get; }
    bool CanPlay { get; }
  }
}