using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using FFMpegCore;
using Logger;
using Microsoft.EntityFrameworkCore;
using Microsoft.WindowsAPICodePack.Dialogs;
using Ninject;
using PCloudClient.Domain;
using Prism.Events;
using VCore.Standard;
using VCore.Standard.Factories.ViewModels;
using VCore.Standard.Helpers;
using VCore.Standard.Modularity.Interfaces;
using VCore.Standard.Providers;
using VCore.Standard.ViewModels.WindowsFile;
using VCore.WPF;
using VCore.WPF.Interfaces.Managers;
using VCore.WPF.Misc;
using VCore.WPF.Modularity.RegionProviders;
using VFfmpeg;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.DomainClasses.Video;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Events;
using VPlayer.Core.Managers.Status;
using VPlayer.Core.ViewModels;
using VPlayer.Core.ViewModels.SoundItems;
using VPLayer.Domain;
using VPlayer.WindowsPlayer.Players;
using VVLC.Players;
using FileInfo = System.IO.FileInfo;

namespace VPlayer.Core.Players
{
  public abstract class FilePlayableRegionViewModel<TView, TItemViewModel, TPlaylistModel, TPlaylistItemModel, TModel, TPopupViewModel> :
    PlayableRegionViewModel<TView, TItemViewModel, TPlaylistModel, TPlaylistItemModel, TModel>, IFilePlayableRegionViewModel
    where TView : class, IView
    where TItemViewModel : class, IFileItemInPlayList<TModel>, IDisposable
    where TModel : PlayableItem, IFilePlayableModel, IUpdateable<TModel>
    where TPlaylistModel : class, IFilePlaylist<TPlaylistItemModel>, new()
    where TPlaylistItemModel : class, IItemInPlaylist<TModel>
    where TPopupViewModel : FileItemSliderPopupDetailViewModel<TModel>
  {
    protected readonly IViewModelsFactory viewModelsFactory;
    private readonly IVFfmpegProvider iVFfmpegProvider;
    private readonly ISettingsProvider settingsProvider;
    private readonly IVPlayerCloudService cloudService;
    private long lastTimeChangedMs;
    private Dictionary<TModel, PublicLink> publicLinks = new Dictionary<TModel, PublicLink>();

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
      ISettingsProvider settingsProvider,
      IVPlayerCloudService cloudService,
      VLCPlayer vLCPlayer) : base(regionProvider, kernel, logger, storageManager, eventAggregator, statusManager, viewModelsFactory, windowManager, vLCPlayer)
    {
      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));
      this.iVFfmpegProvider = iVFfmpegProvider ?? throw new ArgumentNullException(nameof(iVFfmpegProvider));
      this.settingsProvider = settingsProvider ?? throw new ArgumentNullException(nameof(settingsProvider));
      this.cloudService = cloudService ?? throw new ArgumentNullException(nameof(cloudService));
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

    private List<TItemViewModel> checkedFiles = new List<TItemViewModel>();

    public List<TItemViewModel> CheckedFiles
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

    public bool WatchFolder
    {
      get
      {
        return !string.IsNullOrEmpty(ActualSavedPlaylist?.WatchedFolder);
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

    private ActionCommand<bool?> watchFolderPlaylistCommand;

    public ICommand WatchFolderPlaylistCommand
    {
      get
      {
        if (watchFolderPlaylistCommand == null)
        {
          watchFolderPlaylistCommand = new ActionCommand<bool?>(OnWatchFolderPlaylistCommand);
        }

        return watchFolderPlaylistCommand;
      }
    }

    protected string GetDefaultFolder()
    {
      var defaultFolder = Path.GetDirectoryName(PlayList.Where(x => x.Model != null).Select(x => x.Model.Source).FirstOrDefault());

      if (defaultFolder != null && defaultFolder.Contains("http"))
      {
        defaultFolder = settingsProvider.GetSetting(GlobalSettings.FileBrowserInitialDirectory)?.Value;
      }

      return defaultFolder;
    }

    public void OnWatchFolderPlaylistCommand(bool? watchFolder)
    {
      if (watchFolder != null)
      {
        if (watchFolder.Value)
        {
          var defaultFolder = ActualSavedPlaylist.WatchedFolder;

          if (string.IsNullOrEmpty(defaultFolder))
          {
            defaultFolder = GetDefaultFolder();
          }

          CommonOpenFileDialog dialog = new CommonOpenFileDialog();

          dialog.IsFolderPicker = true;
          dialog.Title = "Select folder to watch";
          dialog.InitialDirectory = defaultFolder;

          if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
          {
            ActualSavedPlaylist.WatchedFolder = dialog.FileName;

            if (!string.IsNullOrEmpty(ActualSavedPlaylist.WatchedFolder))
            {
              AddMissingFilesFromFolder();
            }
          }
        }
        else
        {
          ActualSavedPlaylist.WatchedFolder = null;
        }
      }
      else
      {
        AddMissingFilesFromFolder();
      }


      UpdateOrAddActualSavedPlaylist();
      RaisePropertyChanged(nameof(WatchFolder));
    }

    #endregion

    #endregion

    #region Methods

    #region InitializeAsync

    protected override void InitializeAsync()
    {
      HookToPlaylistCollectionChanged();

      positionChangedSubject.Throttle(TimeSpan.FromMilliseconds(1000)).Subscribe(position =>
      {
        MediaPlayer.TimeChanged += OnVlcTimeChanged;
      });

      base.InitializeAsync();
    }

    #endregion

    protected void HookToPlaylistCollectionChanged()
    {
      PlayList.CollectionChanged += PlayList_CollectionChanged;
    }

    protected void UnHookToPlaylistCollectionChanged()
    {
      PlayList.CollectionChanged -= PlayList_CollectionChanged;
    }

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
      cTSOnActualItemChangeds.ToList().ForEach(x => x.Cancel());

      var cTsOnActualItemChanged = new CancellationTokenSource();

      cTSOnActualItemChangeds.ToList().Add(cTsOnActualItemChanged);

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

//#if !DEBUG
          ActualItem.Model.TimePlayed += totalPlayedTime;
          PlaylistTotalTimePlayed += totalPlayedTime;
//#endif

          int totalSec = (int)PlaylistTotalTimePlayed.TotalSeconds;

          if (totalSec - lastTotalTimeSaved > 10 && totalSec > lastTotalTimeSaved)
          {
            lastTotalTimeSaved = totalSec;

            //Data race pri CLEAR
            await Task.Delay(500);

            if(ActualItem != null)
            {
              await UpdateActualSavedPlaylistPlaylist();
              await storageManager.UpdateEntityAsync(ActualItem.Model);
              ActualItem.RaiseNotifyPropertyChanged(nameof(ViewModel<TModel>.Model));
            }
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
      VSynchronizationContext.InvokeOnDispatcher(async () =>
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
        var itemDuration = (int)duration / 1000;
        var wasChanged = itemDuration != ActualItem.Duration;
        ActualItem.Duration = itemDuration;

        if (MediaPlayer.Media != null)
          MediaPlayer.Media.DurationChanged -= Media_DurationChanged;

        if (wasChanged)
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
      VSynchronizationContext.PostOnUIThread(() =>
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
          VSynchronizationContext.InvokeOnDispatcher(() =>
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

      var list = PlayList.ToList();

      var validItems = list.Where(x => !string.IsNullOrEmpty(x.Model?.Source)
                                       && !x.Model.Source.Contains("https://")
                                       && !x.Model.Source.Contains("http://")).ToList();

      var cloudItems = list
        .Where(x => x.Model.FileInfoEntity != null)
        .Where(x => long.TryParse(x.Model.FileInfoEntity.Indentificator, out var id)).ToList();


      List<TItemViewModel> changedItems = new List<TItemViewModel>();

      var itemsToUpdate = validItems.Where(x => x.Created == default);

      foreach (var item in itemsToUpdate)
      {
        item.Created = File.GetCreationTime(item.Model.Source);
        item.Modified = File.GetLastWriteTime(item.Model.Source);

        changedItems.Add(item);
      }


      itemsToUpdate = validItems.Where(x => x.Duration == 0);

      foreach (var item in itemsToUpdate)
      {
        try
        {
          var mediaInfo = await FFProbe.AnalyseAsync(item.Model.Source);

          item.Duration = (int)mediaInfo.Duration.TotalSeconds;

          changedItems.Add(item);
        }
        catch (Exception ex)
        {
        }
      }

      if (changedItems.Count > 0)
        await storageManager.UpdateEntitiesAsync(changedItems.Select(x => x.Model));

      await DownloadPublicLinks(cloudItems, cancellationToken);
    }

    #endregion

    #region BeforePlayEvent

    protected override async Task BeforePlayEvent(PlayItemsEventData<TItemViewModel> data)
    {
      await base.BeforePlayEvent(data);

      lastTotalTimeSaved = 0;
      lastTimeChangedMs = 0;

      VSynchronizationContext.InvokeOnDispatcher(() =>
      {
        CheckedFiles.Clear();
      });
    }

    #endregion

    #region BeforeClearPlaylist

    protected override void BeforeClearPlaylist()
    {
      VSynchronizationContext.InvokeOnDispatcher(() =>
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

      if (!string.IsNullOrEmpty(ActualSavedPlaylist.WatchedFolder))
      {
        AddMissingFilesFromFolder();
      }
    }

    #endregion

    #region AddMissingFilesFromFolder

    private void AddMissingFilesFromFolder()
    {
      Task.Run(() =>
      {
        if (!string.IsNullOrEmpty(ActualSavedPlaylist.WatchedFolder))
        {
          var allFiles = PlayList.Where(x => x.Model != null).Select(x => x.Model.Source).ToList();

          var files = Directory.GetFiles(ActualSavedPlaylist.WatchedFolder, "*", SearchOption.AllDirectories);

          var missingFilesInPlaylist = files.Where(x =>
          {
            var fileType = Path.GetExtension(x).GetFileType();

            return fileType == FileType.Sound || fileType == FileType.Video;
          }).Where(x => !allFiles.Contains(x)).ToList();

          foreach (var file in missingFilesInPlaylist)
          {
            var newModel = viewModelsFactory.Create<TModel>();
            var fileInfo = new FileInfo(file);
           
            newModel.FileInfoEntity = new FileInfoEntity()
            {
              Indentificator = fileInfo.FullName,
              FullName = fileInfo.FullName,
              Name = fileInfo.Name,
              Extension = fileInfo.Extension,
              
            };

            newModel.Source = fileInfo.FullName;
            newModel.Name = fileInfo.Name;

            var existing = storageManager.GetTempRepository<TModel>()
              .Include(x => x.FileInfoEntity)
              .Where(x => x.FileInfoEntity != null)
              .FirstOrDefault(x => x.FileInfoEntity.Indentificator == newModel.FileInfoEntity.Indentificator);

            if (existing == null)
            {
              storageManager.StoreEntity(newModel, out existing);
            }

            VSynchronizationContext.InvokeOnDispatcher(() =>
            {
              PlayList.Add(viewModelsFactory.Create<TItemViewModel>(existing));
            });
          }

          if (missingFilesInPlaylist.Any())
          {
            VSynchronizationContext.PostOnUIThread(() =>
            {
              RequestReloadVirtulizedPlaylist();
            });

            StorePlaylist(PlayList.ToList());
          }
        }
      });
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
      if (!CheckedFiles.Contains(itemViewModel))
      {
        CheckedFiles.Add(itemViewModel);

       RaisePropertyChanged(nameof(CheckedFiles)); 
      }
    }

    #endregion

    #region ClearPlaylist

    public override Task ClearPlaylist()
    {
      DetailViewModel?.Dispose();
      lastTotalTimeSaved = 0;
      lastTimeChangedMs = 0;

      return base.ClearPlaylist();
    }

    #endregion

    #region GetMediaInfo

    protected virtual Task GetMediaInfo(TModel model)
    {
      return Task.CompletedTask;
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

    #region BeforeSetMedia

    protected override async Task BeforeSetMedia(TModel model)
    {
      await base.BeforeSetMedia(model);

      await DownloadUrlLink(model);
    }

    #endregion

    #region DownloadUrlLinks

    private async Task DownloadUrlLinks(IEnumerable<TModel> soundItems, CancellationToken cancellationToken)
    {
      var list = soundItems.ToList();
      var onlyNeededList = new List<TModel>();

      foreach (var soundItem in list)
      {
        if (publicLinks.TryGetValue(soundItem, out var storedPublicLink) &&
            storedPublicLink.ExpiresDate > DateTime.Now)
        {
          continue;
        }
        else if (long.TryParse(soundItem.FileInfoEntity.Indentificator, out var parsed))
        {
          onlyNeededList.Add(soundItem);
        }
      }

      if (onlyNeededList.Count == 0)
      {
        return;
      }

      var validIds = onlyNeededList.Select(x => long.Parse(x.FileInfoEntity.Indentificator));

      var getLinksTask = cloudService.GetFileLinks(validIds, cancellationToken);

      var result = await getLinksTask.Process;

      if (result != null)
      {
        foreach (var keyPair in result)
        {
          var originalItem = onlyNeededList.FirstOrDefault(x => x.FileInfoEntity.Indentificator == keyPair.Key.ToString());
          var publicLink = keyPair.Value;

          if (originalItem != null)
          {
            if (publicLinks.TryGetValue(originalItem, out var storedPublicLink) &&
                storedPublicLink.ExpiresDate > DateTime.Now)
            {
              continue;
            }

            if (publicLink != null)
            {
              var oldLink = originalItem.FileInfoEntity.Source;

              if (publicLink.Link != oldLink)
              {
                originalItem.FileInfoEntity.Source = publicLink.Link;
              }

              if (storedPublicLink != null)
              {
                publicLinks[originalItem] = publicLink;
              }
              else
              {
                publicLinks.Add(originalItem, publicLink);
              }
            }
          }
        }
      }
    }

    #endregion

    #region DownloadUrlLink

    private async Task DownloadUrlLink(TModel soundItem)
    {
      if (soundItem != null &&
          soundItem.FileInfoEntity != null)
      {
        //Asi docasny fix pre pokazene itemy
        if(soundItem.FileInfoEntity.Indentificator == null)
        {
          soundItem.FileInfoEntity.Indentificator = soundItem.FileInfoEntity.Source;
        }

        if (long.TryParse(soundItem.FileInfoEntity.Indentificator, out var id))
        {
          if (publicLinks.TryGetValue(soundItem, out var storedPublicLink) &&
              storedPublicLink.ExpiresDate > DateTime.Now)
          {
            return;
          }

          PublicLink publicLink = null;

          try
          {
             publicLink = await cloudService.GetFileLink(id);
          }
          catch (Exception)
          {
            publicLink = await cloudService.GetFileLink(id);
          }

          if (publicLink != null)
          {
            var oldLink = soundItem.FileInfoEntity.Source;

            if (publicLink.Link != oldLink)
            {
              soundItem.FileInfoEntity.Source = publicLink.Link;
            }

            if (storedPublicLink != null)
            {
              publicLinks[soundItem] = publicLink;
            }
            else
            {
              publicLinks.Add(soundItem, publicLink);
            }
          }
        }
      }
    }

    #endregion

    #region DownloadPublicLinks

    private Task DownloadPublicLinks(IEnumerable<TItemViewModel> validItems, CancellationToken cancellationToken)
    {
      return Task.Run(async () =>
      {
        try
        {
          var validItemsToUpdate = GetValidItemsForCloud(validItems).ToList();

          var itemsAfter = validItemsToUpdate.Skip(actualItemIndex);
          var itemsBefore = validItemsToUpdate.Take(actualItemIndex);

          await DownloadUrlLinks(itemsAfter.Select(x => x.Model), cancellationToken);
          await DownloadUrlLinks(itemsBefore.Select(x => x.Model), cancellationToken);

          logger.Log(MessageType.Success, "Public links were downloaded");
        }
        catch (OperationCanceledException)
        {
        }

      }, cancellationToken);
    }

    #endregion

    protected abstract IEnumerable<TItemViewModel> GetValidItemsForCloud(IEnumerable<TItemViewModel> validItems);


    #region Dispose

    public override void Dispose()
    {
      base.Dispose();

      MediaPlayer.TimeChanged -= OnVlcTimeChanged;
      UnHookToPlaylistCollectionChanged();

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