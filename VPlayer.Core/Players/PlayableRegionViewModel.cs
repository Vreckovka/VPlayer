using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
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
using VCore.ItemsCollections;
using VCore.Standard.Helpers;
using VCore.Standard.Modularity.Interfaces;
using LibVLCSharp.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic.FileIO;
using SoundManagement;
using VCore.Standard;
using VCore.Standard.Factories.ViewModels;
using VCore.Standard.ViewModels.TreeView;
using VCore.Standard.ViewModels.WindowsFile;
using VCore.WPF;
using VCore.WPF.Behaviors;
using VCore.WPF.Helpers;
using VCore.WPF.Interfaces.Managers;
using VCore.WPF.ItemsCollections.VirtualList;
using VCore.WPF.ItemsCollections.VirtualList.VirtualLists;
using VCore.WPF.Managers;
using VCore.WPF.Misc;
using VCore.WPF.Modularity.RegionProviders;
using VCore.WPF.ViewModels;
using VCore.WPF.ViewModels.Prompt;
using MediaPlayer = LibVLCSharp.Shared.MediaPlayer;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Events;
using VPlayer.Core.Managers.Status;
using VPlayer.Core.ViewModels.TvShows;
using VPlayer.WindowsPlayer.Players;
using VVLC.Players;

namespace VPlayer.Core.ViewModels
{
  public enum PlaylistSortOrder
  {
    None,
    Name,
    Created,
    Modified,
    ItemProperties,
  }

