using System;
using System.Windows.Input;
using Prism.Events;
using VCore;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.ViewModels;

namespace VPlayer.Library.ViewModels
{
  public abstract class PlaylistViewModel<TPlaylistItemViewModel,TPlaylistModel, TPlaylistItemModel> : PlayableViewModel<TPlaylistItemViewModel, TPlaylistModel>
    where TPlaylistModel : class, IPlaylist<TPlaylistItemModel>
    where TPlaylistItemModel : class, IEntity
  {
    private readonly IStorageManager storageManager;
   

    protected PlaylistViewModel(
      TPlaylistModel model, 
      IEventAggregator eventAggregator,
      IStorageManager storageManager) : base(model, eventAggregator)
    {
      this.storageManager = storageManager ?? throw new ArgumentNullException(nameof(storageManager));
    }

    #region Properties

    public bool IsUserCreated => Model.IsUserCreated;

    #region IsRepeating

    public bool IsRepeating
    {
      get
      {
        return Model.IsReapting;
      }
      set
      {
        if (value != Model.IsReapting)
        {
          Model.IsReapting = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region IsShuffle

    public bool IsShuffle
    {
      get
      {
        return Model.IsShuffle;
      }
      set
      {
        if (value != Model.IsShuffle)
        {
          Model.IsShuffle = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region LastPlayed

    public DateTime LastPlayed
    {
      get { return Model.LastPlayed; }
      set
      {
        if (value != Model.LastPlayed)
        {
          Model.LastPlayed = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region TotalPlayedTime

    public TimeSpan TotalPlayedTime
    {
      get { return Model.TotalPlayedTime; }
      set
      {
        if (value != Model.TotalPlayedTime)
        {
          Model.TotalPlayedTime = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    public int? ItemsCount => Model.ItemCount;
    public long? HashCode => Model.HashCode;

    #endregion

    #region Commands

    #region Delete

    private ActionCommand delete;

    public ICommand Delete
    {
      get
      {
        if (delete == null)
        {
          delete = new ActionCommand(OnDelete);
        }

        return delete;
      }
    }

    public void OnDelete()
    {
      storageManager.DeletePlaylist<TPlaylistModel, TPlaylistItemModel>(Model);
    }

    #endregion

    #endregion

    #region Methods

    #region RaisePropertyChanges

    public virtual void RaisePropertyChanges()
    {
      RaisePropertyChanged(nameof(LastPlayed));
      RaisePropertyChanged(nameof(Name));
      RaisePropertyChanged(nameof(IsUserCreated));
      RaisePropertyChanged(nameof(ItemsCount));
      RaisePropertyChanged(nameof(HashCode));
      RaisePropertyChanged(nameof(IsShuffle));
      RaisePropertyChanged(nameof(IsRepeating));
      RaisePropertyChanged(nameof(TotalPlayedTime));
    }

    #endregion

    #region Update

    public override void Update(TPlaylistModel updateItem)
    {
      this.Model.Update(updateItem);

      RaisePropertyChanges();
    }

    #endregion


    #endregion



  }
}