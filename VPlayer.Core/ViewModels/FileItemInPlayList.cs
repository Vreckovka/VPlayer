using System;
using Prism.Events;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.Interfaces.Storage;

namespace VPlayer.Core.ViewModels
{
  public abstract class FileItemInPlayList<TModel> : ItemInPlayList<TModel>, IFileItemInPlayList<TModel>
    where TModel : class, IFilePlayableModel,
    IUpdateable<TModel>, IEntity
  {
    public FileItemInPlayList(
      TModel model,
      IEventAggregator eventAggregator,
      IStorageManager storageManager) : base(model, eventAggregator,storageManager)
    {
      Duration = model.Duration;
    }

    #region Duration

    private int duration;

    public int Duration
    {
      get { return duration; }
      set
      {
        if (value != duration)
        {
          duration = value;

          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region ActualPosition

    private float actualPosition;

    public float ActualPosition
    {
      get { return actualPosition; }
      set
      {
        if (value != actualPosition)
        {
          actualPosition = value;

          RaisePropertyChanged();
          RaisePropertyChanged(nameof(ActualTime));

          OnActualPositionChanged(value);
        }
      }
    }

    #endregion

    protected virtual void OnActualPositionChanged(float value)
    {
    }


    public TimeSpan ActualTime => TimeSpan.FromSeconds(ActualPosition * Duration);
  }
}