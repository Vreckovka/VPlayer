using System;
using System.Windows.Input;
using VCore.Standard;
using VCore.Standard.Modularity.Interfaces;
using VCore.WPF.Misc;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.Interfaces.Storage;

namespace VPlayer.Core.ViewModels.TvShows
{
  public class VideoViewModel<TModel> : ViewModel<TModel>, IItemInPlayList<TModel> 
    where TModel : class, IPlayableModel, IUpdateable<TModel>
  {
    private readonly IStorageManager storageManager;

    public VideoViewModel(TModel model, IStorageManager storageManager) : base(model)
    {
      this.storageManager = storageManager ?? throw new ArgumentNullException(nameof(storageManager));
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

    public bool IsPrivate
    {
      get
      {
        return Model.IsPrivate;
      }
    }

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

    #region SetPrivate

    private ActionCommand setPrivate;

    public ICommand SetPrivate
    {
      get
      {
        if (setPrivate == null)
        {
          setPrivate = new ActionCommand(() => OnSetPrivate(null));
        }

        return setPrivate;
      }
    }


    public async void OnSetPrivate(bool? isPrivate = null)
    {
      if (isPrivate == null)
      {
        Model.IsPrivate = !Model.IsPrivate;
      }
      else if (Model.IsPrivate != isPrivate.Value)
      {
        Model.IsPrivate = isPrivate.Value;

        await storageManager.UpdateEntityAsync(Model);
      }

      RaisePropertyChanged(nameof(IsPrivate));
    }

    public void RaiseNotifyPropertyChanged(string propertyName)
    {
      RaisePropertyChanged(propertyName);
    }

    #endregion
  }
}