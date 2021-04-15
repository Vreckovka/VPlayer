using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using Prism.Events;
using VCore;
using VCore.Standard;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Events;

namespace VPlayer.Core.ViewModels
{
  public abstract class ItemInPlayList<TModel> : ViewModel<TModel>, IItemInPlayList<TModel>
    where TModel : class, IPlayableModel,
    IUpdateable<TModel>, IEntity
  {
    protected readonly IEventAggregator eventAggregator;
    private readonly IStorageManager storageManager;

    protected ItemInPlayList(TModel model, IEventAggregator eventAggregator, IStorageManager storageManager) : base(model)
    {
      this.eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
      this.storageManager = storageManager ?? throw new ArgumentNullException(nameof(storageManager));
    }


    #region Properties

    public bool IsPaused { get; set; }
    public string Name => Model.Name;
    public bool IsFavorite
    {
      get { return Model.IsFavorite; }
      set { UpdateIsFavorite(value); }
    }

    #region IsPlaying

    private bool isPlaying;

    public bool IsPlaying
    {
      get { return isPlaying; }
      set
      {
        if (value != isPlaying)
        {
          isPlaying = value;

          RaisePropertyChanged();

          OnIsPlayingChanged(value);
        }
      }
    }

    #endregion



    #endregion

    #region Commands

    #region DeleteItemFromPlaylist

    private ActionCommand deleteItemFromPlaylist;

    public ICommand DeleteItemFromPlaylist
    {
      get
      {
        if (deleteItemFromPlaylist == null)
        {
          deleteItemFromPlaylist = new ActionCommand(OnDeleteSongFromPlaylist);
        }

        return deleteItemFromPlaylist;
      }
    }

    public void OnDeleteSongFromPlaylist()
    {
      PublishRemoveFromPlaylist();
    }

    #endregion

    #region Play

    private ActionCommand play;

    public ICommand Play
    {
      get
      {
        if (play == null)
        {
          play = new ActionCommand(OnPlayButton);
        }

        return play;
      }
    }


    private void OnPlayButton()
    {
      if (!IsPlaying)
      {
        PublishPlayEvent();
      }
      else
      {
        eventAggregator.GetEvent<PlayPauseEvent>().Publish();
      }
    }



    #endregion

    #region OpenContainingFolder

    private ActionCommand openContainingFolder;

    public ICommand OpenContainingFolder
    {
      get
      {
        if (openContainingFolder == null)
        {
          openContainingFolder = new ActionCommand(OnOpenContainingFolder);
        }

        return openContainingFolder;
      }
    }


    private void OnOpenContainingFolder()
    {
      if (!string.IsNullOrEmpty(Model.Source))
      {
        var folder = Path.GetDirectoryName(Model.Source);

        if (!string.IsNullOrEmpty(folder))
        {
          Process.Start(new System.Diagnostics.ProcessStartInfo()
          {
            FileName = folder,
            UseShellExecute = true,
            Verb = "open"
          });
        }
      }
    }


    #endregion

    #endregion

    #region UpdateIsFavorite

    protected async void UpdateIsFavorite(bool value)
    {
      if (value != Model.IsFavorite)
      {
        var oldVAlue = Model.IsFavorite;
        Model.IsFavorite = value;

        var updated = await storageManager.UpdateEntityAsync(Model);

        if (updated)
        {
          RaisePropertyChanged(nameof(IsFavorite));
        }
        else
        {
          Model.IsFavorite = oldVAlue;
        }
      }
    }

    #endregion

    protected virtual void OnIsPlayingChanged(bool value)
    {
    }

    protected abstract void PublishPlayEvent();

    protected abstract void PublishRemoveFromPlaylist();
  }
}