  public abstract class PlayableRegionViewModel<TView, TItemViewModel, TPlaylistModel, TPlaylistItemModel, TModel> : RegionViewModel<TView>, IPlayableRegionViewModel, IHideable
    where TView : class, IView
    where TItemViewModel : class, IItemInPlayList<TModel>, ISelectable, IDisposable
    where TModel : class, IPlayableModel, IEntity, IUpdateable<TModel>
    where TPlaylistModel : class, IPlaylist<TPlaylistItemModel>, new()
    where TPlaylistItemModel : class, IItemInPlaylist<TModel>
  {

    #region Fields

    protected readonly ILogger logger;
    protected readonly IStorageManager storageManager;
    private readonly IStatusManager statusManager;
    protected readonly IViewModelsFactory viewModelsFactory;
    protected readonly IWindowManager windowManager;
    protected int actualItemIndex;
    protected HashSet<TItemViewModel> shuffleList = new HashSet<TItemViewModel>();
    private bool wasVlcInitilized;
    private Subject<int> volumeSubject = new Subject<int>();

    #endregion

    #region Constructors

    public PlayableRegionViewModel(
      IRegionProvider regionProvider,
      IKernel kernel,
      ILogger logger,
      IStorageManager storageManager,
      IEventAggregator eventAggregator,
      IStatusManager statusManager,
      IViewModelsFactory viewModelsFactory,
      IWindowManager windowManager,
      VLCPlayer vLCPlayer) : base(regionProvider)
    {
      this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
      this.storageManager = storageManager ?? throw new ArgumentNullException(nameof(storageManager));
      this.statusManager = statusManager ?? throw new ArgumentNullException(nameof(statusManager));
      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));
      this.windowManager = windowManager ?? throw new ArgumentNullException(nameof(windowManager));
      EventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));

      Kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
      MediaPlayer = vLCPlayer;

      shuffleRandomSeed = new Random().Next(0, int.MaxValue);

      shuffleRandom = new Random(shuffleRandomSeed);
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

    #region ActualItemIndex

    public int ActualItemIndex
    {
      get
      {
        return actualItemIndex;
      }
    }

    #endregion

    #region ActualPlaylistSortOrder

    private PlaylistSortOrder actualPlaylistSortOrder;

    public PlaylistSortOrder ActualPlaylistSortOrder
    {
      get { return actualPlaylistSortOrder; }
      set
      {
        if (value != actualPlaylistSortOrder)
        {
          actualPlaylistSortOrder = value;
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
            LastTime = null;
            LastTimeMs = null;
            actualItem.IsPlaying = false;
          }

          actualItem = value;

          if (actualItem != null)
          {
            actualItem.IsPlaying = true;
          }
          actualItemSubject.OnNext(PlayList.IndexOf(actualItem));

          RaisePropertyChanged();
          RaisePropertyChanged(nameof(ActualItemIndex));
          OnActualItemChanged();
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

          if (string.IsNullOrEmpty(actualSearch))
          {
            PlaylistFromSearch = false;
          }

          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region PlaylistFromSearch

    private bool playlistFromSearch;

    public bool PlaylistFromSearch
    {
      get { return playlistFromSearch; }
      set
      {
        if (value != playlistFromSearch)
        {
          playlistFromSearch = value;

          OnPlaylistFromSearch(playlistFromSearch);
          RaisePropertyChanged();
        }
      }
    }

    private RxObservableCollection<TItemViewModel> playlistCopy;
    private int savedActualItemIndex;
    private void OnPlaylistFromSearch(bool value)
    {
      if (value)
      {
        playlistCopy = new RxObservableCollection<TItemViewModel>();

        foreach (var item in PlayList)
        {
          playlistCopy.Add(item);
        }

        var newPlaylist = new RxObservableCollection<TItemViewModel>();

        foreach (var item in VirtualizedPlayList.Generator.AllItems)
        {
          newPlaylist.Add(item);
        }

        savedActualItemIndex = actualItemIndex;
        PlayList = newPlaylist;
        actualItemIndex = 0;
      }
      else
      {
        PlayList = playlistCopy;
        actualItemIndex = savedActualItemIndex;
        playlistCopy = new RxObservableCollection<TItemViewModel>();
      }
    }

    #endregion

    #region PlayList

    private RxObservableCollection<TItemViewModel> playList = new RxObservableCollection<TItemViewModel>();

    public RxObservableCollection<TItemViewModel> PlayList
    {
      get { return playList; }
      set
      {
        if (value != playList)
        {
          playList = value;
          RaisePropertyChanged();
        }
      }
    }


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

    public bool IsPlayFnished { get; protected set; }

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

          RaisePropertyChanged(nameof(Volume));
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

    #region LastTime

    private float? lastTime;
    public float? LastTime
    {
      get { return lastTime; }
      set
      {
        if (value != lastTime)
        {
          lastTime = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region LastTimeMs

    private float? lastTimeMs;
    public float? LastTimeMs
    {
      get { return lastTimeMs; }
      set
      {
        if (value != lastTimeMs)
        {
          lastTimeMs = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region IsRepeate

    private bool isRepeate = true;
    public bool IsRepeate
    {
      get { return isRepeate; }
      set
      {
        if (value != isRepeate)
        {
          isRepeate = value;

          OnRepeate(value);

          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region IsShuffle

    private bool isShuffle;

    private int shuffleRandomSeed;
    protected Random shuffleRandom;

    public bool IsShuffle
    {
      get { return isShuffle; }
      set
      {
        if (value != isShuffle)
        {
          isShuffle = value;

          OnShuffle(value);

          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region PlayNextItemOnEndReached

    public bool PlayNextItemOnEndReached { get; set; } = true;

    #endregion

    #region Volume

    //get MediaPlayer.Volume is freezing UI thread
    private int volume;
    public int Volume
    {
      get { return volume; }
      set
      {
        if (value != volume)
        {
          MediaPlayer.Volume = value;
          volume = value;

          volumeSubject.OnNext(volume);
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region OnVolumeChanged

    public IObservable<int> OnVolumeChanged
    {
      get
      {
        return volumeSubject.AsObservable();
      }
    }

    #endregion

    #region IsMuted

    public bool IsMuted
    {
      get { return MediaPlayer.IsMuted; }
    }

    #endregion

    #region SortDescending

    private bool isSortDescending;

    public bool IsSortDescending
    {
      get { return isSortDescending; }
      set
      {
        if (value != isSortDescending)
        {
          isSortDescending = value;
          UpdatePlaylist();


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

    #region ChangePlaylistFavorite

    private ActionCommand changePlaylistFavorite;

    public ICommand ChangePlaylistFavorite
    {
      get
      {
        if (changePlaylistFavorite == null)
        {
          changePlaylistFavorite = new ActionCommand(OnChangePlaylistFavorite);
        }

        return changePlaylistFavorite;
      }
    }

    public void OnChangePlaylistFavorite()
    {
      ActualSavedPlaylist.IsUserCreated = !ActualSavedPlaylist.IsUserCreated;

      UpdateOrAddActualSavedPlaylist();
    }

    #endregion

    #region ResetAllData

    private ActionCommand resetAllData;

    public ICommand ResetAllData
    {
      get
      {
        if (resetAllData == null)
        {
          resetAllData = new ActionCommand(async () => await OnResetAllData());
        }

        return resetAllData;
      }
    }

    public virtual async Task<bool> OnResetAllData()
    {
      if (windowManager.OkCancel("Do you really want to refresh all data?", cancelVisibility: Visibility.Visible) == PromptResult.Ok)
      {
        PlayList.ForEach(x => x.OnResetAllData());

        await SaveData(PlayList.ToList());
        await DownloadInfos(PlayList.ToList());

        return true;
      }

      return false;
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
      UpdatePlaylist();
    }

    #endregion

    #region ClearPlaylistCommand

    private ActionCommand clearPlaylistCommand;

    public ICommand ClearPlaylistCommand
    {
      get
      {
        if (clearPlaylistCommand == null)
        {
          clearPlaylistCommand = new ActionCommand(OnClearPlaylistCallback);
        }

        return clearPlaylistCommand;
      }
    }

    private async void OnClearPlaylistCallback()
    {
      await ClearPlaylist();
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

    #region ToggleMute

    private ActionCommand toggleMute;
    public ICommand ToggleMute
    {
      get
      {
        if (toggleMute == null)
        {
          toggleMute = new ActionCommand(OnToggleMute);
        }

        return toggleMute;
      }
    }

    public void OnToggleMute()
    {
      MediaPlayer.ToggleMute();
    }

    #endregion

    #region ChangePlaylistOrder

    private ActionCommand<PlaylistSortOrder> changePlaylistOrder;
    public ICommand ChangePlaylistOrder
    {
      get
      {
        if (changePlaylistOrder == null)
        {
          changePlaylistOrder = new ActionCommand<PlaylistSortOrder>(OnChangePlaylistOrder);
        }

        return changePlaylistOrder;
      }
    }

    private List<int> defaultSortOrder;
    protected virtual void SortPlaylist(PlaylistSortOrder playlistSort)
    {
      if (ActualPlaylistSortOrder == PlaylistSortOrder.None && defaultSortOrder == null)
      {
        defaultSortOrder = PlayList.Select(x => x.Model.Id).ToList();
      }

      ActualPlaylistSortOrder = playlistSort;

      switch (playlistSort)
      {
        case PlaylistSortOrder.None:
          PlayList = new RxObservableCollection<TItemViewModel>(PlayList.OrderBy(d => defaultSortOrder.IndexOf(d.Model.Id)).ToList());
          break;
        case PlaylistSortOrder.Name:
          PlayList.Sort((x, y) => x.Name.CompareTo(y.Name));
          break;
      }
    }

    protected void OnChangePlaylistOrder(PlaylistSortOrder playlistSort)
    {
      SortPlaylist(playlistSort);

      if (IsSortDescending)
      {
        PlayList = new RxObservableCollection<TItemViewModel>(PlayList.OrderByDescending(d => PlayList.IndexOf(d)).ToList());
      }

      RequestReloadVirtulizedPlaylist();
      StorePlaylist(PlayList.Select(x => x).ToList(), editSaved: true);

      actualItemIndex = PlayList.IndexOf(ActualItem);
      actualItemSubject.OnNext(actualItemIndex);
    }

    #endregion

    #region SetPlaylistPrivate

    private ActionCommand setPlaylistPrivate;

    public ICommand SetPlaylistPrivate
    {
      get
      {
        if (setPlaylistPrivate == null)
        {
          setPlaylistPrivate = new ActionCommand(OnSetPlaylistPrivate);
        }

        return setPlaylistPrivate;
      }
    }


    private async void OnSetPlaylistPrivate()
    {
      ActualSavedPlaylist.IsPrivate = !ActualSavedPlaylist.IsPrivate;

      foreach (var item in PlayList)
      {
        item.OnSetPrivate(ActualSavedPlaylist.IsPrivate);
      }

      RaisePropertyChanged(nameof(ActualSavedPlaylist));

      await UpdateActualSavedPlaylistPlaylist();
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

    IDisposable clearPlaylistDisposable;
    protected virtual void InitializeAsync()
    {
      IsPlaying = false;

      base.Initialize();

      HookToPlayerEvents();

      actualSearchSubject = new ReplaySubject<string>(1).DisposeWith(this);

      PlayList.DisposeWith(this);

      PlayList.ItemAdded.ObserveOnDispatcher().Subscribe((x) =>
      {
        x.EventArgs.IsInPlaylist = true;
      }).DisposeWith(this);

      clearPlaylistDisposable = PlayList.Cleared.Subscribe(async (x) => { await ResetProperties(); });

      PlayList.ItemRemoved.ObserveOnDispatcher().Subscribe((x) =>
      {
        x.EventArgs.IsInPlaylist = false;

        if (PlaylistFromSearch)
          playlistCopy?.Remove(x.EventArgs);

      }).DisposeWith(this);

      actualSearchSubject.Throttle(TimeSpan.FromMilliseconds(250)).Subscribe(FilterByActualSearch).DisposeWith(this);

      HookToPubSubEvents();

      Volume = MediaPlayer.Volume;

      AudioDeviceManager.Instance.ObservePropertyChange(x => x.SelectedSoundDevice).ObserveOnDispatcher().Subscribe(async x =>
      {
        await Task.Delay(500);

        volumeSubject.OnNext(Volume);
        RaisePropertyChanged(nameof(Volume));
      });
    }

    #endregion

    #region HookToVlcEvents

    protected virtual void HookToPlayerEvents()
    {
      MediaPlayer?.Initilize();

      if (MediaPlayer == null)
      {
        logger.Log(Logger.MessageType.Error, "VLC was not initlized!");
        return;
      }

      MediaPlayer.EncounteredError += (sender, e) =>
      {
        OnVlcError();
      };


      MediaPlayer.Paused += (sender, e) =>
      {
        if (ActualItem != null)
        {
          ActualItem.IsPaused = true;
        }

        OnPaused();
      };


      MediaPlayer.Stopped += (sender, e) =>
      {
        OnMediaPlayerStopped();
      };

      MediaPlayer.Muted += MediaPlayer_MutedChanged;
      MediaPlayer.Unmuted += MediaPlayer_MutedChanged;

      MediaPlayer.Playing += OnVlcPlayingChanged;

      Volume = MediaPlayer.Volume;
      volumeSubject.OnNext(Volume);

      OnVlcLoaded();
    }

    protected virtual void OnPaused()
    {

    }

    private void MediaPlayer_MutedChanged(object sender, EventArgs e)
    {
      RaisePropertyChanged(nameof(IsMuted));
    }


    #endregion

    #region Clear

    public virtual async Task ClearPlaylist()
    {
      if (ActualSavedPlaylist.Id > 0)
      {
        await UpdateActualSavedPlaylistPlaylist();
      }

      await ResetProperties();

      PlayList.Clear();
    }

    #endregion

    #region ResetProperties

    protected virtual async Task ResetProperties()
    {
      BeforeClearPlaylist();

      PlayList.ForEach(x => x.IsInPlaylist = false);

      IsPlaying = false;
      VirtualizedPlayList = null;

      PlayList?.OfType<IDisposable>().ForEach(x => x.Dispose());
      ActualItem?.Dispose();
      ActualItem = null;
      MediaPlayer.Stop();
      await SetMedia(null);
      MediaPlayer.Play();
      actualItemIndex = 0;
      PlaylistTotalTimePlayed = new TimeSpan(0);
      ActualSavedPlaylist = new TPlaylistModel() { Id = -1 };
      shuffleList.Clear();
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
      eventAggregator.GetEvent<DownloadInfoEvent<TItemViewModel>>().Subscribe(OnDownloadInfoEvent).DisposeWith(this);
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
          actualItemIndex = index;
          ActualItem.IsPlaying = true;


          OnSetActualItem(ActualItem, true);

          if (ActualSavedPlaylist?.PlaylistItems != null)
          {
            ActualSavedPlaylist.LastItemIndex = PlayList.IndexOf(ActualItem);

            TPlaylistItemModel playlistItem = default(TPlaylistItemModel);
            if (ActualSavedPlaylist.PlaylistItems.Count > actualItemIndex)
            {
              playlistItem = ActualSavedPlaylist.PlaylistItems.OrderBy(x => x.OrderInPlaylist).ToList()[actualItemIndex];
            }

            ActualSavedPlaylist.ActualItem = playlistItem;
            ActualSavedPlaylist.ActualItemId = playlistItem?.Id;

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

    public virtual async void SetItemAndPlay(int? itemIndex = null, bool forcePlay = false, bool onlyItemSet = false)
    {


      if (IsShuffle && itemIndex == null)
      {
        var result = PlayList.Where(p => shuffleList.All(p2 => p2 != p)).ToList();

        if (result.Count == 0)
        {
          shuffleList.Clear();
          result = PlayList.Where(p => shuffleList.All(p2 => p2 != p)).ToList();
        }

        var shuffleIndex = (int)Math.Floor(shuffleRandom.NextDouble() * result.Count);

        itemIndex = PlayList.IndexOf(result[shuffleIndex]);
      }


      if (IsRepeate && actualItemIndex > PlayList.Count - 1)
      {
        actualItemIndex = 0;
        itemIndex = 0;
      }


      IsPlayFnished = false;

      if (itemIndex == null)
      {
        actualItemIndex++;
      }
      else
      {
        actualItemIndex = itemIndex.Value;
      }

      if (actualItemIndex >= PlayList.Count)
      {
        if (!IsRepeate)
        {

          IsPlayFnished = true;


          Pause();
          return;
        }
        else if (actualItemIndex > PlayList.Count - 1)
        {
          actualItemIndex = 0;
          itemIndex = 0;
        }
      }


      SetActualItem(actualItemIndex);


      if (ActualItem == null)
        return;

      var oldPlaying = IsPlaying;

      await SetMedia(ActualItem.Model);


      ActualItem.IsPlaying = true;


      if (oldPlaying || forcePlay)
      {
        if (!onlyItemSet)
          await Play();
      }
      else if (!IsPlaying && itemIndex != null)
      {
        if (!onlyItemSet)
          await Play();
      }
      else if (ActualItem != null)
      {
        ActualItem.IsPaused = true;
      }
    }

    #endregion

    #region OnVlcError

    protected virtual void OnVlcError()
    {
      VSynchronizationContext.PostOnUIThread(() =>
      {
        windowManager.ShowErrorPrompt($"Unable to play item {ActualItem?.Name}\nSource: {ActualItem?.Model?.Source}");
      });
    }

    #endregion

    #region OnEndReached

    protected virtual void OnEndReached()
    {
      if (PlayNextItemOnEndReached)
      {
        Task.Run(() =>
        {
          TItemViewModel nextItem = null;

          if (IsRepeate && actualItemIndex >= PlayList.Count - 1)
          {
            actualItemIndex = 0;

            if (PlayList.Count > 0)
            {
              nextItem = PlayList[actualItemIndex];
            }
          }

          PlayNextWithItem(nextItem);
        });
      }
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


    CancellationTokenSource mediaToken;

    protected virtual async Task SetMedia(TModel model)
    {
      mediaToken?.Cancel();
      mediaToken = new CancellationTokenSource();

      MediaPlayer.SetNewMedia(null, mediaToken.Token);

      if (model == null)
        return;

      await BeforeSetMedia(model);

      if (model.Source != null)
      {
        try
        {
          var fileUri = new Uri(model.Source);

          MediaPlayer.SetNewMedia(fileUri, mediaToken.Token);

          OnNewItemPlay(model);
        }
        catch (UriFormatException ex)
        {
          VSynchronizationContext.PostOnUIThread(() =>
          {
            statusManager.ShowFailedMessage($"Item source was not in correct format.\nURI: \"{model.Source}\"", true);
          });
        }
      }
      else
      {
        VSynchronizationContext.PostOnUIThread(() =>
        {
          statusManager.ShowFailedMessage($"Item source is NULL", true);
        });
      }
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
          if (ActualItem != null)
          {
            if (MediaPlayer.Media == null)
            {
              await SetMedia(ActualItem.Model);
            }

            MediaPlayer.Play();

            VSynchronizationContext.InvokeOnDispatcher(() =>
            {
              IsPlaying = true;
            });
          }
        }

        OnPlay();
      });
    }

    #endregion

    #region PlayPlaylist

    private int? playlistItemIndex = null;
    protected virtual void PlayPlaylist(PlayItemsEventData<TItemViewModel> data, int? lastSongIndex = null, bool onlySet = false)
    {
      clearPlaylistDisposable?.Dispose();

      ActualSavedPlaylist = data.GetModel<TPlaylistModel>();
      ActualSavedPlaylist.LastPlayed = DateTime.Now;

      if (data.IsShuffle.HasValue)
        IsShuffle = data.IsShuffle.Value;

      if (data.IsRepeat.HasValue)
        IsRepeate = data.IsRepeat.Value;


      var playlistItems = ActualSavedPlaylist.PlaylistItems
        .Where(x => x.ReferencedItem.Source != null)
        .DistinctBy(x => x.ReferencedItem.Source).ToList();


      playlistItems.AddRange(ActualSavedPlaylist.PlaylistItems
        .Where(x => x.ReferencedItem.Source == null));

      playlistItems = playlistItems.OrderBy(x => x.OrderInPlaylist)
        .ToList();

      var savePlaylist = playlistItems.Count != ActualSavedPlaylist.ItemCount;

      if (savePlaylist)
      {
        ActualSavedPlaylist.PlaylistItems = playlistItems;
      }

      var items = GetVmToPlayFromPlaylist(playlistItems);

      if (lastSongIndex == null)
      {
        PlayItems(items, 0, savePlaylist, onlyItemSet: onlySet);
      }
      else
      {
        PlayItems(items, lastSongIndex.Value, savePlaylist, onlyItemSet: onlySet);

        if (data.SetPostion.HasValue)
        {
          MediaPlayer.Position = data.SetPostion.Value;
        }
      }

      OnPlayPlaylist(data);

      clearPlaylistDisposable = PlayList.Cleared.Subscribe(async (x) => { await ResetProperties(); });
    }

    #endregion

    protected virtual IEnumerable<TItemViewModel> GetVmToPlayFromPlaylist(IEnumerable<TPlaylistItemModel> playlistItems)
    {
      return playlistItems.Select(x => viewModelsFactory.Create<TItemViewModel>(x.ReferencedItem));
    }

    #region PlayPuse

    public async void PlayPause()
    {
      await Task.Run(() =>
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
        actualItemIndex = PlayList.Count;
      }

      if (IsShuffle)
      {
        var list = shuffleList.ToList();

        if (list.Count > 1)
        {
          var lastItem = list[^2];

          shuffleList.Remove(lastItem);
          shuffleList.Remove(list[^1]);

          shuffleRandom = new Random(shuffleRandomSeed);

          for (int i = 0; i < shuffleList.Count; i++)
          {
            shuffleRandom.NextDouble();
          }

          actualItemIndex = PlayList.IndexOf(lastItem);
        }
      }

      SetItemAndPlay(actualItemIndex, true);
    }

    #endregion

    #region PlayNext

    public virtual void PlayNext()
    {
      SetItemAndPlay(null, true);
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

    protected virtual void PlayItems(IEnumerable<TItemViewModel> items, int songIndex, bool savePlaylist = true, bool editSaved = false, bool onlyItemSet = false)
    {
      var itemList = items.ToList();

      PlayList.ForEach(x => x.IsInPlaylist = false);
      PlayList.Clear();
      PlayList.AddRange(itemList);
      actualItemIndex = songIndex;

      RequestReloadVirtulizedPlaylist();

      if (!onlyItemSet)
        IsPlaying = true;

      if (savePlaylist)
      {
        StorePlaylist(itemList, editSaved: editSaved);

      }

      SetItemAndPlay(songIndex, onlyItemSet: onlyItemSet);
    }

    #endregion

    #region BeforePlayEvent

    protected virtual async Task BeforePlayEvent(PlayItemsEventData<TItemViewModel> data)
    {
      if (data.EventAction != EventAction.Add)
      {
        await SetMedia(null);
      }
    }

    #endregion

    #region PlayItemsFromEvent

    private bool firstInit = true;
    protected async void PlayItemsFromEvent(PlayItemsEventData<TItemViewModel> data)
    {
      ActualSearch = "";

      if (!data.Items.Any())
        return;

      if (PlayList.Any() && data.EventAction == EventAction.InitSetPlaylist)
      {
        return;
      }

      if (ActualSavedPlaylist != null && ActualSavedPlaylist.Id > 0 && data.EventAction != EventAction.Add)
      {
        await UpdateActualSavedPlaylistPlaylist();
        ActualSavedPlaylist = new TPlaylistModel() { Id = -1 };
      }

      await BeforePlayEvent(data);

      {
        if (data.EventAction != EventAction.InitSetPlaylist && data.EventAction != EventAction.Add)
        {
          VSynchronizationContext.InvokeOnDispatcher(() =>
          {
            IsActive = true;
          });

          if (firstInit)
            await Task.Delay(50);
        }
      }

      firstInit = false;

      switch (data.EventAction)
      {
        case EventAction.Play:
          PlayItems(data.Items, 0, data.StorePlaylist, onlyItemSet: data.SetItemOnly);

          break;
        case EventAction.Add:
          PlayList.AddRange(data.Items);

          if (ActualItem == null)
          {
            SetActualItem(0);

            if (ActualItem != null)
              await SetMedia(ActualItem.Model);
          }

          RequestReloadVirtulizedPlaylist();
          RaisePropertyChanged(nameof(CanPlay));

          StorePlaylist(PlayList.ToList(), ActualSavedPlaylist.IsUserCreated);
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
        case EventAction.InitSetPlaylist:
          {
            var model = data.GetModel<TPlaylistModel>();
            PlayPlaylist(data, model.LastItemIndex, true);
          }

          break;
        default:
          throw new ArgumentOutOfRangeException();
      }

      RaisePropertyChanged(nameof(CanPlay));

      //Nie su to tie iste viewmodely z nejakeho dôvodu
      if (data.EventAction == EventAction.Play)
      {
        data.Items = PlayList;
      }

      OnPlayEvent(data);
    }

    #endregion

    #endregion

    #region Playlist methods

    #region StorePlaylist

    public Task<bool> StorePlaylist(List<TItemViewModel> items, bool isUserCreated = false, bool editSaved = true)
    {
      return Task.Run(() =>
      {
        var acutalPlaylist = items;

        if (acutalPlaylist == null || acutalPlaylist.Count == 0)
        {
          return false;
        }

        var playlistModels = new List<TPlaylistItemModel>();

        for (int i = 0; i < acutalPlaylist.Count; i++)
        {
          var song = acutalPlaylist[i];

          if (song != null)
          {
            var newItem = GetNewPlaylistItemViewModel(song, i);

            if (newItem == null)
            {
              return false;
            }

            newItem.OrderInPlaylist = i + 1;

            playlistModels.Add(newItem);
          }
        }

        var songIds = acutalPlaylist.Where(x => x.Model != null).Select(x => x.Model.Id).ToList();

        var hashCode = songIds.GetSequenceHashCode();

        var entityPlayList = GetNewPlaylistModel(playlistModels, isUserCreated);

        if (entityPlayList == null)
        {
          return false;
        }

        entityPlayList.LastPlayed = DateTime.Now;
        entityPlayList.HashCode = hashCode;

        bool success = false;

        var storedPlaylist = storageManager.GetTempRepository<TPlaylistModel>()
          .Include(x => x.PlaylistItems)
          .ThenInclude(x => x.ReferencedItem)
          .OrderByDescending(x => x.IsUserCreated)
          .FirstOrDefault(x => x.HashCode == hashCode);

        if (storedPlaylist != null && storedPlaylist.ItemCount != entityPlayList.ItemCount)
        {
          var songIds1 = storedPlaylist.PlaylistItems.Select(x => x.Id).ToList();

          var hashCode1 = songIds1.GetSequenceHashCode();

          storedPlaylist.HashCode = hashCode1;

          storageManager.UpdatePlaylist<TPlaylistModel, TPlaylistItemModel, TModel>(storedPlaylist,out var ns);
          
          if(storedPlaylist.HashCode != hashCode)
          {
            storedPlaylist = null;
          }
        }


        if (storedPlaylist == null)
        {
          if (editSaved || ActualSavedPlaylist.IsUserCreated)
          {
            if (hashCode != ActualSavedPlaylist.HashCode || ActualSavedPlaylist.ItemCount != entityPlayList.ItemCount)
            {
              var newPlaylistItems = new List<TPlaylistItemModel>();

              if (ActualSavedPlaylist.PlaylistItems != null)
              {
                VSynchronizationContext.InvokeOnDispatcher(() =>
                {
                  var oldItems = ActualSavedPlaylist.PlaylistItems.Where(x => playlistModels.Any(y =>
                  {
                    var isSame = y.IdReferencedItem == x.IdReferencedItem;

                    if (isSame)
                    {
                      x.Update(y);
                    }

                    return isSame;
                  }));

                  var diff = playlistModels.Where(x => ActualSavedPlaylist.PlaylistItems.All(y => y.IdReferencedItem != x.IdReferencedItem));

                  newPlaylistItems.AddRange(oldItems);
                  newPlaylistItems.AddRange(diff);
                });
              }
              else
              {
                newPlaylistItems = playlistModels;
              }

              ActualSavedPlaylist.HashCode = hashCode;
              ActualSavedPlaylist.PlaylistItems = newPlaylistItems;
              ActualSavedPlaylist.ItemCount = newPlaylistItems.Count;
              ActualSavedPlaylist.ActualItemId = null;

              if (ActualSavedPlaylist.Id < 0)
              {
                success = storageManager.StoreEntity(entityPlayList, out var dbEntityPlalist);

                UpdatePlaylist(entityPlayList, dbEntityPlalist);

                VSynchronizationContext.InvokeOnDispatcher(() =>
                {
                  ActualSavedPlaylist = entityPlayList;

                  ActualSavedPlaylist.LastPlayed = DateTime.Now;
                });
              }

              if (ActualSavedPlaylist.PlaylistItems.Count > actualItemIndex)
              {
                ActualSavedPlaylist.ActualItem = ActualSavedPlaylist.PlaylistItems.OrderBy(x => x.OrderInPlaylist).ToList()[actualItemIndex];

                ActualSavedPlaylist.ActualItemId = ActualSavedPlaylist?.ActualItem?.Id;
              }

            }
          }
          else if (!ActualSavedPlaylist.IsUserCreated)
          {
            if (ActualSavedPlaylist.Id <= 0)
            {
              success = storageManager.StoreEntity(entityPlayList, out var dbEntityPlalist);

              if (success)
              {
                UpdatePlaylist(entityPlayList, dbEntityPlalist);

                VSynchronizationContext.InvokeOnDispatcher(() =>
                {
                  ActualSavedPlaylist = entityPlayList;
                });

                if (ActualSavedPlaylist.PlaylistItems?.Count > actualItemIndex)
                {
                  ActualSavedPlaylist.ActualItem = ActualSavedPlaylist.PlaylistItems.OrderBy(x => x.OrderInPlaylist).ToList()[actualItemIndex];
                  ActualSavedPlaylist.ActualItemId = ActualSavedPlaylist?.ActualItem?.Id;

                  UpdateActualSavedPlaylistPlaylist();
                }
              }
            }
            else
            {
              success = storageManager.StoreEntity(entityPlayList, out var dbEntityPlalist);

              UpdatePlaylist(entityPlayList, dbEntityPlalist);

              VSynchronizationContext.InvokeOnDispatcher(() =>
              {
                ActualSavedPlaylist = entityPlayList;

                ActualSavedPlaylist.LastPlayed = DateTime.Now;
              });
            }
          }
        }
        else
        {
         
          VSynchronizationContext.InvokeOnDispatcher(() =>
          {
            ActualSavedPlaylist = storedPlaylist;
            SetActualItem(storedPlaylist.LastItemIndex);

            ActualSavedPlaylist.LastPlayed = DateTime.Now;

            OnStoredPlaylistLoaded();
          });
        }

        if (ActualSavedPlaylist != null)
        {
          VSynchronizationContext.PostOnUIThread(() =>
          {
            ActualSavedPlaylist.LastPlayed = DateTime.Now;
          });
        }

        UpdateActualSavedPlaylistPlaylist();

        return success;
      });
    }

    #endregion

    #region UpdatePlaylist

    protected virtual void UpdatePlaylist(TPlaylistModel playlistToUpdate, TPlaylistModel other)
    {
      if (playlistToUpdate.Id == 0)
        playlistToUpdate.Id = other.Id;

      playlistToUpdate.IsUserCreated = other.IsUserCreated;
    }

    #endregion

    #region UpdateActualSavedPlaylistPlaylist

    private SemaphoreSlim playlistSemaphore = new SemaphoreSlim(1, 1);
    protected async Task<bool> UpdateActualSavedPlaylistPlaylist()
    {
      try
      {
        if (ActualSavedPlaylist.PlaylistItems?.Any() == true)
        {
          ActualSavedPlaylist.ItemCount = ActualSavedPlaylist.PlaylistItems.Count;

          if (ActualSavedPlaylist.ActualItem != null)
            ActualSavedPlaylist.ActualItem.ReferencedItem = ActualItem.Model;
        }


        var clone = ActualSavedPlaylist.DeepClone();

        return await Task.Run(async () =>
        {
          try
          {
            await playlistSemaphore.WaitAsync();

            var result = storageManager.UpdatePlaylist<TPlaylistModel, TPlaylistItemModel, TModel>(clone, out var updated);

            if (result && updated.IsPrivate)
            {
              var notPrivateItems = updated.PlaylistItems.Where(x => x.ReferencedItem != null).Where(x => !x.ReferencedItem.IsPrivate).ToList();

              //I was lazy to add items as prite when added to playlist, instead of forcing all items to by private in private playlist
              foreach (var item in notPrivateItems)
              {
                item.ReferencedItem.IsPrivate = true;

                var playlistItem = PlayList.SingleOrDefault(x => x.Model.Id == item.IdReferencedItem);

                if (playlistItem != null)
                {
                  playlistItem.Model = item.ReferencedItem;
                  playlistItem.RaiseNotifyPropertyChanged(nameof(IItemInPlayList<TModel>.Model));
                  playlistItem.RaiseNotifyPropertyChanged(nameof(IItemInPlayList<TModel>.IsPrivate));
                }
              }

              if (notPrivateItems.Any())
              {
                await storageManager.UpdateEntitiesAsync(notPrivateItems.Select(x => x.ReferencedItem));
              }
            }

            if (result && !isDisposing)
            {
              try
              {
                VSynchronizationContext.PostOnUIThread(() =>
                {
                  if (VFocusManager.FocusedItems.Count(x => x.Name == "NameTextBox") == 0)
                  {
                    ActualSavedPlaylist = updated;
                  }
                });
              }
              catch (Exception)
              {
              }
            }

            return result;
          }
          finally
          {
            playlistSemaphore.Release();
          }
        });
      }
      catch (Exception ex)
      {
        logger.Log(ex);
        return false;
      }
    }

    #endregion

    #endregion

    #region IsInFind

    protected bool IsInFind(string original, string phrase, bool useContains = true)
    {
      bool result = false;
      phrase = phrase?.ToLower();

      if (original != null && phrase != null)
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

    #region RequestReloadVirtulizedPlaylist

    private VTimer virtulizedPlaylistTimer = new VTimer();

    protected void RequestReloadVirtulizedPlaylist()
    {
      virtulizedPlaylistTimer.RequestMethodCall(ReloadVirtulizedPlaylist);
    }

    #endregion

    #region ReloadVirtulizedPlaylist

    private void ReloadVirtulizedPlaylist()
    {
      var generator = new ItemsGenerator<TItemViewModel>(PlayList, 15);

      VirtualizedPlayList = new VirtualList<TItemViewModel>(generator);
    }

    #endregion

    #region RemoveItemsFromPlaylist

    protected async void RemoveItemsFromPlaylist(RemoveFromPlaylistEventArgs<TItemViewModel> obj)
    {
      var oldPlaylist = new List<KeyValuePair<TItemViewModel, int>>();

      for (int i = 0; i < PlayList.Count; i++)
      {
        oldPlaylist.Add(new KeyValuePair<TItemViewModel, int>(PlayList[i], i));
      }

      switch (obj.DeleteType)
      {
        case DeleteType.SingleFromPlaylist:
          foreach (var songToDelete in obj.ItemsToRemove)
          {
            var songInPlaylist = PlayList.SingleOrDefault(x => x == songToDelete);

            if (ActualItem == songInPlaylist && PlayList.Count == 1)
            {
              await ClearPlaylist();
            }
            else if (songInPlaylist != null)
            {
              PlayList.Remove(songInPlaylist);
            }
          }
          break;
        case DeleteType.AlbumFromPlaylist:
          OnRemoveItemsFromPlaylist(DeleteType.AlbumFromPlaylist, obj);

          break;
        case DeleteType.File:
          {
            foreach (var item in obj.ItemsToRemove)
            {
              var result = windowManager.ShowDeletePrompt(item.Name);

              if (result == VCore.WPF.ViewModels.Prompt.PromptResult.Ok)
              {
                BeforeDeleteFile(item);

                if (ActualItem == item)
                {
                  MediaPlayer.Stop();
                  await SetMedia(null);
                }

                var itemInPlayList = PlayList.SingleOrDefault(x => x.Model.Id == item.Model.Id);

                if (ActualSavedPlaylist?.ActualItem?.IdReferencedItem == item.Model.Id)
                {
                  ActualSavedPlaylist.ActualItem = null;
                  ActualSavedPlaylist.ActualItemId = null;
                }

                if (itemInPlayList != null)
                {
                  PlayList.Remove(item);
                }

                Task.Run(async () =>
                {
                  await Task.Delay(1000);

                  try
                  {
                    var parentDirectory = new DirectoryInfo(Path.GetDirectoryName(item.Model.Source));
                    var files = parentDirectory.GetFiles();
                    var sounds = files.Count(x => x.Extension.GetFileType() == FileType.Sound);
                    var videos = files.Count(x => x.Extension.GetFileType() == FileType.Video);

                    if (sounds + videos == 1)
                    {
                      FileSystem.DeleteDirectory(parentDirectory.FullName, UIOption.AllDialogs, RecycleOption.SendToRecycleBin);
                    }
                    else
                    {
                      FileSystem.DeleteFile(item.Model.Source, UIOption.AllDialogs, RecycleOption.SendToRecycleBin);
                    }

                  }
                  catch (Exception ex)
                  {
                  }
                });
              }
            }
          }
          break;
        default:
          throw new ArgumentOutOfRangeException();
      }

      if (!PlayList.Contains(ActualItem))
      {
        ActualItem = null;
      }

      if (ActualItem != null)
      {
        actualItemIndex = PlayList.IndexOf(ActualItem);
      }
      else if (PlayList.Count > 0)
      {
        actualItemIndex--;
        SetItemAndPlay();
      }

      RequestReloadVirtulizedPlaylist();
      StorePlaylist(PlayList.ToList());
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

    #region UpdateOrAddActualSavedPlaylist

    protected async Task UpdateOrAddActualSavedPlaylist()
    {
      var result = await UpdateActualSavedPlaylistPlaylist();

      if (!result)
      {
        ActualSavedPlaylist.ActualItem = null;
        ActualSavedPlaylist.PlaylistItems?.ForEach(x => x.Id = 0);

        var success = storageManager.StoreEntity(ActualSavedPlaylist, out var dbEntityPlalist);

        if (success)
        {
          UpdatePlaylist(ActualSavedPlaylist, dbEntityPlalist);

          VSynchronizationContext.InvokeOnDispatcher(() =>
          {
            ActualSavedPlaylist = ActualSavedPlaylist;

            ActualSavedPlaylist.LastPlayed = DateTime.Now;
          });
        }
      }
    }

    #endregion

    #region GetAllItemsSources

    public IEnumerable<string> GetAllItemsSources()
    {
      return PlayList.Select(x => x.Model.Source).ToArray();
    }

    #endregion

    #region SetVolumeAndRaiseNotification

    public void SetVolumeAndRaiseNotification(int pVolume)
    {
      int step = 2;
      bool raise = pVolume > MediaPlayer.Volume;

      if (pVolume == 99 && raise)
      {
        pVolume = 100;
      }
      else if (pVolume == 101 && !raise)
      {
        pVolume = 100;
      }

      if (pVolume > 100 && raise && AudioDeviceManager.Instance.ActualVolume < 100)
      {
        AudioDeviceManager.Instance.ActualVolume = AudioDeviceManager.Instance.ActualVolume + step;
      }
      else if (!raise && pVolume < 100 && AudioDeviceManager.Instance.ActualVolume > 0)
      {
        AudioDeviceManager.Instance.ActualVolume = AudioDeviceManager.Instance.ActualVolume - step;
      }
      else
      {
        SetVolumeWihtoutNotification(pVolume);
      }
    }

    #endregion

    #region SetVolume

    public void SetVolumeWihtoutNotification(int pVolume)
    {
      if (MediaPlayer != null)
      {
        Volume = pVolume;
      }
    }

    #endregion

    #region RaiseMediaPlayerProperties

    protected void RaiseMediaPlayerProperties()
    {
      RaisePropertyChanged(nameof(IsMuted));
      RaisePropertyChanged(nameof(IsPlaying));
      RaisePropertyChanged(nameof(Volume));
    }

    #endregion

    private async void UpdatePlaylist()
    {
      var songIds = PlayList.Where(x => x.Model != null).Select(x => x.Model.Id).ToList();

      var hashCode = songIds.GetSequenceHashCode();

      if(hashCode != ActualSavedPlaylist.HashCode)
      {
        RequestReloadVirtulizedPlaylist();
        await StorePlaylist(PlayList.Select(x => x).ToList(), editSaved: true);
      }
      else
      {
        await UpdateActualSavedPlaylistPlaylist();
      }


      actualItemIndex = PlayList.IndexOf(ActualItem);
      actualItemSubject.OnNext(actualItemIndex);

      RaisePropertyChanged(nameof(ActualItemIndex));
    }

    private VDispatcherTimer dispatcherTimer = new VDispatcherTimer(500);

    protected void RequestUIDispatcher(Action action)
    {
      dispatcherTimer.RequestMethodCallOnDispatcher(action);
    }

    protected virtual void OnStoredPlaylistLoaded()
    {

    }

    //Virtual methods 
    #region Virtual methods

    #region OnNewItemPlay

    public virtual void OnNewItemPlay(TModel model)
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

    protected virtual void BeforeClearPlaylist()
    {

    }

    protected virtual Task BeforeSetMedia(TModel model)
    {
      return Task.CompletedTask;
    }

    protected virtual void OnPlay()
    {
      VSynchronizationContext.InvokeOnDispatcher(() =>
      {
        if (ActualItem != null)
          ActualItem.IsPlaying = true;
      });
    }

    protected virtual void OnPlayPlaylist(PlayItemsEventData<TItemViewModel> data)
    {

    }

    protected virtual void OnIsPlayingChanged()
    {

    }

    protected virtual void OnShuffle(bool value) { }

    protected virtual void OnRepeate(bool value) { }

    protected virtual void BeforeDeleteFile(TItemViewModel itemViewModel) { }

    protected virtual void OnDownloadInfoEvent(TItemViewModel itemViewModel) { }

    protected virtual Task DownloadInfos(IEnumerable<TItemViewModel> itemViewModels) { return Task.CompletedTask; }

    protected virtual Task SaveData(IEnumerable<TItemViewModel> itemViewModels) { return Task.CompletedTask; }


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

    private bool isDisposing;
    public override void Dispose()
    {
      isDisposing = true;

      Task.Run(async () =>
      {
        await ClearPlaylist();
      });

      MediaPlayer.Media = null;
      MediaPlayer.Stop();

      dispatcherTimer.Dispose();
      virtulizedPlaylistTimer.Dispose();

      volumeSubject?.Dispose();
      clearPlaylistDisposable?.Dispose();

      MediaPlayer.Playing -= OnVlcPlayingChanged;
      MediaPlayer.Dispose();

      base.Dispose();
    }

    #endregion

    #endregion
  }
}
