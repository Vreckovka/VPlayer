﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Logger;
using Ninject;
using Prism.Events;
using VCore;
using VCore.Helpers;
using VCore.ItemsCollections;
using VCore.ItemsCollections.VirtualList;
using VCore.ItemsCollections.VirtualList.VirtualLists;
using VCore.Modularity.RegionProviders;
using VCore.Standard.Helpers;
using VCore.Standard.Modularity.Interfaces;
using VCore.ViewModels;
using LibVLCSharp.Shared;
using VCore.Standard.ViewModels.TreeView;
using VCore.WPF.Behaviors;
using MediaPlayer = LibVLCSharp.Shared.MediaPlayer;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Events;
using VPlayer.Core.Providers;
using VPlayer.WindowsPlayer.Players;

namespace VPlayer.Core.ViewModels
{
  public abstract class PlayableRegionViewModel<TView, TItemViewModel, TPlaylistModel, TPlaylistItemModel, TModel> : RegionViewModel<TView>, IPlayableRegionViewModel, IHideable
    where TView : class, IView
    where TItemViewModel : class, IItemInPlayList<TModel>, ISelectable
    where TModel : IPlayableModel
    where TPlaylistModel : class, IPlaylist<TPlaylistItemModel>, new()
    where TPlaylistItemModel : IItemInPlaylist<TModel>
  {
   

    #region Fields

    protected readonly ILogger logger;
    protected readonly IStorageManager storageManager;
    protected int actualItemIndex;
    protected HashSet<TItemViewModel> shuffleList = new HashSet<TItemViewModel>();
    private bool wasVlcInitilized;


    #endregion

    #region Constructors

    public PlayableRegionViewModel(
      IRegionProvider regionProvider,
      IKernel kernel,
      ILogger logger,
      IStorageManager storageManager,
      IEventAggregator eventAggregator,
      VLCPlayer vLCPlayer) : base(regionProvider)
    {
      this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
      this.storageManager = storageManager ?? throw new ArgumentNullException(nameof(storageManager));
      EventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));

      Kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
      MediaPlayer = vLCPlayer;
    }

    #endregion

    #region Properties

    #region MediaPlayer

    private IPlayer mediaPlayer;
    public IPlayer MediaPlayer
    {
      get { return mediaPlayer; }
      set
      {
        if (value != mediaPlayer)
        {
          mediaPlayer = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region EventAgreggator

    protected IEventAggregator eventAggregator;

    public IEventAggregator EventAggregator
    {
      get { return eventAggregator; }
      set
      {
        if (value != eventAggregator)
        {
          eventAggregator = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region ActualItem

    private TItemViewModel actualItem;
    public TItemViewModel ActualItem
    {
      get { return actualItem; }
      protected set
      {
        if (value != actualItem)
        {
          if (actualItem != null)
          {
            ItemLastTime = null;
            actualItem.IsPlaying = false;
          }

          actualItem = value;

          if (actualItem != null)
          {
            actualItem.IsPlaying = true;
          }
          actualItemSubject.OnNext(PlayList.IndexOf(actualItem));

          OnActualItemChanged();
          RaisePropertyChanged();
        }
      }
    }

    #endregion

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
          OnIsPlayingChanged();
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region ActualSearch

    private ReplaySubject<string> actualSearchSubject;
    private string actualSearch;
    public string ActualSearch
    {
      get { return actualSearch; }
      set
      {
        if (value != actualSearch)
        {
          actualSearch = value;

          actualSearchSubject.OnNext(actualSearch);

          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region PlayList

    public RxObservableCollection<TItemViewModel> PlayList { get; } = new RxObservableCollection<TItemViewModel>();

    #endregion

    #region VirtualizedPlayList

    private VirtualList<TItemViewModel> virtualizedPlayList;
    public VirtualList<TItemViewModel> VirtualizedPlayList
    {
      get { return virtualizedPlayList; }
      protected set
      {
        if (value != virtualizedPlayList)
        {
          virtualizedPlayList = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region CanPlay

    public bool CanPlay
    {
      get { return PlayList.Count != 0; }
    }

    #endregion

    #region Kernel

    public IKernel Kernel { get; set; }

    #endregion

    #region ActualSavedPlaylist

    private TPlaylistModel actualSavedPlaylist = new TPlaylistModel() { Id = -1 };

    public TPlaylistModel ActualSavedPlaylist
    {
      get { return actualSavedPlaylist; }
      set
      {
        if (value != actualSavedPlaylist)
        {
          actualSavedPlaylist = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region IsPlayFnished

    public bool IsPlayFnished { get; private set; }

    #endregion

    #region ActualItemChanged

    private ReplaySubject<int> actualItemSubject = new ReplaySubject<int>(1);

    public IObservable<int> ActualItemChanged
    {
      get { return actualItemSubject.AsObservable(); }
    }

    #endregion
    
    #region IsSelectedToPlay

    private bool isSelectedToPlay;

    public bool IsSelectedToPlay
    {
      get { return isSelectedToPlay; }
      set
      {
        if (value != isSelectedToPlay)
        {
          isSelectedToPlay = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region PlaylistTotalTimePlayed

    private TimeSpan playlistTotalTimePlayed;

    public TimeSpan PlaylistTotalTimePlayed
    {
      get { return ActualSavedPlaylist.TotalPlayedTime; }
      set
      {
        if (value != ActualSavedPlaylist.TotalPlayedTime)
        {
          ActualSavedPlaylist.TotalPlayedTime = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region ItemLastTime

    private int? itemLastTime;
    public int? ItemLastTime
    {
      get { return itemLastTime; }
      set
      {
        if (value != itemLastTime)
        {
          itemLastTime = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #endregion

    #region Commands

    #region ExpandHidePlaylist

    public event EventHandler<bool> Hide;

    private ActionCommand expandHidePlaylist;
    public ICommand ExpandHidePlaylist
    {
      get
      {
        if (expandHidePlaylist == null)
        {
          expandHidePlaylist = new ActionCommand(OnExpandHidePlaylist);
        }

        return expandHidePlaylist;
      }
    }

    public void OnExpandHidePlaylist()
    {
      OnHidePlaylist(false);
    }

    #endregion

    #region SavePlaylistCommand

    private ActionCommand savePlaylistCommand;

    public ICommand SavePlaylistCommand
    {
      get
      {
        if (savePlaylistCommand == null)
        {
          savePlaylistCommand = new ActionCommand(OnSavePlaylist);
        }

        return savePlaylistCommand;
      }
    }

    public void OnSavePlaylist()
    {
      if (!ActualSavedPlaylist.IsUserCreated && !StorePlaylist(true))
      {
        ActualSavedPlaylist.IsUserCreated = true;
        UpdateActualSavedPlaylistPlaylist();
        RaisePropertyChanged(nameof(ActualSavedPlaylist));
      }
    }

    #endregion

    #region ResumePlaying

    private ActionCommand resumePlaying;

    public ICommand ResumePlaying
    {
      get
      {
        if (resumePlaying == null)
        {
          resumePlaying = new ActionCommand(OnResumePlaying);
        }

        return resumePlaying;
      }
    }

    protected virtual void OnResumePlaying()
    {
    }

    #endregion

    #endregion

    #region Methods

    #region Initialize

    public override void Initialize()
    {
      InitializeAsync();
    }

    #endregion

    #region InitializeAsync

    protected virtual async Task InitializeAsync()
    {
      IsPlaying = false;

      base.Initialize();

      await HookToPlayerEvents();

      actualSearchSubject = new ReplaySubject<string>(1).DisposeWith(this);

      PlayList.DisposeWith(this);

      actualSearchSubject.Throttle(TimeSpan.FromMilliseconds(150)).Subscribe(FilterByActualSearch).DisposeWith(this);

      HookToPubSubEvents();
    }

    #endregion

    #region LoadVlc

    protected async Task LoadVlc()
    {
      await MediaPlayer.Initilize();
    }

    #endregion

    #region HookToVlcEvents

    protected virtual async Task HookToPlayerEvents()
    {
      await LoadVlc();

      if (MediaPlayer == null)
      {
        logger.Log(Logger.MessageType.Error, "VLC was not initlized!");
        return;
      }

      MediaPlayer.EncounteredError += (sender, e) =>
      {
        OnVlcError();
      };


      MediaPlayer.EndReached += (sender, e) => { OnEndReached(); };

      MediaPlayer.Paused += (sender, e) =>
      {
        if (ActualItem != null)
        {
          ActualItem.IsPaused = true;
        }
      };

      MediaPlayer.Stopped += (sender, e) =>
      {
        OnMediaPlayerStopped();
      };

      MediaPlayer.Playing += OnVlcPlayingChanged;

      OnVlcLoaded();
    }


    #endregion

    #region OnMediaPlayerStopped

    protected virtual void OnMediaPlayerStopped()
    {
      if (IsPlayFnished && ActualItem != null)
      {
        ActualItem.IsPlaying = false;
        ActualItem.IsPaused = false;
        IsPlaying = false;
      }
    }

    #endregion

    #region HookToPubSubEvents

    protected virtual void HookToPubSubEvents()
    {
      eventAggregator.GetEvent<RemoveFromPlaylistEvent<TItemViewModel>>().Subscribe(RemoveItemsFromPlaylist).DisposeWith(this);
      eventAggregator.GetEvent<PlaySongsFromPlayListEvent<TItemViewModel>>().Subscribe(PlayItemFromPlayList).DisposeWith(this);
      eventAggregator.GetEvent<PlayItemsEvent<TModel, TItemViewModel>>().Subscribe(PlayItemsFromEvent).DisposeWith(this);

    }

    #endregion

    #region SetActualItem

    protected void SetActualItem(int index)
    {
      try
      {
        if (actualItemIndex < PlayList.Count && actualItemIndex >= 0)
        {
          if (ActualItem != null)
          {
            ActualItem.IsPlaying = false;
            ActualItem.IsPaused = false;

            OnSetActualItem(ActualItem, false);
          }

          ActualItem = PlayList[index];
          //actualItemIndex = index;
          ActualItem.IsPlaying = true;

          OnSetActualItem(ActualItem, true);

          if (ActualSavedPlaylist != null)
          {
            ActualSavedPlaylist.LastItemIndex = PlayList.IndexOf(ActualItem);
            UpdateActualSavedPlaylistPlaylist();
          }

          shuffleList.Add(ActualItem);
        }
        else
        {
          if (ActualItem != null)
          {
            ActualItem.IsPlaying = false;
            ActualItem.IsPaused = false;
            OnSetActualItem(ActualItem, false);
          }

          ActualItem = null;
        }
      }
      catch (Exception ex)
      {
        logger.Log(ex);
      }
    }

    #endregion

    #region SetItemAndPlay

    public virtual void SetItemAndPlay(int? songIndex = null, bool forcePlay = false, bool onlyItemSet = false)
    {
      Application.Current?.Dispatcher?.Invoke(async () =>
      {
        if (!string.IsNullOrEmpty(actualSearch))
        {
          ActualSearch = null;
        }

        IsPlayFnished = false;

        if (songIndex == null)
        {
          actualItemIndex++;
        }
        else
        {
          actualItemIndex = songIndex.Value;
        }

        if (actualItemIndex >= PlayList.Count)
        {
          IsPlayFnished = true;
          Pause();
          return;
        }

        SetActualItem(actualItemIndex);

        if (ActualItem == null)
          return;

        await SetMedia(ActualItem.Model);

        if (IsPlaying || forcePlay)
        {
          if (!onlyItemSet)
            Play();
        }
        else if (!IsPlaying && songIndex != null)
        {
          if (!onlyItemSet)
            Play();
        }
        else if (ActualItem != null)
          ActualItem.IsPaused = true;
      });
    }

    #endregion 

    protected virtual void OnVlcError()
    {
      Application.Current.Dispatcher.Invoke(() =>
      {
        MessageBox.Show("VLC BROKE!!!!!");
      });
    }

    #region OnEndReached

    protected virtual void OnEndReached()
    {
      Task.Run(() => PlayNextWithItem());
    }

    #endregion

    #region PlayNextWithItem

    public void PlayNextWithItem(TItemViewModel nextItem = null)
    {
      if (nextItem == null)
      {
        SetItemAndPlay();
      }
      else
      {
        var item = PlayList.SingleOrDefault(x => x == nextItem);

        if (ActualItem != item && item != null)
        {
          SetItemAndPlay(PlayList.IndexOf(item));
        }
      }
    }

    #endregion

    #region SetMedia

    protected virtual Task SetMedia(TModel model)
    {
      return Task.Run(async () =>
      {
        if (model.Source != null)
        {
          var fileUri = new Uri(model.Source);

          await MediaPlayer.SetNewMedia(fileUri);

          OnNewItemPlay();
        }

      });
    }

    #endregion

    #region VlcMethods

    #region OnVlcPlayingChanged

    protected virtual void OnVlcPlayingChanged(object sender, EventArgs eventArgs)
    {
      if (ActualItem != null)
      {
        ActualItem.IsPlaying = true;
        ActualItem.IsPaused = false;
        IsPlaying = true;
      }
    }

    #endregion

    #endregion

    #region Play methods

    #region Play

    public virtual Task Play()
    {
      return Task.Run(async () =>
      {
        if (!wasVlcInitilized)
          await WaitForVlcInitilization();

        if (IsPlayFnished)
        {
          SetItemAndPlay(0, true);
        }
        else
        {
          if (ActualItem != null && MediaPlayer.Media != null)
          {
            MediaPlayer.Play();
          }
        }
      });
    }

    #endregion

    #region PlayPlaylist

    private void PlayPlaylist(PlayItemsEventData<TItemViewModel> data, int? lastSongIndex = null)
    {
      ActualSavedPlaylist = data.GetModel<TPlaylistModel>();
      ActualSavedPlaylist.LastPlayed = DateTime.Now;

      if (lastSongIndex == null)
      {
        PlayItems(data.Items, false);
      }
      else
      {
        PlayItems(data.Items, false, lastSongIndex.Value);

        if (data.SetPostion.HasValue)
          MediaPlayer.Position = data.SetPostion.Value;
      }

      OnPlayPlaylist();
    }

    #endregion

    #region PlayPuse

    public void PlayPause()
    {
      Task.Run(() =>
      {
        if (IsPlaying)
          Pause();
        else
          Play();
      });
    }

    #endregion

    #region PlayPrevious

    public void PlayPrevious()
    {
      actualItemIndex--;

      if (actualItemIndex < 0)
      {
        actualItemIndex = 0;
      }

      SetItemAndPlay(actualItemIndex);
    }

    #endregion

    #region PlayNext

    public virtual void PlayNext()
    {
      actualItemIndex++;

      SetItemAndPlay(actualItemIndex);
    }

    #endregion

    #region Pause

    public void Pause()
    {
      if (IsPlaying)
      {
        MediaPlayer.Pause();
        IsPlaying = false;
      }
    }

    #endregion

    #region PlayItems

    protected void PlayItems(IEnumerable<TItemViewModel> songs, bool savePlaylist = true, int songIndex = 0, bool editSaved = false, bool onlyItemSet = false)
    {
      Application.Current.Dispatcher.Invoke(() =>
      {
        IsActive = true;

        PlayList.Clear();
        PlayList.AddRange(songs);
        ReloadVirtulizedPlaylist();

        IsPlaying = true;

        SetItemAndPlay(songIndex, onlyItemSet: onlyItemSet);

        Task.Run(() =>
        {
          if (savePlaylist)
          {
            StorePlaylist(editSaved: editSaved);
          }
        });

      });
    }



    #endregion

    #region PlayItemsFromEvent

    protected void PlayItemsFromEvent(PlayItemsEventData<TItemViewModel> data)
    {
      if (!data.Items.Any())
        return;

      if (ActualSavedPlaylist.Id > 0)
      {
        UpdateActualSavedPlaylistPlaylist();
        ActualSavedPlaylist = new TPlaylistModel() { Id = -1 };
      }

      switch (data.EventAction)
      {
        case EventAction.Play:
          PlayItems(data.Items, data.StorePlaylist, onlyItemSet: data.SetItemOnly);

          break;
        case EventAction.Add:
          PlayList.AddRange(data.Items);

          if (ActualItem == null)
          {
            SetActualItem(0);
          }

          ReloadVirtulizedPlaylist();
          RaisePropertyChanged(nameof(CanPlay));
          StorePlaylist(editSaved: true);
          break;
        case EventAction.PlayFromPlaylist:
          PlayPlaylist(data);

          break;
        case EventAction.PlayFromPlaylistLast:
          {
            var model = data.GetModel<TPlaylistModel>();
            PlayPlaylist(data, model.LastItemIndex);
          }

          break;
        default:
          throw new ArgumentOutOfRangeException();
      }

      RaisePropertyChanged(nameof(CanPlay));

      OnPlayEvent(data);
    }

    #endregion

    #endregion

    #region Playlist methods

    #region StorePlaylist

    public bool StorePlaylist(bool isUserCreated = false, bool editSaved = false)
    {
      var playlistModels = new List<TPlaylistItemModel>();

      for (int i = 0; i < PlayList.Count; i++)
      {
        var song = PlayList[i];

        var newItem = GetNewPlaylistItemViewModel(song, i);

        if (newItem == null)
        {
          return false;
        }

        playlistModels.Add(newItem);
      }

      var songIds = PlayList.Select(x => x.Model.Id);

      var hashCode = songIds.GetSequenceHashCode();

      var entityPlayList = GetNewPlaylistModel(playlistModels, isUserCreated);

      if (entityPlayList == null)
      {
        return false;
      }

      entityPlayList.HashCode = hashCode;

      bool success = false;

      var storedPlaylist = storageManager.GetRepository<TPlaylistModel>().Where(x => !x.IsUserCreated).SingleOrDefault(x => x.HashCode == hashCode);

      if (storedPlaylist == null)
      {
        if (editSaved && ActualSavedPlaylist.IsUserCreated)
        {
          if (hashCode != ActualSavedPlaylist.HashCode)
          {
            ActualSavedPlaylist.HashCode = hashCode;

            foreach (var itemInPlaylist in playlistModels)
            {
              itemInPlaylist.Id = ActualSavedPlaylist.PlaylistItems.Single(x => x.IdReferencedItem == itemInPlaylist.IdReferencedItem).Id;
            }

            ActualSavedPlaylist.PlaylistItems = playlistModels;
            ActualSavedPlaylist.ItemCount = playlistModels.Count;
          }
        }
        else if (!ActualSavedPlaylist.IsUserCreated)
        {
          if (ActualSavedPlaylist.Id <= 0)
          {
            success = storageManager.StoreEntity(entityPlayList, out var dbEntityPlalist);

            UpdateNonUserCreatedPlaylist(entityPlayList, dbEntityPlalist);

            ActualSavedPlaylist = entityPlayList;
          }
          else
          {
            ActualSavedPlaylist = entityPlayList;
            UpdateActualSavedPlaylistPlaylist();
          }
        }
      }
      else
      {
        if (storedPlaylist.IsUserCreated)
        {
          success = storageManager.StoreEntity(entityPlayList, out var dbEntityPlalist);

          UpdateNonUserCreatedPlaylist(entityPlayList, dbEntityPlalist);

          ActualSavedPlaylist = entityPlayList;

          ActualSavedPlaylist.LastPlayed = DateTime.Now;
        }
        else
        {
          storedPlaylist.Update(entityPlayList);

          if (storageManager.UpdatePlaylist<TPlaylistModel, TPlaylistItemModel>(storedPlaylist, out var updated))
          {
            ActualSavedPlaylist = updated;
          }
        }
      }

      if (ActualSavedPlaylist != null)
      {
        ActualSavedPlaylist.LastPlayed = DateTime.Now;
      }

      UpdateActualSavedPlaylistPlaylist();

      return success;
    }

    #endregion

    #region UpdateNonUserCreatedPlaylist

    protected virtual void UpdateNonUserCreatedPlaylist(TPlaylistModel playlistToUpdate, TPlaylistModel other)
    {
      if (playlistToUpdate.Id == 0)
        playlistToUpdate.Id = other.Id;

      playlistToUpdate.IsUserCreated = other.IsUserCreated;
    }

    #endregion

    #region UpdateActualSavedPlaylistPlaylist

    protected void UpdateActualSavedPlaylistPlaylist()
    {
      if (storageManager.UpdatePlaylist<TPlaylistModel, TPlaylistItemModel>(ActualSavedPlaylist, out var updated))
      {
        ActualSavedPlaylist = updated;
      }
    }

    #endregion

    #endregion

    #region IsInFind

    protected bool IsInFind(string original, string phrase, bool useContains = true)
    {
      bool result = false;

      if (original != null)
      {
        var lowerVariant = original.ToLower();

        if (useContains)
        {
          result = lowerVariant.Contains(phrase);
        }

        return original.Similarity(phrase) > 0.8 || result;
      }

      return result;
    }

    #endregion

    #region ReloadVirtulizedPlaylist

    protected void ReloadVirtulizedPlaylist()
    {
      var generator = new ItemsGenerator<TItemViewModel>(PlayList, 15);
      VirtualizedPlayList = new VirtualList<TItemViewModel>(generator);
    }

    #endregion

    #region RemoveItemsFromPlaylist

    protected void RemoveItemsFromPlaylist(RemoveFromPlaylistEventArgs<TItemViewModel> obj)
    {
      switch (obj.DeleteType)
      {
        case DeleteType.SingleFromPlaylist:
          foreach (var songToDelete in obj.ItemsToRemove)
          {
            var songInPlaylist = PlayList.SingleOrDefault(x => x == songToDelete);

            if (songInPlaylist != null)
            {
              PlayList.Remove(songInPlaylist);
            }
          }

          StorePlaylist(editSaved: true);

          break;
        case DeleteType.AlbumFromPlaylist:
          OnRemoveItemsFromPlaylist(DeleteType.AlbumFromPlaylist, obj);

          break;
        default:
          throw new ArgumentOutOfRangeException();
      }


      ReloadVirtulizedPlaylist();

      if (obj.ItemsToRemove.Count(x => x.Model.Id == ActualItem.Model.Id) > 0)
      {
        ActualItem = null;
      }

      SetItemAndPlay(actualItemIndex);
    }

    #endregion

    #region PlayItemFromPlayList

    protected void PlayItemFromPlayList(TItemViewModel viewModel)
    {
      if (viewModel == ActualItem)
      {
        if (!ActualItem.IsPaused)
          PlayPause();
        else
          Play();
      }
      else
      {
        PlayNextWithItem(viewModel);
      }
    }

    #endregion

    #region SetVolume

    public void SetVolume(int pVolume)
    {
      if (MediaPlayer != null)
      {
        MediaPlayer.Volume = pVolume;
      }
    }

    #endregion

    //Virtual methods 
    #region Virtual methods

    #region OnNewItemPlay

    public virtual void OnNewItemPlay()
    {
    }

    #endregion

    #region OnSetActualItem

    public virtual void OnSetActualItem(TItemViewModel itemViewModel, bool isPlaying)
    {
    }

    #endregion

    #region OnPlayEvent

    protected virtual void OnPlayEvent(PlayItemsEventData<TItemViewModel> data)
    {

    }

    #endregion

    #region WaitForInitilization

    protected virtual Task WaitForVlcInitilization()
    {
      return Task.Run(() => { wasVlcInitilized = true; });
    }

    #endregion

    #region OnVlcLoaded

    protected virtual void OnVlcLoaded()
    {

    }

    #endregion

    #region OnHidePlaylist

    protected virtual void OnHidePlaylist(bool e)
    {
      Hide?.Invoke(this, e);
    }

    #endregion

    #region OnActualItemChanged

    protected virtual void OnActualItemChanged()
    {

    }

    #endregion

    protected virtual void OnPlayPlaylist()
    {

    }

    protected virtual void OnIsPlayingChanged()
    {

    }

    #endregion

    //Abstract methods 
    #region Abstract methods

    protected abstract TPlaylistItemModel GetNewPlaylistItemViewModel(TItemViewModel itemViewModel, int index);
    protected abstract void OnRemoveItemsFromPlaylist(DeleteType deleteType, RemoveFromPlaylistEventArgs<TItemViewModel> args);
    protected abstract void ItemsRemoved(EventPattern<TItemViewModel> eventPattern);
    protected abstract void FilterByActualSearch(string predictate);
    protected abstract TPlaylistModel GetNewPlaylistModel(List<TPlaylistItemModel> playlistModels, bool isUserCreated);

    #endregion

    #region Dispose

    public override void Dispose()
    {
      if (ActualSavedPlaylist != null)
        UpdateActualSavedPlaylistPlaylist();

      Task.Run(() =>
      {

        MediaPlayer.Playing -= OnVlcPlayingChanged;

        MediaPlayer.Dispose();



        base.Dispose();
      });
    }

    #endregion

    #endregion


  }
}