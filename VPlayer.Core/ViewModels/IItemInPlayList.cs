using System;
using System.ComponentModel;

namespace VPlayer.Core.ViewModels
{
  public interface IItemInPlayList<TModel> : INotifyPropertyChanged
    where TModel : IPlayableModel
  {
    float ActualPosition { get; set; }
    TimeSpan ActualTime { get; }
    int Duration { get; }
    bool IsPaused { get; set; }
    string Name { get; }
    bool IsFavorite { get; set; }
    bool IsPlaying { get; set; }
    TModel Model { get; set; }
  }
}