using System;
using System.Collections.Generic;
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
using FFMpegCore;
using FFMpegCore.Exceptions;
using LibVLCSharp.Shared;
using Logger;
using Ninject;
using Prism.Events;
using SoundManagement;
using VCore;
using VCore.ItemsCollections;
using VCore.Standard;
using VCore.Standard.Factories.ViewModels;
using VCore.Standard.Helpers;
using VCore.Standard.Modularity.Interfaces;
using VCore.Standard.ViewModels.WindowsFile;
using VCore.WPF.Interfaces.Managers;
using VCore.WPF.Managers;
using VCore.WPF.Misc;
using VCore.WPF.Modularity.RegionProviders;
using VCore.WPF.ViewModels.Prompt;
using VFfmpeg;
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
    where TPlaylistItemModel : class, IItemInPlaylist<TModel>
    where TPopupViewModel : FileItemSliderPopupDetailViewModel<TModel>
  {
    protected readonly IViewModelsFactory viewModelsFactory;
    private readonly IVFfmpegProvider iVFfmpegProvider;
    private long lastTimeChangedMs;

    protected FilePlayableRegionViewModel(
      IRegionProvider regionProvider,
      IKernel kernel,
      ILogger logger,
      IStorageManager storageManager,
      IEventAggregator eventAggregator,
      IWindowManager windowManager,
      IStatusManager statusManager,
      IViewModelsFactory viewModelsFactory,
      IVFfmpegProvider iVFfmpegProvider,
      VLCPlayer vLCPlayer) : base(regionProvider, kernel, logger, storageManager, eventAggregator, statusManager, windowManager, vLCPlayer)
    {
      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));
      this.iVFfmpegProvider = iVFfmpegProvider ?? throw new ArgumentNullException(nameof(iVFfmpegProvider));
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

    public double ActualPercItemPlaylist
    {
      get
      {
        return PlayList.Count > 0 ? ((double)(ActualItemIndex + 1) / PlayList.Count) * 100.0 : 0;
      }
    }

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

        ReloadMediaPlayer();

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
        AddMissingFilesFromFolder();
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

    private CancellationTokenSource GetCTSAndCancel()
    {
      cTSOnActualItemChangeds.ForEach(x => x.Cancel());

      var cTsOnActualItemChanged = new CancellationTokenSource();

      cTSOnActualItemChangeds.Add(cTsOnActualItemChanged);

      return cTsOnActualItemChanged;
    }

    #region OnNewItemPlay

    private List<CancellationTokenSource> cTSOnActualItemChangeds = new List<CancellationTokenSource>();

    public override void OnNewItemPlay(TModel model)
    {
      base.OnNewItemPlay(model);
      RaisePropertyChanged(nameof(ActualPercItemPlaylist));
      RaisePropertyChanged(nameof(TotalPlaylistDuration));

      if (MediaPlayer.Media != null)
      {
        DetailViewModel = viewModelsFactory.Create<TPopupViewModel>(ActualItem.Model);

        MediaPlayer.Media.DurationChanged += Media_DurationChanged;

        Task.Run(async () =>
        {
          await GetMediaInfo(model);
          await DownloadItemInfo(GetCTSAndCancel().Token);
        });
      }
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

          if (deltaTimeChanged < 0 || deltaTimeChanged > 10000)
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

    protected virtual void Media_DurationChanged(object sender, MediaDurationChangedArgs e)
    {
      Application.Current.Dispatcher.Invoke(async () =>
      {
        await ChangeDuration(e.Duration);

        if (DetailViewModel == null || DetailViewModel.TotalTime == new TimeSpan(0))
        {
          if (ActualItem.Model.Duration == 0)
          {
            ActualItem.Model.Duration = ((int)e.Duration) / 1000;
          }

          DetailViewModel = viewModelsFactory.Create<TPopupViewModel>(ActualItem.Model);
        }

        RaisePropertyChanged(nameof(TotalPlaylistDuration));
      });
    }

    #endregion

    #region ChangeDuration

    private async Task ChangeDuration(float duration)
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

    private bool setLastPosition = false;
    protected override void OnPlayPlaylist(PlayItemsEventData<TItemViewModel> data)
    {
      setLastPosition = false;

      RaisePropertyChanged(nameof(TotalPlaylistDuration));

      base.OnPlayPlaylist(data);

      if (data.EventAction == EventAction.PlayFromPlaylistLast)
      {
        setLastPosition = true;
      }

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

        if (position != null && position > 0 && ActualItem != null)
          ItemLastTime = (int)(position.Value * ActualItem.Duration);
      });
    }

    #endregion

    #region SetLastPosition

    private void SetLastPosition(TPlaylistModel playlist)
    {
      var lastTime = GetLastItemElapsed(playlist);

      if (lastTime > 0.05)
      {
        if (ActualItem != null)
        {
          Application.Current.Dispatcher.Invoke(() =>
          {
            MediaPlayer.Position = lastTime.Value;
            ActualItem.ActualPosition = lastTime.Value;
          });
        }
      }
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

    protected virtual async Task DownloadItemInfo(CancellationToken cancellationToken)
    {
      RaisePropertyChanged(nameof(TotalPlaylistDuration));

      var itemsToUpdate = PlayList.Where(x => x.Created == default).ToList();

      foreach (var item in itemsToUpdate.Where(x => !string.IsNullOrEmpty(x.Model?.Source)
                                                    && !x.Model.Source.Contains("https://")
                                                    && !x.Model.Source.Contains("http://")))
      {
        item.Created = File.GetCreationTime(item.Model.Source);
        item.Modified = File.GetLastWriteTime(item.Model.Source);
      }


      itemsToUpdate = PlayList.Where(x => x.Duration == 0).ToList();

      foreach (var item in itemsToUpdate.Where(x => !string.IsNullOrEmpty(x.Model?.Source)
                                               && !x.Model.Source.Contains("https://")
                                               && !x.Model.Source.Contains("http://")))
      {
        try
        {
          var mediaInfo = await FFProbe.AnalyseAsync(item.Model.Source);

          item.Duration = (int)mediaInfo.Duration.TotalSeconds;
        }
        catch (Exception ex)
        {
        }
      }

      if (itemsToUpdate.Count > 0)
        await storageManager.UpdateEntitiesAsync(itemsToUpdate.Select(x => x.Model));
    }

    #endregion

    #region BeforePlayEvent

    protected override async Task BeforePlayEvent(PlayItemsEventData<TItemViewModel> data)
    {
      await base.BeforePlayEvent(data);

      Application.Current?.Dispatcher?.Invoke(() =>
      {
        CheckedFiles.Clear();
      });
    }

    #endregion

    #region BeforeClearPlaylist

    protected override void BeforeClearPlaylist()
    {
      Application.Current?.Dispatcher?.Invoke(() =>
      {
        CheckedFiles.Clear();
      });

      base.BeforeClearPlaylist();
    }

    #endregion

    #region PlayPlaylist

    protected override void PlayPlaylist(PlayItemsEventData<TItemViewModel> data, int? lastSongIndex = null, bool onlySet = false)
    {
      base.PlayPlaylist(data, lastSongIndex, onlySet);

      Task.Run(async () =>
      {
        await DownloadItemInfo(GetCTSAndCancel().Token);
      });

      if (ActualSavedPlaylist.WatchFolder)
      {
        AddMissingFilesFromFolder();
      }
    }

    #endregion

    #region AddMissingFilesFromFolder

    private void AddMissingFilesFromFolder()
    {
      var allFiles = PlayList.Where(x => x.Model != null).Select(x => x.Model.Source).ToList();
      var folders = allFiles.Select(x => Path.GetDirectoryName(x)).Distinct().ToList();
      var parentFolder = folders.Select(x => new DirectoryInfo(x).Parent?.FullName).Distinct().ToList();

      if (parentFolder.Count == 1 && folders.Count > 1)
      {
        if (parentFolder[0] != null)
        {
          folders.Clear();
          folders.Add(parentFolder[0]);
        }
      }

      foreach (var folder in folders)
      {
        var files = Directory.GetFiles(folder, "*", SearchOption.AllDirectories);

        var missingFilesInPlaylist = files.Where(x =>
        {
          var fileType = Path.GetExtension(x).GetFileType();

          return fileType == FileType.Sound || fileType == FileType.Video;
        }).Where(x => !allFiles.Contains(x)).ToList();

        foreach (var file in missingFilesInPlaylist)
        {
          var newModel = viewModelsFactory.Create<TModel>();
          var fileInfo = new FileInfo(file);

          newModel.Source = fileInfo.FullName;
          newModel.Name = fileInfo.Name;

          var existing = storageManager.GetRepository<TModel>()
            .FirstOrDefault(x => x.Source == newModel.Source);

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

    #endregion

    #region BeforeDeleteFile

    protected override void BeforeDeleteFile(TItemViewModel itemViewModel)
    {
      base.BeforeDeleteFile(itemViewModel);

      if (DetailViewModel.Model == itemViewModel.Model)
      {
        DetailViewModel.Dispose();
      }
    }

    #endregion

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

    #region GetMediaInfo

    protected virtual Task GetMediaInfo(TModel model)
    {
      return Task.CompletedTask;
    }

    #endregion

    #region OnResetAllData

    public override async Task<bool> OnResetAllData()
    {
      CheckedFiles.Clear();

      var result = await base.OnResetAllData();

      return result;
    }

    #endregion

    protected override void OnPlay()
    {
      base.OnPlay();

      if (setLastPosition)
      {
        SetLastPosition(ActualSavedPlaylist);
        setLastPosition = false;
      }
    }

    protected override void SortPlaylist(PlaylistSortOrder playlistSort)
    {
      base.SortPlaylist(playlistSort);

      switch (playlistSort)
      {
        case PlaylistSortOrder.Created:
          PlayList.Sort((x, y) => x.Created.CompareTo(y.Created));
          break;
        case PlaylistSortOrder.Modified:
          PlayList.Sort((x, y) => x.Modified.CompareTo(y.Modified));
          break;
      }
    }

    #region Dispose

    public override void Dispose()
    {
      base.Dispose();

      MediaPlayer.TimeChanged -= OnVlcTimeChanged;
      PlayList.CollectionChanged -= PlayList_CollectionChanged;

      cTSOnActualItemChangeds.ForEach(x => x.Cancel());
    }

    #endregion

    protected override void PlayItems(IEnumerable<TItemViewModel> items, bool savePlaylist = true, int songIndex = 0, bool editSaved = false, bool onlyItemSet = false)
    {
      base.PlayItems(items, savePlaylist, songIndex, editSaved, onlyItemSet);

      if (savePlaylist)
      {
        setLastPosition = true;
        HandleLastItemElapsed();
      }
    }

    protected override void OnStoredPlaylistLoaded()
    {
      base.OnStoredPlaylistLoaded();

      IsRepeate = ActualSavedPlaylist.IsReapting;
      IsShuffle = ActualSavedPlaylist.IsShuffle;
    }

    #endregion

  }

  public interface ISliderPopupViewModel
  {
    double ActualSliderValue { get; set; }
    double MaxValue { get; set; }
    bool IsPopupOpened { get; set; }
    bool DisablePopup { get; set; }
  }

  public class FileItemSliderPopupDetailViewModel<TModel> : ViewModel<TModel>, ISliderPopupViewModel
    where TModel : IFilePlayableModel
  {
    protected Subject<Unit> refreshSubject = new Subject<Unit>();
    public FileItemSliderPopupDetailViewModel(TModel model) : base(model)
    {
      TotalTime = TimeSpan.FromSeconds(model.Duration);
      refreshSubject.Throttle(TimeSpan.FromMilliseconds(5)).Subscribe(x =>
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

    #region IsPopupOpened

    private bool isPopupOpened;

    public bool IsPopupOpened
    {
      get { return isPopupOpened; }
      set
      {
        if (value != isPopupOpened)
        {
          isPopupOpened = value;
          RaisePropertyChanged();
        }
      }
    }



    #region DisablePopup

    private bool disablePopup;

    public bool DisablePopup
    {
      get { return disablePopup; }
      set
      {
        if (value != disablePopup)
        {
          disablePopup = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #endregion

    protected virtual void Refresh()
    {
      RaisePropertyChanged(nameof(ActualTime));
    }

    #region GetEmptyImage

    protected byte[] GetEmptyImage()
    {
      var stream = new MemoryStream();
      var emptyImage = new Bitmap(10, 10, PixelFormat.Format24bppRgb);
      emptyImage.Save(stream, ImageFormat.Jpeg);

      return stream.ToArray();
    }

    #endregion

  }
}