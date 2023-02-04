using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.DomainClasses.Video;

namespace VPlayer.Core.ViewModels.TvShows
{
  public class TvShowVideoViewModel : VideoViewModel<TvShowEpisode>
  {
    public TvShowVideoViewModel(TvShowEpisode model) : base(model)
    {
    }
  }
}