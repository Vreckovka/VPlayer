using System;
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
    public abstract void PlayNext(int? songIndex, bool forcePlay = false);
    public abstract void PlayPrevious();
    public abstract void Stop();

    #region IsPlaying

    private bool isPlaying;

    public virtual bool IsPlaying
    {
      get { return isPlaying; }
      protected set
      {
        if (value != isPlaying)
        {
          isPlaying = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    public Subject<bool> IsPlayingSubject { get; } = new Subject<bool>();
    public abstract bool CanPlay { get; }
  }

  public interface IPlayableRegionViewModel
  {
    void Play();
    void Pause();
    void PlayNext(int? songIndex, bool forcePlay = false);
    void PlayPrevious();
    void Stop();
    bool IsPlaying { get; }
    bool CanPlay { get; }
  }
}
