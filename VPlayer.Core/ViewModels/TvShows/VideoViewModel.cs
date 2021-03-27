using System;
using VCore.Standard;

namespace VPlayer.Core.ViewModels.TvShows
{
  public class VideoViewModel<TModel> : ViewModel<TModel>, IItemInPlayList<TModel> 
    where TModel : IPlayableModel
  {
    public VideoViewModel(TModel model) : base(model)
    {
    }

    public float ActualPosition { get; set; }
    public TimeSpan ActualTime { get; }
    public int Duration { get; set; }
    public bool IsPaused { get; set; }
    public string Name { get; set; }
    public bool IsFavorite { get; set; }
    public bool IsPlaying { get; set; }

    public string FileLocation { get; set; }

   
  }
}