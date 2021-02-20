using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Logger;
using Ninject;
using Prism.Events;
using VCore;
using VCore.Annotations;
using VCore.Helpers;
using VCore.ItemsCollections;
using VCore.ItemsCollections.VirtualList;
using VCore.ItemsCollections.VirtualList.VirtualLists;
using VCore.Modularity.RegionProviders;
using VCore.Standard.Helpers;
using VCore.Standard.Modularity.Interfaces;
using VCore.ViewModels;
using Vlc.DotNet.Core;
using Vlc.DotNet.Wpf;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Events;

namespace VPlayer.Core.ViewModels
{
  public abstract class PlayableRegionViewModel<TView, TItemViewModel, TPlayItemEventData, TPlaylistModel, TPlaylistItemModel, TModel> : RegionViewModel<TView>, IPlayableRegionViewModel
    where TView : class, IView
    where TItemViewModel : class, IItemInPlayList<TModel>
    where TModel : IPlayableModel
    where TPlaylistModel : class, IPlaylist<TPlaylistItemModel>, new()
    where TPlayItemEventData : IPlayItemEventData<TItemViewModel>
  {
    #region Fields

    protected readonly ILogger logger;
    protected readonly IStorageManager storageManager;
    private int actualItemIndex;
    protected HashSet<TItemViewModel> shuffleList = new HashSet<TItemViewModel>();

    #endregion

    #region Constructors

    public PlayableRegionViewModel(
      IRegionProvider regionProvider,
      [NotNull] IKernel kernel,
      [NotNull] ILogger logger,
      [NotNull] IStorageManager storageManager,
      [NotNull] IEventAggregator eventAggregator) : base(regionProvider)
    {
      this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
      this.storageManager = storageManager ?? throw new ArgumentNullException(nameof(storageManager));
      EventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));


      Kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
    }

    #endregion

    #region Properties

    #region EventAgreggator

    private IEventAggregator eventAggregator;

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

          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region IsPlaying

    private bool isPlaying;

    public virtual bool IsPlaying
    {
      get { return isPlaying; }
      protected set
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

    #region VlcControl

    private VlcControl vlcControl = new VlcControl();

    public VlcControl VlcControl
    {
      get { return vlcControl; }
      set
      {
        if (value != vlcControl)
        {
          vlcControl = value;
          RaisePropertyChanged();
        }
      }
    }

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

    #endregion

    #region Methods

    #region Initialize

    public override void Initialize()
    {
      Task.Run(async () =>
      {
        base.Initialize();

        actualSearchSubject = new ReplaySubject<string>(1);
        actualSearchSubject.DisposeWith(this);
        PlayList.DisposeWith(this);

        actualSearchSubject.Throttle(TimeSpan.FromMilliseconds(150)).Subscribe(FilterByActualSearch).DisposeWith(this);

        LoadVlc();

        if (VlcControl.SourceProvider.MediaPlayer == null)
        {
          logger.Log(Logger.MessageType.Error, "VLC was not initlized!");
          return;
        }

        VlcControl.SourceProvider.MediaPlayer.EncounteredError += (sender, e) =>
        {
          Console.Error.Write("An error occurred");
          IsPlayFnished = true;
        };

        VlcControl.SourceProvider.MediaPlayer.EndReached += (sender, e) => { Task.Run(() => PlayNextWithItem()); };

        VlcControl.SourceProvider.MediaPlayer.TimeChanged += (sender, e) =>
        {
          if (ActualItem != null)
          {
            ActualItem.ActualPosition = ((VlcMediaPlayer)sender).Position;
            ActualSavedPlaylist.LastItemElapsedTime = ((VlcMediaPlayer)sender).Position;
          }
        };

        VlcControl.SourceProvider.MediaPlayer.Paused += (sender, e) =>
        {
          if (ActualItem != null)
          {
            ActualItem.IsPaused = true;
          }

          IsPlaying = false;
        };

        VlcControl.SourceProvider.MediaPlayer.Stopped += (sender, e) =>
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

        VlcControl.SourceProvider.MediaPlayer.Playing += (sender, e) =>
        {
          if (ActualItem != null)
          {
            ActualItem.IsPlaying = true;
            ActualItem.IsPaused = false;
            IsPlaying = true;
          }
        };
      });

      eventAggregator.GetEvent<RemoveFromPlaylistEvent<TItemViewModel>>().Subscribe(RemoveItemsFromPlaylist).DisposeWith(this);
      eventAggregator.GetEvent<PlaySongsFromPlayListEvent<TItemViewModel>>().Subscribe(PlayItemFromPlayList).DisposeWith(this);
      eventAggregator.GetEvent<PlayPauseEvent>().Subscribe(PlayPause).DisposeWith(this);
    }

    #endregion

    #region LoadVlc

    private void LoadVlc()
    {
      var currentAssembly = Assembly.GetEntryAssembly();

      if (currentAssembly != null)
      {
        var currentDirectory = new FileInfo(currentAssembly.Location).DirectoryName;

        var path = new DirectoryInfo(Path.Combine(currentDirectory, "libvlc", IntPtr.Size == 4 ? "win-x86" : "win-x64"));

        var libDirectory = new DirectoryInfo(path.FullName);

        try
        {
          VlcControl.SourceProvider.CreatePlayer(libDirectory);
        }
        catch (Exception ex)
        {

          throw;
        }
      }
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

    #region PlayNext

    public void PlayNext(int? songIndex = null, bool forcePlay = false)
    {
      Application.Current?.Dispatcher?.Invoke(() =>
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

        if (actualItemIndex == PlayList.Count)
        {
          if (IsRepeate)
            actualItemIndex = 0;
          else
          {
            IsPlayFnished = true;
            Stop();
            return;
          }
        }

        SetActualItem(actualItemIndex);

        if (IsPlaying || forcePlay)
        {
          SetVlcMedia(ActualItem.Model);
          Play();
        }
        else if (!IsPlaying && songIndex != null)
        {
          SetVlcMedia(ActualItem.Model);
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
        PlayNext();
      }
      else
      {
        var item = PlayList.SingleOrDefault(x => x == nextItem);

        if (ActualItem != item && item != null)
        {
          PlayNext(PlayList.IndexOf(item));
        }
      }
    }

    #endregion

    #region SetVlcMedia

    private void SetVlcMedia(TModel model)
    {
      var media = VlcControl.SourceProvider.MediaPlayer.GetMedia();

      if (media == null || media.NowPlaying != model.DiskLocation)
      {
        var location = new Uri(model.DiskLocation);

        VlcControl.SourceProvider.MediaPlayer.SetMedia(location);

        OnNewItemPlay();
      }
    }

    #endregion

    #region Play

    public void Play()
    {
      Task.Run(() =>
      {
        if (IsPlayFnished)
        {
          PlayNext(0, true);
        }
        else
        {
          if (ActualItem != null)
          {
            if (IsPlaying)
            {
              SetVlcMedia(ActualItem.Model);
            }

            VlcControl.SourceProvider.MediaPlayer.Play();
          }
        }
      });
    }

    #endregion

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
    
    #region PlayPause

    public void PlayPause()
    {
      Task.Run(() =>
      {
        if (IsPlaying)
          VlcControl.SourceProvider.MediaPlayer.Pause();
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

      PlayNext(actualItemIndex);
    }

    #endregion

    #region Stop

    public void Stop()
    {
      VlcControl.SourceProvider.MediaPlayer.Stop();
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
      if (!SavePlaylist(true))
      {
        ActualSavedPlaylist.IsUserCreated = true;
        UpdateActualSavedPlaylistPlaylist();
        RaisePropertyChanged(nameof(ActualSavedPlaylist));
      }
    }

    #endregion

    #region SavePlaylist

    public bool SavePlaylist(bool isUserCreated = false, bool editSaved = false)
    {
      var playlistModels = new List<TPlaylistItemModel>();

      for (int i = 0; i < PlayList.Count; i++)
      {
        var song = PlayList[i];

        var newItem = GetNewPlaylistItemViewModel(song, i);

        playlistModels.Add(newItem);
      }

      var songIds = PlayList.Select(x => x.Model.Id);

      var hashCode = songIds.GetSequenceHashCode();

      var entityPlayList = GetNewPlaylistModel(playlistModels, isUserCreated);

      entityPlayList.HashCode = hashCode;

      bool success = false;

      if (editSaved && ActualSavedPlaylist.IsUserCreated)
      {
        if (hashCode != ActualSavedPlaylist.HashCode)
        {
          ActualSavedPlaylist.HashCode = hashCode;
          ActualSavedPlaylist.PlaylistItems = playlistModels;
          ActualSavedPlaylist.ItemCount = playlistModels.Count;
        }

        UpdateActualSavedPlaylistPlaylist();
      }
      else if (!ActualSavedPlaylist.IsUserCreated)
      {
        success = storageManager.StorePlaylist(entityPlayList, out var entityPlaylistDb);

        UpdateNonUserCreatedPlaylist(entityPlayList, entityPlaylistDb);

        ActualSavedPlaylist = entityPlayList;

        if (!success)
        {
          ActualSavedPlaylist.LastPlayed = DateTime.Now;
          UpdateActualSavedPlaylistPlaylist();
        }
      }

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

    #region GetNewPlaylistItemViewModel

    protected abstract TPlaylistItemModel GetNewPlaylistItemViewModel(TItemViewModel itemViewModel, int index);

    #endregion

    #region UpdateActualSavedPlaylistPlaylist

    protected void UpdateActualSavedPlaylistPlaylist()
    {
      storageManager.UpdateData(ActualSavedPlaylist);
    }

    #endregion

    #region IsInFind

    protected bool IsInFind(string original, string phrase)
    {
      return original.ToLower().Contains(phrase) || original.Similarity(phrase) > 0.8;
    }

    #endregion

    #region PlaySongs

    protected void PlayItems(IEnumerable<TItemViewModel> songs, bool savePlaylist = true, int songIndex = 0, bool editSaved = false)
    {
      Application.Current.Dispatcher.Invoke(() =>
      {
        IsActive = true;

        PlayList.Clear();
        PlayList.AddRange(songs);
        ReloadVirtulizedPlaylist();

        IsPlaying = true;

        PlayNext(songIndex);

        if (ActualItem != null)
          ActualItem.IsPlaying = false;

        Task.Run(() =>
        {
          if (savePlaylist)
          {
            SavePlaylist(editSaved: editSaved);
          }
        });

      });
    }

    #region PlayItemsFromEvent

    protected void PlayItemsFromEvent(TPlayItemEventData data)
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
          SavePlaylist(editSaved: true);
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

    private void PlayPlaylist(TPlayItemEventData data, int? lastSongIndex = null)
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
          VlcControl.SourceProvider.MediaPlayer.Position = data.SetPostion.Value;
      }
    }

    #endregion

    #region OnPlayEvent

    protected virtual void OnPlayEvent()
    {

    }

    #endregion

    #region Dispose

    public override void Dispose()
    {
      if (ActualSavedPlaylist != null)
        UpdateActualSavedPlaylistPlaylist();

      try
      {
        VlcControl.SourceProvider.Dispose();
        VlcControl.Dispose();
        
      }
      catch (Exception ex)
      {

        throw;
      }

      base.Dispose();

    }

    #endregion

    #region RemoveItemsFromPlaylist

    private void RemoveItemsFromPlaylist(RemoveFromPlaylistEventArgs<TItemViewModel> obj)
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

          SavePlaylist(editSaved: true);

          break;
        case DeleteType.AlbumFromPlaylist:
          OnRemoveItemsFromPlaylist(DeleteType.AlbumFromPlaylist,obj);

          break;
        default:
          throw new ArgumentOutOfRangeException();
      }
      

      ReloadVirtulizedPlaylist();

      if (obj.ItemsToRemove.Count(x => x.Model.Id == ActualItem.Model.Id) > 0)
      {
        ActualItem = null;
      }

      PlayNext(actualItemIndex);
    }

    #endregion

    #region PlaySongFromPlayList

    private void PlayItemFromPlayList(TItemViewModel viewModel)
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

   
    protected abstract void OnRemoveItemsFromPlaylist(DeleteType deleteType, RemoveFromPlaylistEventArgs<TItemViewModel> args);
    protected abstract void ItemsRemoved(EventPattern<TItemViewModel> eventPattern);
    protected abstract void FilterByActualSearch(string predictate);
    protected abstract TPlaylistModel GetNewPlaylistModel(List<TPlaylistItemModel> playlistModels, bool isUserCreated);

    #endregion
  }
}
