using System;
using System.ComponentModel;
using FFMpegCore;
using VCore.Standard.ViewModels.TreeView;

namespace VPlayer.Core.ViewModels
{
  public interface IFileItemInPlayList : IItemInPlayList
  {
    int Duration { get; set; }
    TimeSpan ActualTime { get; }
    float ActualPosition { get; set; }
    IMediaAnalysis MediaInfo { get; set; }

    DateTime Created { get; set; }
    DateTime Modified { get; set; }
  }

  public interface IFileItemInPlayList<TModel> : IItemInPlayList<TModel>, IFileItemInPlayList where TModel : IPlayableModel
  {
  }

  public interface IItemInPlayList<TModel> : IItemInPlayList, ISelectable where TModel : IPlayableModel
  {
    TModel Model { get; set; }
    bool IsPrivate { get; }
    public void OnSetPrivate(bool? isPrivate = null);
    public void RaiseNotifyPropertyChanged(string propertyName);
  }
 

  public interface IItemInPlayList : INotifyPropertyChanged 
  {
    bool IsPaused { get; set; }
    string Name { get; }
    bool IsFavorite { get; set; }
    /// <summary>
    /// Is in order to play not actual play state, wrong naming
    /// </summary>
    bool IsPlaying { get; set; }
    bool IsInPlaylist { get; set; }

    void OnResetAllDataAndSave();
    void OnResetAllData();
  }
}