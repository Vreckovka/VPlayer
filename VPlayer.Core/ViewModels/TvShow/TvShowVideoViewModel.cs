using System.Collections.Generic;
using Prism.Events;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.Core.ViewModels;

namespace VPlayer.WindowsPlayer.ViewModels
{
  public class TvShowVideoViewModel : VideoViewModel<TvShowEpisode>
  {
    public TvShowVideoViewModel(TvShowEpisode model) : base(model)
    {
    }
  }
}