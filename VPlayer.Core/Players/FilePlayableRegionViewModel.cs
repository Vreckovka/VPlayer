using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using LibVLCSharp.Shared;
using Logger;
using Ninject;
using Prism.Events;
using SoundManagement;
using VCore;
using VCore.ItemsCollections;
using VCore.Standard;
using VCore.Standard.Factories.ViewModels;
using VCore.Standard.Modularity.Interfaces;
using VCore.WPF.Interfaces.Managers;
using VCore.WPF.Managers;
using VCore.WPF.Misc;
using VCore.WPF.Modularity.RegionProviders;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Events;
using VPlayer.Core.Managers.Status;
using VPlayer.Core.ViewModels.SoundItems;
using VPlayer.IPTV.ViewModels;
using VPlayer.WindowsPlayer.Players;
using VVLC.Players;

namespace VPlayer.Core.ViewModels
{
  public abstract class FilePlayableRegionViewModel<TView, TItemViewModel, TPlaylistModel, TPlaylistItemModel, TModel, TPopupViewModel> :
    PlayableRegionViewModel<TView, TItemViewModel, TPlaylistModel, TPlaylistItemModel, TModel>, IFilePlayableRegionViewModel
    where TView : class, IView
    where TItemViewModel : class, IFileItemInPlayList<TModel>, IDisposable
    where TModel : class, IFilePlayableModel, IUpdateable<TModel>
    where TPlaylistModel : class, IFilePlaylist<TPlaylistItemModel>, new()
    where TPlaylistItemModel : IItemInPlaylist<TModel>
  where TPopupViewModel : FileItemSliderPopupDetailViewModel<TModel>
  {
    protected readonly IViewModelsFactory viewModelsFactory;
    private readonly VLCPlayer vLcPlayer;
    private long lastTimeChangedMs;
    private PlayItemsEventData<TItemViewModel> actualPlaylistData;

    protected FilePlayableRegionViewModel(
      IRegionProvider regionProvider,
      IKernel kernel,
      ILogger logger,
      IStorageManager storageManager,
      IEventAggregator eventAggregator,
      IWindowManager windowManager,
      IStatusManager statusManager,
      IViewModelsFactory viewModelsFactory,
      VLCPlayer vLCPlayer) : base(regionProvider, kernel, logger, storageManager, eventAggregator, statusManager, windowManager, vLCPlayer)
    {
      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));
      vLcPlayer = vLCPlayer ?? throw new ArgumentNullException(nameof(vLCPlayer));
      BufferingSubject.Throttle(TimeSpan.FromSeconds(0.5)).Subscribe(x =>
      {
        IsBuffering = x;
      });
    }

    #region Properties

    #region TotalPlaylistDuration

    public TimeSpan TotalPlaylistDuration
    {
      get { return TimeSpan.FromSeconds(PlayList.Sum(x => x.Duration)); }
    }

    #endregion

    protected ReplaySubject<bool> BufferingSubject { get; } = new ReplaySubject<bool>(1);

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

    #region CheckedFiles

    private ObservableCollection<TItemViewModel> checkedFiles = new ObservableCollection<TItemViewModel>();

    public ObservableCollection<TItemViewModel> CheckedFiles
    {
      get { return checkedFiles; }
      set
      {
        if (value != checkedFiles)
        {
          checkedFiles = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region DetailViewModel

    private TPopupViewModel detailViewModelar;

    public TPopupViewModel DetailViewModel
    {
      get { return detailViewModelar; }
      set
      {
        if (value != detailViewModelar)
        {
          detailViewModelar = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region WasAllItemsProcessed

    private bool wasAllItemsProcessed;

    public virtual bool WasAllItemsProcessed
    {
      get { return wasAllItemsProcessed; }
      set
      {
        if (value != wasAllItemsProcessed)
        {
          wasAllItemsProcessed = value;

          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #endregion

    #region Commands

    #region ReloadFile

    private ActionCommand reloadFile;

    public ICommand ReloadFile
    {
      get
      {
        if (reloadFile == null)
        {
          reloadFile = new ActionCommand(OnReloadFile);
        }

        return reloadFile;
      }
    }

    private float? reloadPosition;
    public async void OnReloadFile()
    {
      if (ActualItem != null)
      {
        reloadPosition = ActualItem.ActualPosition;

        MediaPlayer.Reload();

        await SetMedia(ActualItem.Model);

        if (IsPlaying)
        {
          IsPlayFnished = false;
          await Play();
        }
      }
    }

    #endregion

    #region WatchFolderPlaylistCommand

    private ActionCommand watchFolderPlaylistCommand;

    public ICommand WatchFolderPlaylistCommand
    {
      get
      {
        if (watchFolderPlaylistCommand == null)
        {
          watchFolderPlaylistCommand = new ActionCommand(OnWatchFolderPlaylistCommand);
        }

        return watchFolderPlaylistCommand;
      }
    }

    public void OnWatchFolderPlaylistCommand()
    {
      ActualSavedPlaylist.WatchFolder = !ActualSavedPlaylist.WatchFolder;

      if (ActualSavedPlaylist.WatchFolder)
      {
        AddMissingFilesFromFolder(actualPlaylistData);
      }
       
      UpdateOrAddActualSavedPlaylist();
    }

    #endregion

    #endregion

    #region Methods

    #region InitializeAsync

    protected override void InitializeAsync()
    {
      PlayList.CollectionChanged += PlayList_CollectionChanged;

      positionChangedSubject.Throttle(TimeSpan.FromMilliseconds(1000)).Subscribe(position =>
      {
        MediaPlayer.TimeChanged += OnVlcTimeChanged;
      });

      base.InitializeAsync();
    }

    #endregion

    #region HookToVlcEvents

    protected override void HookToPlayerEvents()
    {
      base.HookToPlayerEvents();

      MediaPlayer.TimeChanged += OnVlcTimeChanged;
      MediaPlayer.Buffering += MediaPlayer_Buffering;
    }

    #endregion

    #region OnNewItemPlay

    private CancellationTokenSource cTSOnActualItemChanged;
    public override void OnNewItemPlay(TModel model)
    {
      base.OnNewItemPlay(model);

      if (MediaPlayer.Media != null)
      {
        MediaPlayer.Media.DurationChanged += Media_DurationChanged;

        cTSOnActualItemChanged?.Cancel();
        cTSOnActualItemChanged = new CancellationTokenSource();

        Task.Run(() =>
        {
          return DownloadItemInfo(cTSOnActualItemChanged.Token);
        });
      }

      DetailViewModel?.Dispose();
      DetailViewModel = viewModelsFactory.Create<TPopupViewModel>(model);
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

    private async void OnVlcTimeChanged(object sender, PlayerTimeChangedArgs eventArgs)
    {
      if (ActualItem != null)
      {
        if (ActualItem.Duration <= 0)
        {
          ChangeDuration(MediaPlayer.Media.Duration);
        }

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

          var totalPlayedTime = TimeSpan.FromMilliseconds(deltaTimeChanged);

          PlaylistTotalTimePlayed += totalPlayedTime;

          int totalSec = (int)PlaylistTotalTimePlayed.TotalSeconds;

          if (totalSec % 10 == 0 && totalSec > lastTotalTimeSaved)
          {
            lastTotalTimeSaved = totalSec;

            await UpdateActualSavedPlaylistPlaylist();
          }
        }
      }
    }

    #endregion

    #region Position methods

    #region SetMediaPosition

    private object positionBatton = new object();
    private Subject<bool> positionChangedSubject = new Subject<bool>();

    public void SetMediaPosition(float position)
    {
      lock (positionBatton)
      {
        MediaPlayer.TimeChanged -= OnVlcTimeChanged;

        positionChangedSubject.OnNext(true);

        if (ActualItem == null)
        {
          return;
        }

        if (position < 0)
        {
          position = 0;
        }

        MediaPlayer.Position = position;
        ActualItem.ActualPosition = MediaPlayer.Position;
        lastTimeChangedMs = (long)(ActualItem.ActualPosition * (double)ActualItem.Duration) * 1000;

        positionChangedSubject.OnNext(false);
      }
    }

    #endregion

    #region SeekForward

    public void SeekForward(int seekSize = 50)
    {
      var position = ActualItem.ActualPosition + GetSeekSize(seekSize);

      SetMediaPosition(position);
    }

    #endregion

    #region SeekBackward

    public void SeekBackward(int seekSize = 50)
    {
      var position = ActualItem.ActualPosition - GetSeekSize(seekSize);

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
        BufferingSubject.OnNext(true);
      else
      {
        if (MediaPlayer.Media != null)
        {
          var dur = MediaPlayer.Media.Duration;
        }

        BufferingSubject.OnNext(false);
      }

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
        ActualItem.ActualPosition = MediaPlayer.Position;
      }
    }

    #endregion

    #region UpdatePlaylist

    protected override void UpdatePlaylist(TPlaylistModel playlistToUpdate, TPlaylistModel other)
    {
      base.UpdatePlaylist(playlistToUpdate, other);

      playlistToUpdate.Name = other.Name;
      playlistToUpdate.IsReapting = other.IsReapting;
      playlistToUpdate.IsShuffle = other.IsShuffle;
    }

    #endregion

    #region Media_DurationChanged

    protected void Media_DurationChanged(object sender, MediaDurationChangedArgs e)
    {
      Application.Current.Dispatcher.Invoke(async () =>
      {
        ChangeDuration(e.Duration);
      });
    }

    #endregion

    #region ChangeDuration

    private async void ChangeDuration(float duration)
    {
      if (duration != 0 && ActualItem != null)
      {
        ActualItem.Duration = (int)duration / 1000;

        if (MediaPlayer.Media != null)
          MediaPlayer.Media.DurationChanged -= Media_DurationChanged;

        await storageManager.UpdateEntityAsync(ActualItem.Model);

        if (ActualItem != null && reloadPosition != null)
        {
          MediaPlayer.Position = reloadPosition.Value;

          reloadPosition = null;
        }

        if (DetailViewModel?.Model != null && MediaPlayer?.Media != null)
        {
          DetailViewModel.Model.Duration = ((int)MediaPlayer.Media.Duration) / 1000;
          DetailViewModel.TotalTime = TimeSpan.FromSeconds(DetailViewModel.Model.Duration);
        }

        RaisePropertyChanged(nameof(TotalPlaylistDuration));
      }
    }

    #endregion

    #region OnPlayPlaylist

    protected override void OnPlayPlaylist(PlayItemsEventData<TItemViewModel> data)
    {
      base.OnPlayPlaylist(data);

      HandleLastItemElapsed();
    }

    #endregion

    #region HandleLastItemElapsed

    private void HandleLastItemElapsed()
    {
      Application.Current.Dispatcher.Invoke(() =>
      {
        ItemLastTime = null;

        var position = GetLastItemElapsed(ActualSavedPlaylist);

        if (position != null && position > 0)
          ItemLastTime = (int)(position.Value * ActualItem.Duration);
      });
    }

    #endregion

    #region GetLastItemElapsed

    protected float? GetLastItemElapsed(TPlaylistModel playlistModel)
    {
      return playlistModel.LastItemElapsedTime > 0 ? (float?)playlistModel.LastItemElapsedTime : null;
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

    #region OnActualItemChanged

    protected override void OnActualItemChanged()
    {
      base.OnActualItemChanged();


      if (ActualItem != null)
        ActualItem.ActualPosition = 0;
    }

    #endregion

    #region DownloadItemInfo

    protected virtual Task DownloadItemInfo(CancellationToken cancellationToken)
    {
      return Task.CompletedTask;
    }

    #endregion

    protected override async Task BeforePlayEvent(PlayItemsEventData<TItemViewModel> data)
    {
      await base.BeforePlayEvent(data);

      Application.Current?.Dispatcher?.Invoke(() =>
      {
        CheckedFiles.Clear();
      });

    }

    protected override void BeforeClearPlaylist()
    {
      Application.Current?.Dispatcher?.Invoke(() =>
      {
        CheckedFiles.Clear();
      });

      base.BeforeClearPlaylist();
    }

    protected override void PlayPlaylist(PlayItemsEventData<TItemViewModel> data, int? lastSongIndex = null, bool onlySet = false)
    {
      actualPlaylistData = data;
      base.PlayPlaylist(data, lastSongIndex, onlySet);

      if (ActualSavedPlaylist.WatchFolder)
      {
        AddMissingFilesFromFolder(data);
      }
    }

    private void AddMissingFilesFromFolder(PlayItemsEventData<TItemViewModel> data)
    {
      var allFiles = data.Items?.Where(x => x.Model != null).Select(x => x.Model.Source).ToList();
      var folders = allFiles.Select(x => Path.GetDirectoryName(x)).Distinct().ToList();

      if (folders?.Count == 1)
      {
        var folder = folders.Single();
        var files = Directory.GetFiles(folder);

        var missingFilesInPlaylist = files.Where(x => !allFiles.Contains(x)).ToList();

        foreach (var file in missingFilesInPlaylist)
        {
          var newModel = viewModelsFactory.Create<TModel>();
          var fileInfo = new FileInfo(file);

          newModel.Source = fileInfo.FullName;
          newModel.Name = fileInfo.Name;

          var existing = storageManager.GetRepository<TModel>()
            .SingleOrDefault(x => x.Source == newModel.Source);

          if (existing == null)
          {
            storageManager.StoreEntity(newModel, out existing);
          }
       
          PlayList.Add(viewModelsFactory.Create<TItemViewModel>(existing));
        }

        if (missingFilesInPlaylist.Any())
        {
          RequestReloadVirtulizedPlaylist();
          StorePlaylist(PlayList.ToList());
        }
      }
    }

    #region MarkViewModelAsChecked

    protected void MarkViewModelAsChecked(TItemViewModel itemViewModel)
    {
      Application.Current?.Dispatcher?.InvokeAsync(() =>
      {
        if (!CheckedFiles.Contains(itemViewModel))
        {
          CheckedFiles.Add(itemViewModel);
        }
      });
    }

    #endregion

    #region ClearPlaylist

    public override Task ClearPlaylist()
    {
      DetailViewModel?.Dispose();

      return base.ClearPlaylist();
    }

    #endregion

    #region Dispose

    public override void Dispose()
    {
      base.Dispose();

      MediaPlayer.TimeChanged -= OnVlcTimeChanged;
      PlayList.CollectionChanged -= PlayList_CollectionChanged;

      cTSOnActualItemChanged?.Cancel();
    }

    #endregion

    #endregion

  }

  public interface ISliderPopupViewModel
  {
    double ActualSliderValue { get; set; }
    double MaxValue { get; set; }
  }

  public class FileItemSliderPopupDetailViewModel<TModel> : ViewModel<TModel>, ISliderPopupViewModel
    where TModel : IFilePlayableModel
  {
    protected Subject<Unit> refreshSubject = new Subject<Unit>();
    public FileItemSliderPopupDetailViewModel(TModel model) : base(model)
    {
      TotalTime = TimeSpan.FromSeconds(model.Duration);
      refreshSubject.Throttle(TimeSpan.FromMilliseconds(15)).Subscribe(x =>
      {
        Refresh();
      });
    }


    #region Image

    private byte[] image;

    public byte[] Image
    {
      get { return image; }
      set
      {
        if (value != image)
        {
          image = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region ActualTime

    public TimeSpan ActualTime
    {
      get
      {
        if (MaxValue > 0)
          return TimeSpan.FromMilliseconds(TotalTime.TotalMilliseconds * ActualSliderValue / MaxValue);

        return TimeSpan.Zero;
      }

    }

    #endregion

    #region TotalTime

    private TimeSpan totalTime;

    public TimeSpan TotalTime
    {
      get { return totalTime; }
      set
      {
        if (value != totalTime)
        {
          totalTime = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region ActualSliderValue

    private double actualSliderValue;

    public double ActualSliderValue
    {
      get { return actualSliderValue; }
      set
      {
        if (value != actualSliderValue)
        {
          actualSliderValue = value;
          RaisePropertyChanged();
          refreshSubject.OnNext(Unit.Default);
        }
      }
    }
    #endregion

    #region MaxValue

    private double maxValue = 1;

    public double MaxValue
    {
      get { return maxValue; }
      set
      {
        if (value != maxValue)
        {
          maxValue = value;
          RaisePropertyChanged();
          refreshSubject.OnNext(Unit.Default);
        }
      }
    }

    #endregion


    protected virtual void Refresh()
    {
      RaisePropertyChanged(nameof(ActualTime));
    }

    #region GetEmptyImage

    protected byte[] GetEmptyImage()
    {
      var stream = new MemoryStream();
      var emptyImage = new Bitmap(10, 10, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
      emptyImage.Save(stream, ImageFormat.Jpeg);

      return stream.ToArray();
    }

    #endregion
  }
}