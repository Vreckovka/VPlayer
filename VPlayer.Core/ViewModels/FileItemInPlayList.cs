using System;
using System.Linq;
using Prism.Events;
using VCore.Standard.Modularity.Interfaces;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.Interfaces.Storage;

namespace VPlayer.Core.ViewModels
{
  public abstract class FileItemInPlayList<TModel> : ItemInPlayList<TModel>, IFileItemInPlayList<TModel>
    where TModel : class, IFilePlayableModel, IUpdateable<TModel>, IEntity
  {
    public FileItemInPlayList(
      TModel model,
      IEventAggregator eventAggregator,
      IStorageManager storageManager) : base(model, eventAggregator, storageManager)
    {
      Duration = model.Duration;
    }

    #region Duration

    public int Duration
    {
      get { return Model.Duration; }
      set
      {
        if (value != Model.Duration)
        {
          Model.Duration = value;

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
        if (value != actualPosition && !float.IsNaN(value) && !float.IsInfinity(value))
        {

          int multiplier = 10000;

          var stringValue = Math.Round((value * multiplier));

          var floatValue = stringValue / multiplier;

          if (floatValue <= 1)
            actualPosition = (float)floatValue;

          RaisePropertyChanged();
          RaisePropertyChanged(nameof(ActualTime));

          OnActualPositionChanged(value);
        }
      }
    }

    #endregion


    #region ExtraData

    private object extraData;

    public object ExtraData
    {
      get { return extraData; }
      set
      {
        if (value != extraData)
        {
          extraData = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    protected virtual void OnActualPositionChanged(float value)
    {
    }


    public TimeSpan ActualTime
    {
      get
      {
        var seconds = ActualPosition * Duration;

        if (TimeSpan.MaxValue.TotalSeconds > seconds)
        {
          return TimeSpan.FromSeconds(ActualPosition * Duration);
        }

        return TimeSpan.FromSeconds(0);
      }
    }
  }
}