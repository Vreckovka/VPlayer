using System;
using System.ComponentModel;
using VCore.Standard.ViewModels.TreeView;

namespace VPlayer.Core.ViewModels
{
  public interface IFileItemInPlayList : IItemInPlayList
  {
    int Duration { get; set; }
    TimeSpan ActualTime { get; }
    float ActualPosition { get; set; }
  }

  public interface IFileItemInPlayList<TModel> : IItemInPlayList<TModel>, IFileItemInPlayList where TModel : IPlayableModel
  {
  }

  public interface IItemInPlayList<TModel> : IItemInPlayList, ISelectable where TModel : IPlayableModel
  {
    TModel Model { get; set; }
  }
 

  public interface IItemInPlayList : INotifyPropertyChanged 
  {
    bool IsPaused { get; set; }
    string Name { get; }
    bool IsFavorite { get; set; }
    bool IsPlaying { get; set; }
    bool IsInPlaylist { get; set; }

    void OnResetAllDataAndSave();
    void OnResetAllData();
  }
}