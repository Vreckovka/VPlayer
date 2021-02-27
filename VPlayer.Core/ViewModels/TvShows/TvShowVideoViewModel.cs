using VPlayer.AudioStorage.DomainClasses;

namespace VPlayer.Core.ViewModels.TvShows
{
  public class TvShowVideoViewModel : VideoViewModel<TvShowEpisode>
  {
    public TvShowVideoViewModel(TvShowEpisode model) : base(model)
    {
    }
  }
}