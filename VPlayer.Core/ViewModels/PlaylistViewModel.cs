using System;
using System.Windows.Input;
using Prism.Events;
using VCore;
using VCore.ViewModels.Navigation;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.ViewModels;
using VPlayer.Core.ViewModels.Artists;

namespace VPlayer.Library.ViewModels
{
  public abstract class PlaylistViewModel<TPlaylistItemViewModel,TPlaylistModel, TPlaylistItemModel> : 
    PlayableViewModel<TPlaylistItemViewModel, TPlaylistModel> 
    where TPlaylistModel : class, IPlaylist<TPlaylistItemModel>
    where TPlaylistItemModel : class, IEntity
  {
    protected readonly IStorageManager storageManager;
   

    protected PlaylistViewModel(
      TPlaylistModel model, 
      IEventAggregator eventAggregator,
      IStorageManager storageManager) : base(model, eventAggregator)
    {
      this.storageManager = storageManager ?? throw new ArgumentNullException(nameof(storageManager));
    }

    #region Properties

    #region IsActive

    private bool isActive;

    public bool IsActive
    {
      get { return isActive; }
      set
      {
        if (value != isActive)
        {
          isActive = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    public string Header
    {
      get
      {
        return ToString();
      }
    }


    public bool IsUserCreated => Model.IsUserCreated;

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