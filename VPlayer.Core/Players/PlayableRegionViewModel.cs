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
using VCore.WPF.Managers;
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
    private readonly IWindowManager windowManager;
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
      IWindowManager windowManager,
      VLCPlayer vLCPlayer) : base(regionProvider)
    {
      this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
      this.storageManager = storageManager ?? throw new ArgumentNullException(nameof(storageManager));
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
    protected Random shuffleRandom = new Random();

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
      ActualSavedPlaylist.IsUserCreated = !ActualSavedPlaylist.IsUserCreated;

      UpdateActualSavedPlaylistPlaylist();

      RaisePropertyChanged(nameof(ActualSavedPlaylist));
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
      if (ActualSavedPlaylist.Id > 0)
      {
        await UpdateActualSavedPlaylistPlaylist();
      }

      BeforeClearPlaylist();

      IsPlaying = false;
      VirtualizedPlayList = null;
      PlayList.Clear();
      ActualItem = null;
      MediaPlayer.Stop();
      await MediaPlayer.SetNewMedia(null);
      MediaPlayer.Reload();
      actualItemIndex = 0;
      PlaylistTotalTimePlayed = new TimeSpan(0);
      ActualSavedPlaylist = new TPlaylistModel() { Id = -1 };
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

    #region InitilizeMediaPlayer

    protected async Task InitilizeMediaPlayer()
    {
      await MediaPlayer.Initilize();
    }

    #endregion

    #region HookToVlcEvents

    protected virtual async Task HookToPlayerEvents()
    {
      await Task.Run(() => InitilizeMediaPlayer());

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

    public virtual async void SetItemAndPlay(int? songIndex = null, bool forcePlay = false, bool onlyItemSet = false)
    {

      if (IsShuffle && songIndex == null)
      {
        var result = PlayList.Where(p => shuffleList.All(p2 => p2 != p)).ToList();

        if (result.Count == 0)
        {
          shuffleList.Clear();
          result = PlayList.Where(p => shuffleList.All(p2 => p2 != p)).ToList();
        }

        var shuffleIndex = (int)Math.Floor(shuffleRandom.NextDouble() * result.Count);

        songIndex = PlayList.IndexOf(result[shuffleIndex]);
      }

      if (songIndex != null)
      {
        actualItemIndex = songIndex.Value;
      }

      if (IsRepeate && actualItemIndex > PlayList.Count - 1)
      {
        actualItemIndex = 0;
        songIndex = 0;
      }

      if (!string.IsNullOrEmpty(actualSearch))
      {
        Application.Current?.Dispatcher?.Invoke(() =>
        {
          ActualSearch = null;
        });
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
        Application.Current?.Dispatcher?.Invoke(() =>
        {
          IsPlayFnished = true;
        });

        Pause();
        return;
      }

      Application.Current?.Dispatcher?.Invoke(() =>
      {
        SetActualItem(actualItemIndex);
      });

      if (ActualItem == null)
        return;

      var oldPlaying = IsPlaying;

      Application.Current?.Dispatcher?.Invoke(() =>
      {
        IsPlaying = true;
        ActualItem.IsPlaying = true;
      });

      await SetMedia(ActualItem.Model);

      if (oldPlaying || forcePlay)
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
      {
        Application.Current?.Dispatcher?.Invoke(() =>
        {
          ActualItem.IsPaused = true;
        });
      }
    }

    #endregion

    #region OnVlcError

    protected virtual void OnVlcError()
    {
      Application.Current.Dispatcher.Invoke(() =>
      {
        windowManager.ShowErrorPrompt($"Unable to play item {ActualItem?.Name}\nSource: {ActualItem?.Model?.Source}");
      });
    }

    #endregion

    #region OnEndReached

    protected virtual void OnEndReached()
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

        OnPlay();
      });
    }

    #endregion

    #region PlayPlaylist

    private void PlayPlaylist(PlayItemsEventData<TItemViewModel> data, int? lastSongIndex = null)
    {
      ActualSavedPlaylist = data.GetModel<TPlaylistModel>();
      ActualSavedPlaylist.LastPlayed = DateTime.Now;

      if (data.IsShuffle.HasValue)
        IsShuffle = data.IsShuffle.Value;

      if (data.IsRepeat.HasValue)
        IsRepeate = data.IsRepeat.Value;

      if (lastSongIndex == null)
      {
        PlayItems(data.Items, false);
      }
      else
      {
        PlayItems(data.Items, false, lastSongIndex.Value);

        if (data.SetPostion.HasValue)
        {
          MediaPlayer.Position = data.SetPostion.Value;
        }
      }

      OnPlayPlaylist(data);
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

      SetItemAndPlay(actualItemIndex);
    }

    #endregion

    #region PlayNext

    public virtual void PlayNext()
    {
      SetItemAndPlay(null);
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
        RequestReloadVirtulizedPlaylist();

        IsPlaying = true;

        SetItemAndPlay(songIndex, onlyItemSet: onlyItemSet);

        var listPlaylist = PlayList.ToList();

        Task.Run(() =>
        {
          if (savePlaylist)
          {
            StorePlaylist(listPlaylist, editSaved: editSaved);
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

          RequestReloadVirtulizedPlaylist();
          RaisePropertyChanged(nameof(CanPlay));

          StorePlaylist(PlayList.ToList(), editSaved: true);
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

    public bool StorePlaylist(List<TItemViewModel> items, bool isUserCreated = false, bool editSaved = false)
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

      entityPlayList.HashCode = hashCode;

      bool success = false;

      var storedPlaylist = storageManager.GetRepository<TPlaylistModel>().Where(x => !x.IsUserCreated).SingleOrDefault(x => x.HashCode == hashCode);

      if (storedPlaylist == null)
      {
        if (editSaved && ActualSavedPlaylist.IsUserCreated)
        {
          if (hashCode != ActualSavedPlaylist.HashCode)
          {
            Application.Current.Dispatcher.Invoke(() =>
            {
              ActualSavedPlaylist.HashCode = hashCode;

              foreach (var pItemInPlaylist in playlistModels)
              {
                pItemInPlaylist.Id = ActualSavedPlaylist.PlaylistItems
                  .Single(x => x.IdReferencedItem == pItemInPlaylist.IdReferencedItem).Id;
              }

              ActualSavedPlaylist.PlaylistItems = playlistModels;
              ActualSavedPlaylist.ItemCount = playlistModels.Count;
            });

          }
        }
        else if (!ActualSavedPlaylist.IsUserCreated)
        {
          if (ActualSavedPlaylist.Id <= 0)
          {
            success = storageManager.StoreEntity(entityPlayList, out var dbEntityPlalist);

            UpdateNonUserCreatedPlaylist(entityPlayList, dbEntityPlalist);

            Application.Current.Dispatcher.Invoke(() => { ActualSavedPlaylist = entityPlayList; });
          }
          else
          {
            Application.Current.Dispatcher.Invoke(() => { ActualSavedPlaylist = entityPlayList; });

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

          Application.Current.Dispatcher.Invoke(() =>
          {
            ActualSavedPlaylist = entityPlayList;

            ActualSavedPlaylist.LastPlayed = DateTime.Now;
          });
        }
        else
        {
          storedPlaylist.Update(entityPlayList);

          if (storageManager.UpdatePlaylist<TPlaylistModel, TPlaylistItemModel>(storedPlaylist, out var updated))
          {
            Application.Current.Dispatcher.Invoke(() =>
            {
              ActualSavedPlaylist = updated;
            });
          }
        }
      }

      if (ActualSavedPlaylist != null)
      {
        Application.Current.Dispatcher.Invoke(() =>
        {
          ActualSavedPlaylist.LastPlayed = DateTime.Now;
        });
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

    protected Task UpdateActualSavedPlaylistPlaylist()
    {
      return Task.Run(() =>
       {
         if (storageManager.UpdatePlaylist<TPlaylistModel, TPlaylistItemModel>(ActualSavedPlaylist, out var updated))
         {
           Application.Current.Dispatcher.Invoke(() => { ActualSavedPlaylist = updated; });
         }
       });
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

    private Stopwatch stopwatchReloadVirtulizedPlaylist;
    private object batton = new object();
    private SerialDisposable serialDisposable = new SerialDisposable();

    protected void RequestReloadVirtulizedPlaylist()
    {
      int dueTime = 1500;
      lock (batton)
      {
        serialDisposable.Disposable = Observable.Timer(TimeSpan.FromMilliseconds(dueTime)).Subscribe((x) =>
        {
          stopwatchReloadVirtulizedPlaylist = null;
          ReloadVirtulizedPlaylist();
        });

        if (stopwatchReloadVirtulizedPlaylist == null || stopwatchReloadVirtulizedPlaylist.ElapsedMilliseconds > dueTime)
        {
          ReloadVirtulizedPlaylist();

          stopwatchReloadVirtulizedPlaylist = new Stopwatch();
          stopwatchReloadVirtulizedPlaylist.Start();
        }
      }
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

          StorePlaylist(PlayList.ToList(), editSaved: true);

          break;
        case DeleteType.AlbumFromPlaylist:
          OnRemoveItemsFromPlaylist(DeleteType.AlbumFromPlaylist, obj);

          break;
        default:
          throw new ArgumentOutOfRangeException();
      }


      RequestReloadVirtulizedPlaylist();

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

    protected virtual void BeforeClearPlaylist()
    {

    }

    protected virtual void OnPlay()
    {

    }

    protected virtual void OnPlayPlaylist(PlayItemsEventData<TItemViewModel> data)
    {

    }

    protected virtual void OnIsPlayingChanged()
    {

    }

    protected virtual void OnShuffle(bool value)
    {

    }

    protected virtual void OnRepeate(bool value)
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
      if (ActualSavedPlaylist != null && ActualSavedPlaylist.Id != -1)
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
