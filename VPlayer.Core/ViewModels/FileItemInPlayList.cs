using System;
using System.Linq;
using System.Windows.Input;
using FFMpegCore;
using Prism.Events;
using VCore.Standard.Modularity.Interfaces;
using VCore.WPF.Misc;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Events;
using VPlayer.Core.ViewModels.SoundItems;

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

    #region Commands

    #region RefreshData

    private ActionCommand refreshData;

    public ICommand RefreshData
    {
      get
      {
        if (refreshData == null)
        {
          refreshData = new ActionCommand(OnRefreshData);
        }

        return refreshData;
      }
    }

    public async void OnRefreshData()
    {
      try
      {
        if (!string.IsNullOrEmpty(Model?.Source)
            && !Model.Source.Contains("https://")
            && !Model.Source.Contains("http://"))
        {
          var mediaAnalysis = await FFProbe.AnalyseAsync(Model.Source);
          MediaInfo = mediaAnalysis;

          if (Model is SoundItem soundItem)
          {
            if (mediaInfo.Format.Tags?.ContainsKey("album") == true)
              soundItem.FileInfoEntity.Album = mediaInfo.Format.Tags["album"];

            if (mediaInfo.Format.Tags?.ContainsKey("artist") == true)
              soundItem.FileInfoEntity.Artist = mediaInfo.Format.Tags["artist"];
          }

          var tmp = Model;
          Model = null;
          RaisePropertyChanged(nameof(Model));
          Model = tmp;
          RaisePropertyChanged(nameof(Model));
        }
      }
      catch (Exception)
      {
      }
    }

    #endregion

    #region ClearInfo

    private ActionCommand clearInfo;

    public ICommand ClearInfo
    {
      get
      {
        if (clearInfo == null)
        {
          clearInfo = new ActionCommand(OnClearInfo);
        }

        return clearInfo;
      }
    }

    public virtual void OnClearInfo() { }


    #endregion

    #region DownloadInfo

    private ActionCommand downloadInfo;

    public ICommand DownloadInfo
    {
      get
      {
        if (downloadInfo == null)
        {
          downloadInfo = new ActionCommand(OnDownloadInfo);
        }

        return downloadInfo;
      }
    }

    protected abstract void OnDownloadInfo();


    #endregion

    #region ResetAllData

    private ActionCommand resetAllData;

    public ICommand ResetAllData
    {
      get
      {
        if (resetAllData == null)
        {
          resetAllData = new ActionCommand(OnResetAllDataAndSave);
        }

        return resetAllData;
      }
    }

    public override void OnResetAllDataAndSave()
    {
      OnRefreshData();
      OnClearInfo();
      OnDownloadInfo();

      SaveChanges();
    }

    #endregion

    #endregion

    public virtual void SaveChanges()
    {

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
          RaisePropertyChanged(nameof(ActualTime));
          RaisePropertyChanged(nameof(LeftTime));
          RaisePropertyChanged(nameof(ActualPerc));
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
          if (value <= 1)
            actualPosition = value;


          RaisePropertyChanged();
          RaisePropertyChanged(nameof(ActualTime));
          RaisePropertyChanged(nameof(LeftTime));
          RaisePropertyChanged(nameof(ActualPerc));
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

    #region DateTime

    private DateTime created;

    public DateTime Created
    {
      get { return created; }
      set
      {
        if (value != created)
        {
          created = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region Modified

    private DateTime modified;

    public DateTime Modified
    {
      get { return modified; }
      set
      {
        if (value != modified)
        {
          modified = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region MediaInfo

    private IMediaAnalysis mediaInfo;

    public IMediaAnalysis MediaInfo
    {
      get { return mediaInfo; }
      set
      {
        if (value != mediaInfo)
        {
          mediaInfo = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region ActualTime

    public TimeSpan ActualTime
    {
      get
      {
        if (ActualPosition <= -1)
        {
          return TimeSpan.Zero;
        }

        var seconds = ActualPosition * Duration;

        if (TimeSpan.MaxValue.TotalSeconds > seconds)
        {
          return TimeSpan.FromSeconds(ActualPosition * Duration);
        }

        return TimeSpan.FromSeconds(0);
      }
    }

    #endregion

    #region LeftTime

    public TimeSpan LeftTime
    {
      get { return TimeSpan.FromSeconds(Duration) - ActualTime; }
    }

    #endregion

    #region ActualPerc

    public double ActualPerc
    {
      get { return (ActualTime.TotalSeconds / Duration) * 100; }
    }

    #endregion

    #region OnResetAllData

    public override void OnResetAllData()
    {
      OnRefreshData();
      OnClearInfo();

      base.OnResetAllData();
    }

    #endregion

  }
}