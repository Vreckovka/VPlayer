using System;
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
using VCore.WPF.Behaviors;
using MediaPlayer = LibVLCSharp.Shared.MediaPlayer;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Events;
using VPlayer.WindowsPlayer.Providers;

namespace VPlayer.Core.ViewModels
{
  public abstract class PlayableRegionViewModel<TView, TItemViewModel, TPlaylistModel, TPlaylistItemModel, TModel> : RegionViewModel<TView>, IPlayableRegionViewModel, IHideable
    where TView : class, IView
    where TItemViewModel : class, IItemInPlayList<TModel>
    where TModel : IPlayableModel
    where TPlaylistModel : class, IPlaylist<TPlaylistItemModel>, new()
  {
    #region Fields

    protected readonly ILogger logger;
    protected readonly IStorageManager storageManager;
    private readonly IVlcProvider vlcProvider;
    protected int actualItemIndex;
    protected HashSet<TItemViewModel> shuffleList = new HashSet<TItemViewModel>();
    protected LibVLC libVLC;
    private bool wasVlcInitilized;
    private long lastTimeChanged;
    private int lastUpdateSeconds;

    #endregion

    #region Constructors

    public PlayableRegionViewModel(
      IRegionProvider regionProvider,
      IKernel kernel,
       ILogger logger,
     IStorageManager storageManager,
      IEventAggregator eventAggregator,
       IVlcProvider vlcProvider) : base(regionProvider)
    {
      this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
      this.storageManager = storageManager ?? throw new ArgumentNullException(nameof(storageManager));
      this.vlcProvider = vlcProvider ?? throw new ArgumentNullException(nameof(vlcProvider));
      EventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));


      Kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
    }

    #endregion

    #region Properties

    #region MediaPlayer

    private MediaPlayer mediaPlayer;
    public MediaPlayer MediaPlayer
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
          actualItem = value;

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

          if (ActualSavedPlaylist.IsReapting != value)
          {
            ActualSavedPlaylist.IsReapting = value;
            UpdateActualSavedPlaylistPlaylist();
          }

          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region IsShuffle

    private bool isShuffle;
    public bool IsShuffle
    {
      get { return isShuffle; }
      set
      {
        if (value != isShuffle)
        {
          isShuffle = value;

          if (ActualSavedPlaylist.IsShuffle != value)
          {
            ActualSavedPlaylist.IsShuffle = value;
            UpdateActualSavedPlaylistPlaylist();
          }

          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region Kernel

    public IKernel Kernel { get; set; }

    #endregion

    #region TotalPlaylistDuration

    public TimeSpan TotalPlaylistDuration
    {
      get { return TimeSpan.FromSeconds(PlayList.Sum(x => x.Duration)); }
    }

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

    #region Volume

    private float volume;

    public float Volume
    {
      get { return volume; }
      set
      {
        if (value != volume)
        {
          volume = value;
          RaisePropertyChanged();
        }
      }
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

    #endregion

    #region Methods

    #region Initialize

    public override void Initialize()
    {
      InitializeAsync();
    }

    #endregion

    #region InitializeAsync

    public async Task InitializeAsync()
    {
      IsPlaying = false;

      base.Initialize();

      await HookToVlcEvents();

      actualSearchSubject = new ReplaySubject<string>(1).DisposeWith(this);

      PlayList.DisposeWith(this);

      PlayList.CollectionChanged += PlayList_CollectionChanged;

      actualSearchSubject.Throttle(TimeSpan.FromMilliseconds(150)).Subscribe(FilterByActualSearch).DisposeWith(this);

      HookToPubSubEvents();
    }



    private void PlayList_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
      RaisePropertyChanged(nameof(TotalPlaylistDuration));
    }

    #endregion

    #region LoadVlc

    private async Task LoadVlc()
    {
      var result = await vlcProvider.InitlizeVlc();

      MediaPlayer = result.Key;

      libVLC = result.Value;
    }

    #endregion

    #region HookToVlcEvents

    private async Task HookToVlcEvents()
    {
      await LoadVlc();

      Volume = mediaPlayer.Volume;

      if (MediaPlayer == null)
      {
        logger.Log(Logger.MessageType.Error, "VLC was not initlized!");
        return;
      }

      mediaPlayer.EncounteredError += (sender, e) =>
      {
        logger.Log(new Exception(e.ToString()), true);

        Application.Current.Dispatcher.Invoke(() =>
        {
          MessageBox.Show("VLC BROKE!!!!!");
        });
      };


      mediaPlayer.EndReached += (sender, e) => { Task.Run(() => PlayNextWithItem()); };

      mediaPlayer.TimeChanged += OnVlcTimeChanged;

      mediaPlayer.Paused += (sender, e) =>
      {
        if (ActualItem != null)
        {
          ActualItem.IsPaused = true;
        }
      };

      mediaPlayer.Stopped += (sender, e) =>
      {
        if (IsPlayFnished && ActualItem != null)
        {
          ActualItem.IsPlaying = false;
          ActualItem.IsPaused = false;
          ActualItem = null;
          actualItemIndex = -1;
          IsPlaying = false;
        }
      };

      mediaPlayer.Playing += OnVlcPlayingChanged;

      OnVlcLoaded();
    }


    #endregion

    #region HookToPubSubEvents

    private void HookToPubSubEvents()
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
            ActualSavedPlaylist.LastItemElapsedTime = 0;
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

    public void SetItemAndPlay(int? songIndex = null, bool forcePlay = false)
    {
      Application.Current?.Dispatcher?.Invoke(async () =>
      {
        if (!string.IsNullOrEmpty(actualSearch))
        {
          ActualSearch = null;
        }

        IsPlayFnished = false;

        if (IsShuffle && songIndex == null)
        {
          var random = new Random();
          var result = PlayList.Where(p => shuffleList.All(p2 => p2 != p)).ToList();

          actualItemIndex = random.Next(0, result.Count);
        }
        else if (songIndex == null)
        {
          actualItemIndex++;
        }
        else
        {
          actualItemIndex = songIndex.Value;
        }

        if (actualItemIndex >= PlayList.Count)
        {
          if (IsRepeate)
            actualItemIndex = 0;
          else
          {
            IsPlayFnished = true;
            Pause();
            return;
          }
        }

        SetActualItem(actualItemIndex);

        if (ActualItem == null)
          return;

        await SetVlcMedia(ActualItem.Model);

        if (IsPlaying || forcePlay)
        {
          Play();
        }
        else if (!IsPlaying && songIndex != null)
        {
          Play();
        }
        else if (ActualItem != null)
          ActualItem.IsPaused = true;
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

    #region SetVlcMedia

    protected virtual Task SetVlcMedia(TModel model)
    {
      return Task.Run(() =>
      {
        var media = mediaPlayer.Media;

        var fileUri = new Uri(model.DiskLocation);

        media = new Media(libVLC, fileUri);

        mediaPlayer.Media = media;

        media.DurationChanged += Media_DurationChanged;

        OnNewItemPlay();

      });
    }

    #endregion

    #region Media_DurationChanged

    protected void Media_DurationChanged(object sender, MediaDurationChangedEventArgs e)
    {
      Application.Current.Dispatcher.Invoke(() =>
      {
        ActualItem.Duration = (int)e.Duration / 1000;

        if (MediaPlayer.Media != null)
          MediaPlayer.Media.DurationChanged -= Media_DurationChanged;

       
      });
    }

    #endregion

    #region VlcMethods

    #region OnVlcTimeChanged

   
    private void OnVlcTimeChanged(object sender, MediaPlayerTimeChangedEventArgs eventArgs)
    {
      if (ActualItem != null)
      {
        var position = ((eventArgs.Time * 100) / (ActualItem.Duration * (float)1000.0)) / 100;

        if (!double.IsNaN(position) && !double.IsInfinity(position))
        {
          ActualItem.ActualPosition = position;
          ActualSavedPlaylist.LastItemElapsedTime = position;

          var deltaTimeChanged = eventArgs.Time - lastTimeChanged;

          lastTimeChanged = eventArgs.Time;

          PlaylistTotalTimePlayed +=  TimeSpan.FromMilliseconds(deltaTimeChanged);

          int totalSec = (int)PlaylistTotalTimePlayed.TotalSeconds;

          if (totalSec % 10 == 0 && totalSec > lastUpdateSeconds)
          {
            lastUpdateSeconds = totalSec;
            Task.Run(UpdateActualSavedPlaylistPlaylist);
          }
        }
      }
    }

    #endregion

    #region OnVlcPlayingChanged

    private void OnVlcPlayingChanged(object sender, EventArgs eventArgs)
    {
      if (ActualItem != null)
      {
        lastTimeChanged = 0;
        ActualItem.IsPlaying = true;
        ActualItem.IsPaused = false;
        IsPlaying = true;
      }
    }

    #endregion

    #endregion

    #region Play

    public Task Play()
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
            mediaPlayer.Play();
          }
        }
      });
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

    public void PlayNext()
    {
      actualItemIndex++;

      if (actualItemIndex >= PlayList.Count)
      {
        if (IsRepeate)
          actualItemIndex = 0;
      }

      SetItemAndPlay(actualItemIndex);
    }

    #endregion

    #region Pause

    public void Pause()
    {
      if (IsPlaying)
      {
        mediaPlayer.Pause();
        IsPlaying = false;
      }
    }

    #endregion

    #region SeekForward

    public void SeekForward(int seekSize = 50)
    {
      mediaPlayer.Position = mediaPlayer.Position + GetSeekSize(seekSize);
      ActualItem.ActualPosition = mediaPlayer.Position;
    }

    #endregion

    #region SeekBackward

    public void SeekBackward(int seekSize = 50)
    {
      mediaPlayer.Position = mediaPlayer.Position - GetSeekSize(seekSize);
      ActualItem.ActualPosition = mediaPlayer.Position;
    }

    #endregion

    #region GetSeekSize

    private float GetSeekSize(int seconds)
    {
      return seconds * (float)100.0 / mediaPlayer.Length;
    }

    #endregion

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

    private void UpdateNonUserCreatedPlaylist(TPlaylistModel playlistToUpdate, TPlaylistModel other)
    {
      if (playlistToUpdate.Id == 0)
        playlistToUpdate.Id = other.Id;


      playlistToUpdate.Name = other.Name;
      playlistToUpdate.IsReapting = other.IsReapting;
      playlistToUpdate.IsShuffle = other.IsShuffle;


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

    #region PlayItems

    protected void PlayItems(IEnumerable<TItemViewModel> songs, bool savePlaylist = true, int songIndex = 0, bool editSaved = false)
    {
      Application.Current.Dispatcher.Invoke(() =>
      {
        IsActive = true;

        PlayList.Clear();
        PlayList.AddRange(songs);
        ReloadVirtulizedPlaylist();

        IsPlaying = true;

        SetItemAndPlay(songIndex);

        if (ActualItem != null)
          ActualItem.IsPlaying = false;

        Task.Run(() =>
        {
          if (savePlaylist)
          {
            StorePlaylist(editSaved: editSaved);
          }
        });

      });
    }

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
          PlayItems(data.Items);

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

      if (data.IsShufle.HasValue)
        IsShuffle = data.IsShufle.Value;

      if (data.IsRepeat.HasValue)
        IsRepeate = data.IsRepeat.Value;

      OnPlayEvent();

    }

    #endregion 

    #endregion

    #region ReloadVirtulizedPlaylist

    protected void ReloadVirtulizedPlaylist()
    {
      var generator = new ItemsGenerator<TItemViewModel>(PlayList, 15);
      VirtualizedPlayList = new VirtualList<TItemViewModel>(generator);
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
          mediaPlayer.Position = data.SetPostion.Value;
      }
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
      if (mediaPlayer?.AudioTrack != -1 && mediaPlayer != null)
      {
        mediaPlayer.Volume = pVolume;
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

    protected virtual void OnPlayEvent()
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
        mediaPlayer.TimeChanged -= OnVlcTimeChanged;
        mediaPlayer.Playing -= OnVlcPlayingChanged;

        libVLC.Dispose();
        mediaPlayer.Dispose();

        PlayList.CollectionChanged -= PlayList_CollectionChanged;

        base.Dispose();
      });
    }

    #endregion

    #endregion


  }
}
