using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using Prism.Events;
using VCore;
using VCore.Annotations;
using VCore.Standard;
using VPlayer.Core.Events;

namespace VPlayer.Core.ViewModels
{
  public abstract class ItemInPlayList<TModel> : ViewModel<TModel>, IItemInPlayList<TModel> where TModel : IPlayableModel
  {
    protected readonly IEventAggregator eventAggregator;

    protected ItemInPlayList(TModel model, [NotNull] IEventAggregator eventAggregator) : base(model)
    {
      this.eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
    }

    #region Properties

    public int Duration => Model.Duration;

    public TimeSpan ActualTime => TimeSpan.FromSeconds(ActualPosition * Duration);

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

    #endregion

    #region Commands

    #region DeleteSongFromPlaylist

    private ActionCommand deleteSongFromPlaylist;

    public ICommand DeleteSongFromPlaylist
    {
      get
      {
        if (deleteSongFromPlaylist == null)
        {
          deleteSongFromPlaylist = new ActionCommand(OnDeleteSongFromPlaylist);
        }

        return deleteSongFromPlaylist;
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

      if (!string.IsNullOrEmpty(Model.DiskLocation))
      {
        var folder = Path.GetDirectoryName(Model.DiskLocation);

        if (!string.IsNullOrEmpty(folder))
        {
          Process.Start(folder);
        }
      }
    }


    #endregion

    #endregion

    protected abstract void UpdateIsFavorite(bool value);
    protected abstract void OnActualPositionChanged(float value);
    protected abstract void OnIsPlayingChanged(bool value);
    protected abstract void PublishPlayEvent();

    protected abstract void PublishRemoveFromPlaylist();
  }
}