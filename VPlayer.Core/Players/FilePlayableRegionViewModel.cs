﻿using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using LibVLCSharp.Shared;
using Logger;
using Ninject;
using Prism.Events;
using VCore.Modularity.RegionProviders;
using VCore.Standard.Modularity.Interfaces;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Events;
using VPlayer.Core.Providers;
using VPlayer.IPTV.ViewModels;
using VPlayer.WindowsPlayer.Players;

namespace VPlayer.Core.ViewModels
{
  public abstract class FilePlayableRegionViewModel<TView, TItemViewModel, TPlaylistModel, TPlaylistItemModel, TModel> :
    PlayableRegionViewModel<TView, TItemViewModel, TPlaylistModel, TPlaylistItemModel, TModel>, IFilePlayableRegionViewModel
    where TView : class, IView
    where TItemViewModel : class, IFileItemInPlayList<TModel>
    where TModel : IPlayableModel
    where TPlaylistModel : class, IFilePlaylist<TPlaylistItemModel>, new()
    where TPlaylistItemModel : IItemInPlaylist<TModel>
  {
    private long lastTimeChangedMs;
    

    protected FilePlayableRegionViewModel(IRegionProvider regionProvider, IKernel kernel, ILogger logger,
      IStorageManager storageManager,
      IEventAggregator eventAggregator,
      VLCPlayer vLCPlayer) : base(regionProvider, kernel, logger, storageManager, eventAggregator, vLCPlayer)
    {
    }

    #region Properties

    #region TotalPlaylistDuration

    public TimeSpan TotalPlaylistDuration
    {
      get { return TimeSpan.FromSeconds(PlayList.Sum(x => x.Duration)); }
    }

    #endregion
    
    #region IsBuffering

    private bool isBuffering;

    public bool IsBuffering
    {
      get { return isBuffering; }
      set
      {
        if (value != isBuffering)
        {
          isBuffering = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #endregion

    #region Methods

    #region InitializeAsync

    protected override Task InitializeAsync()
    {
      PlayList.CollectionChanged += PlayList_CollectionChanged;

      return base.InitializeAsync();
    }

    #endregion

    #region HookToVlcEvents

    protected override async Task HookToPlayerEvents()
    {
      await base.HookToPlayerEvents();

      MediaPlayer.TimeChanged += OnVlcTimeChanged;
      MediaPlayer.Buffering += MediaPlayer_Buffering;
    }

    #endregion

    #region OnNewItemPlay

    public override void OnNewItemPlay()
    {
      if (MediaPlayer.Media != null)
        MediaPlayer.Media.DurationChanged += Media_DurationChanged;
    }

    #endregion

    #region PlayNext

    public override void PlayNext()
    {
      base.PlayNext();

      if (actualItemIndex >= PlayList.Count)
      {
        if (IsRepeate)
          actualItemIndex = 0;
      }
    }

    #endregion

    #region OnPlayEvent

    protected override void OnPlayEvent(PlayItemsEventData<TItemViewModel> data)
    {
      if (data.IsShuffle.HasValue)
        IsShuffle = data.IsShuffle.Value;

      if (data.IsRepeat.HasValue)
        IsRepeate = data.IsRepeat.Value;
    }

    #endregion

    #region OnVlcTimeChanged

    private int lastTotalTimeSaved = 0;
    private void OnVlcTimeChanged(object sender, PlayerTimeChangedArgs eventArgs)
    {
      if (ActualItem != null)
      {
        var position = ((eventArgs.Time * 100) / (ActualItem.Duration * (float)1000.0)) / 100;

        if (!double.IsNaN(position) && !double.IsInfinity(position))
        {
          ActualItem.ActualPosition = position;
          ActualSavedPlaylist.LastItemElapsedTime = position;

          var deltaTimeChanged = eventArgs.Time - lastTimeChangedMs;

          if (deltaTimeChanged < 0)
          {
            deltaTimeChanged = 0;
          }

          lastTimeChangedMs = eventArgs.Time;

          PlaylistTotalTimePlayed += TimeSpan.FromMilliseconds(deltaTimeChanged);

#if RELEASE
          int totalSec = (int)PlaylistTotalTimePlayed.TotalSeconds;

          if (totalSec % 10 == 0 && totalSec > lastTotalTimeSaved)
          {
            lastTotalTimeSaved = totalSec;
            Task.Run(() =>
            {
              if (storageManager.UpdatePlaylist<TPlaylistModel, TPlaylistItemModel>(ActualSavedPlaylist, out var updated))
              {
                ActualSavedPlaylist = updated;
              }
            });
          }
#endif
        }
      }
    }

    #endregion

    #region Position methods

    #region SetMediaPosition

    public void SetMediaPosition(float position)
    {
      if (position < 0)
      {
        position = 0;
      }

      MediaPlayer.Position = position;

      ActualItem.ActualPosition = MediaPlayer.Position;

      lastTimeChangedMs = (long)(ActualItem.ActualPosition * (double)ActualItem.Duration) * 1000;
    }

    #endregion

    #region SeekForward

    public void SeekForward(int seekSize = 50)
    {
      var position = MediaPlayer.Position + GetSeekSize(seekSize);

      SetMediaPosition(position);

    }

    #endregion

    #region SeekBackward

    public void SeekBackward(int seekSize = 50)
    {
      var position = MediaPlayer.Position - GetSeekSize(seekSize);

      SetMediaPosition(position);
    }

    #endregion

    #region GetSeekSize

    private float GetSeekSize(int seconds)
    {
      return seconds * (float)100.0 / MediaPlayer.Length;
    }

    #endregion

    #endregion

    #region MediaPlayer_Buffering

    private void MediaPlayer_Buffering(object sender, PlayerBufferingEventArgs e)
    {
      if (e.Cache != 100)
        IsBuffering = true;
      else
        IsBuffering = false;
    }

    #endregion

    #region PlayList_CollectionChanged

    private void PlayList_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
      RaisePropertyChanged(nameof(TotalPlaylistDuration));
    }

    #endregion

    #region OnResumePlaying

    protected override void OnResumePlaying()
    {
      if (ActualItem != null && ItemLastTime != null)
      {
        MediaPlayer.Position = ((float)ItemLastTime / ActualItem.Duration);
      }
    }

    #endregion

    #region UpdateNonUserCreatedPlaylist

    protected override void UpdateNonUserCreatedPlaylist(TPlaylistModel playlistToUpdate, TPlaylistModel other)
    {
      base.UpdateNonUserCreatedPlaylist(playlistToUpdate, other);

      playlistToUpdate.Name = other.Name;
      playlistToUpdate.IsReapting = other.IsReapting;
      playlistToUpdate.IsShuffle = other.IsShuffle;
    }

    #endregion

    #region Media_DurationChanged

    protected void Media_DurationChanged(object sender, MediaDurationChangedArgs e)
    {
      Application.Current.Dispatcher.Invoke(() =>
      {
        ActualItem.Duration = (int)e.Duration / 1000;

        if (MediaPlayer.Media != null)
          MediaPlayer.Media.DurationChanged -= Media_DurationChanged;

      });
    }

    #endregion

    #region OnVlcPlayingChanged

    protected override void OnVlcPlayingChanged(object sender, EventArgs eventArgs)
    {
      lastTimeChangedMs = 0;

      base.OnVlcPlayingChanged(sender, eventArgs);
    }

    #endregion

    #region OnPlayPlaylist

    protected override void OnPlayPlaylist()
    {
      base.OnPlayPlaylist();

      HandleLastItemElapsed();
    }

    #endregion

    #region HandleLastItemElapsed

    private void HandleLastItemElapsed()
    {
      Application.Current.Dispatcher.Invoke(() =>
      {
        ItemLastTime = null;
        var itemLastTimeFloat = ActualSavedPlaylist.LastItemElapsedTime > 0 ? (float?)ActualSavedPlaylist.LastItemElapsedTime : null;

        if (itemLastTimeFloat != null && itemLastTimeFloat > 0)
          ItemLastTime = (int)(itemLastTimeFloat.Value * ActualItem.Duration);

      });
    }

    #endregion

    #region OnShuffle

    protected override void OnShuffle(bool value)
    {
      base.OnShuffle(value);

      if (ActualSavedPlaylist.IsShuffle != value)
      {
        ActualSavedPlaylist.IsShuffle = value;
        UpdateActualSavedPlaylistPlaylist();
      }
    }

    #endregion

    #region OnRepeate

    protected override void OnRepeate(bool value)
    {
      base.OnRepeate(value);

      if (ActualSavedPlaylist.IsReapting != value)
      {
        ActualSavedPlaylist.IsReapting = value;
        UpdateActualSavedPlaylistPlaylist();
      }
    }

    #endregion

    #region Dispose

    public override void Dispose()
    {
      base.Dispose();

      MediaPlayer.TimeChanged -= OnVlcTimeChanged;
      PlayList.CollectionChanged -= PlayList_CollectionChanged;
    }

    #endregion

   
    #endregion

  }
}