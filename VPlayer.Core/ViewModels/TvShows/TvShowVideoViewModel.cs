using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.DomainClasses.Video;
using VPlayer.AudioStorage.Interfaces.Storage;

namespace VPlayer.Core.ViewModels.TvShows
{
  public class TvShowVideoViewModel : VideoViewModel<TvShowEpisode>
  {
    public TvShowVideoViewModel(TvShowEpisode model,IStorageManager storageManager) : base(model, storageManager)
    {
    }
  }
}