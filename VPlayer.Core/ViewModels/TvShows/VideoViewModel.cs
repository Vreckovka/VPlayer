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
    public bool IsInPlaylist { get; set; }
    public void OnResetAllDataAndSave()
    {
      
    }

    public void OnResetAllData()
    {
      
    }

    public string FileLocation { get; set; }

    #region IsSelected

    private bool isSelected;

    public bool IsSelected
    {
      get { return isSelected; }
      set
      {
        if (value != isSelected)
        {
          isSelected = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

  }
}