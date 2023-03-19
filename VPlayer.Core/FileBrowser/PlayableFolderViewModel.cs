using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using Prism.Events;
using VCore;
using VCore.ItemsCollections;
using VCore.Standard.Comparers;
using VCore.Standard.Factories.ViewModels;
using VCore.Standard.Helpers;
using VCore.Standard.ViewModels.WindowsFile;
using VCore.WPF.Misc;
using VCore.WPF.ViewModels.WindowsFiles;
using VPlayer.AudioStorage.AudioDatabase;
using VPlayer.AudioStorage.DataLoader;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.DomainClasses.Video;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Events;
using VPlayer.Core.ViewModels.SoundItems;
using VPlayer.Core.ViewModels.TvShows;
using VPLayer.Domain;
using FileInfo = VCore.WPF.ViewModels.WindowsFiles.FileInfo;

namespace VPlayer.Core.FileBrowser
{
  public abstract class PlayableFolderViewModel<TFolderViewModel, TFileViewModel> : FolderViewModel<PlayableFileViewModel>
    where TFolderViewModel : FolderViewModel<TFileViewModel>
    where TFileViewModel : FileViewModel

  {
    private readonly TFolderViewModel folderViewModel;
    private readonly IEventAggregator eventAggregator;
    private readonly IStorageManager storageManager;

    public PlayableFolderViewModel(
      TFolderViewModel folderViewModel,
      IEventAggregator eventAggregator,
      IViewModelsFactory viewModelsFactory,
      IStorageManager storageManager) : base(folderViewModel?.Model, viewModelsFactory)
    {
      this.folderViewModel = folderViewModel ?? throw new ArgumentNullException(nameof(folderViewModel));
      this.eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));

      this.storageManager = storageManager ?? throw new ArgumentNullException(nameof(storageManager));

      Name = folderViewModel.Model.Name;
    }

    #region Properties

    #region CanPlay

    private bool canPlay;

    public virtual bool CanPlay
    {
      get { return canPlay; }
      set
      {
        if (value != canPlay)
        {
          canPlay = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region IsPlaying

    private bool isInPlaylist;

    public bool IsInPlaylist
    {
      get { return isInPlaylist; }
      set
      {
        if (value != isInPlaylist)
        {
          isInPlaylist = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    protected virtual bool IsRecursive
    {
      get
      {
        return false;
      }
    }

    public ObservableCollection<ThumbnailViewModel> Thumbnails { get; } = new ObservableCollection<ThumbnailViewModel>();

    #region ThumbnailsLoading

    private bool thumbnailsLoading;

    public bool ThumbnailsLoading
    {
      get { return thumbnailsLoading; }
      set
      {
        if (value != thumbnailsLoading)
        {
          thumbnailsLoading = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #endregion

    #region Commands

    #region PlayButton

    private ActionCommand<EventAction> playButton;

    public ICommand PlayButton
    {
      get
      {
        if (playButton == null)
        {
          playButton = new ActionCommand<EventAction>(Play);
        }

        return playButton;
      }
    }

    #endregion

    #region LoadNewItem

    private ActionCommand loadNewItem;

    public ICommand LoadNewItem
    {
      get
      {
        if (loadNewItem == null)
        {
          loadNewItem = new ActionCommand(OnLoadNewItem);
        }

        return loadNewItem;
      }
    }

    #endregion

    #region OnTooltip

    private ActionCommand tooltipCommand;

    public ICommand TooltipCommand
    {
      get
      {
        if (tooltipCommand == null)
        {
          tooltipCommand = new ActionCommand(async () => await OnTooltip());
        }

        return tooltipCommand;
      }
    }

    #region OnTooltip

    private async Task OnTooltip()
    {
      var videos = SubItems.View.OfType<PlayableFileViewModel>().Where(x => x.FileType == FileType.Video).ToList();

      if (Thumbnails.Count == 0 && FolderType == FolderType.Video && videos.Count == 1)
      {
        try
        {
          var video = videos.First();

          ThumbnailsLoading = true;
          Thumbnails.Clear();

          await video.CreateImages();

          Thumbnails.AddRange(video.Thumbnails);
        }
        finally

        {
          ThumbnailsLoading = false;
        }
      }
    }

    #endregion

    #endregion

    #endregion

    #region Methods

    #region Play

    private static CancellationTokenSource cancellationTokenSource;
    public async void Play(EventAction eventAction)
    {
      try
      {
        if (IsLoading)
        {
          cancellationTokenSource?.Cancel();
          return;
        }

        cancellationTokenSource?.Cancel();
        cancellationTokenSource = new CancellationTokenSource();

        isLoadedSubject.OnNext(true);

        List<PlayableFileViewModel> itemsInFolder = null;

        if (!IsRecursive)
        {
          await LoadSubFolders(this, cancellationTokenSource.Token);

          itemsInFolder = SubItems.ViewModels.OfType<PlayableFileViewModel>().ToList();
        }
        else
        {
          itemsInFolder = (await GetFiles(IsRecursive)).Select(CreateNewFileItem).ToList();
        }

        var numberStringComparer = new NumberStringComparer();

        var playableFiles = SubItems.ViewModels
          .SelectManyRecursive(x => x.SubItems.ViewModels)
          .OfType<PlayableFileViewModel>()
          .ToList();

        //Docasne kym nevymyslim ako spustat mixed foldre
        if (FolderType == FolderType.Mixed && IsRecursive)
        {
          FolderType = FolderType.Sound;
        }

        if (FolderType == FolderType.Other)
        {
          var videos = itemsInFolder.Where(x => x.FileType == FileType.Video).ToList();
          var music = itemsInFolder.Where(x => x.FileType == FileType.Sound).ToList();

          if (videos.Any() && music.Any())
          {
            FolderType = FolderType.Mixed;
          }
          else if (!videos.Any() && music.Any())
          {
            FolderType = FolderType.Sound;
          }
          else if(videos.Any() && !music.Any())
          {
            FolderType = FolderType.Video;
          }
        }

        if (FolderType == FolderType.Video)
        {
          itemsInFolder = itemsInFolder.Where(x => x.FileType == FileType.Video).ToList();

          var playableFilesList = playableFiles.Concat(itemsInFolder).Where(x => x.FileType == FileType.Video).ToList();

          var videoItems = storageManager.GetTempRepository<VideoItem>()
            .Where(x => playableFilesList.Select(y => y.Model.Indentificator)
              .Contains(x.Source)).ToList();

          var videoItemsIds = videoItems.Select(y => y.Source);

          var notExisting = playableFilesList.Where(x => !videoItemsIds.Contains(x.Model.Indentificator)).ToList();

          if (notExisting.Count > 0)
          {
            foreach (var item in notExisting.Select(x => x.Model))
            {
              var videoItem = new VideoItem()
              {
                Name = item.Name,
                Source = item.Source
              };

              storageManager.StoreEntity(videoItem, out var stored);

              videoItems.Add(stored);

            }
          }

          var vms = videoItems.Select(x => viewModelsFactory.Create<VideoItemInPlaylistViewModel>(x)).ToList();

          var rx = new RxObservableCollection<VideoItemInPlaylistViewModel>();
          rx.AddRange(vms);

          rx.ItemUpdated.Where(x => x.EventArgs.PropertyName == nameof(VideoItemInPlaylistViewModel.IsInPlaylist)).Subscribe((updatedItem) =>
          {
            var vm = ((VideoItemInPlaylistViewModel)updatedItem.Sender);

            var file = playableFiles.SingleOrDefault(x => x.Model.Source == vm.Model.Source);

            if (file != null)
            {
              file.IsInPlaylist = vm.IsInPlaylist;
            }

            IsInPlaylist = rx.Any(x => x.IsInPlaylist);
          });


          var vmsWithSeries = vms.Select(x => new
          {
            item = x,
            tvshowNumber = DataLoader.GetTvShowSeriesNumber(x.Name)
          }).ToList();

          vms = vmsWithSeries.OrderBy(x => x.tvshowNumber?.SeasonNumber).ThenBy(x => x.tvshowNumber?.EpisodeNumber).Select(x => x.item).ToList();

          var data = new PlayItemsEventData<VideoItemInPlaylistViewModel>(vms, eventAction, this);

          eventAggregator.GetEvent<PlayItemsEvent<VideoItem, VideoItemInPlaylistViewModel>>().Publish(data);
        }
        else if (FolderType == FolderType.Sound)
        {
          itemsInFolder = itemsInFolder.Where(x => x.FileType == FileType.Sound).ToList();

          var playableFilesList = playableFiles.Concat(itemsInFolder).Where(x => x.FileType == FileType.Sound).ToList();

          var soundItems = storageManager.GetTempRepository<SoundItem>().Include(x => x.FileInfo)
               .Where(x => playableFilesList.Select(y => y.Model.Indentificator)
              .Contains(x.FileInfo.Indentificator)).ToList();

          var soundItemsIds = soundItems.Select(y => y.FileInfo.Indentificator);

          var notExisting = playableFilesList.Where(x => !soundItemsIds.Contains(x.Model.Indentificator)).ToList();

          if (notExisting.Count > 0)
          {
            foreach (var item in notExisting.Select(x => x.Model))
            {
              var fileInfo = new SoundFileInfo(item.FullName, item.Source)
              {
                Length = item.Length,
                Indentificator = item.Indentificator,
                Name = item.Name,
              };

              var soudItem = new SoundItem()
              {
                FileInfo = fileInfo
              };

              storageManager.StoreEntity(soudItem, out var stored);

              soundItems.Add(stored);

            }
          }

          List<SoundItem> finalSoundItems = new List<SoundItem>();

          if (!IsRecursive)
          {
            var acutalFolderfilesOrdered = itemsInFolder.OrderBy(x => x.Name, numberStringComparer);

            foreach (var file in acutalFolderfilesOrdered)
            {
              var soundItem = soundItems.SingleOrDefault(x => x.FileInfo.Indentificator == file.Path);

              if (soundItem != null)
              {
                finalSoundItems.Add(soundItem);
              }
            }

            var folders = SubItems.ViewModels
              .SelectManyRecursive(x => x.SubItems.ViewModels)
              .OfType<PlayableFolderViewModel<TFolderViewModel, TFileViewModel>>();

            folders = folders.Concat(SubItems.ViewModels.OfType<PlayableFolderViewModel<TFolderViewModel, TFileViewModel>>());

            foreach (var folder in folders)
            {
              var filesOrdered = folder.SubItems.ViewModels.OfType<FileViewModel>().OrderBy(x => x.Name, numberStringComparer);

              foreach (var file in filesOrdered)
              {
                var soundItem = soundItems.SingleOrDefault(x => x.FileInfo.Indentificator == file.Path);

                if (soundItem != null)
                {
                  finalSoundItems.Add(soundItem);
                }
              }
            }
          }
          else
          {
            finalSoundItems = soundItems;
          }

          var data = new PlayItemsEventData<SoundItemInPlaylistViewModel>(
            finalSoundItems.Select(x => viewModelsFactory.Create<SoundItemInPlaylistViewModel>(x)),
            eventAction,
            this);

          eventAggregator.GetEvent<PlayItemsEvent<SoundItem, SoundItemInPlaylistViewModel>>().Publish(data);
        }
      }
      catch (TaskCanceledException) { }
      catch (OperationCanceledException) { }
      finally
      {
        Application.Current.Dispatcher.Invoke(() =>
        {
          LoadingMessage = null;
          isLoadedSubject.OnNext(false);
          cancellationTokenSource?.Cancel();
          cancellationTokenSource = null;
        });
      }
    }

    #endregion

    private string GetSubcratedString(string original, string copy)
    {
      string newString = "";

      var originalWords = original.Split(" ");
      var copyWords = copy.Split(" ");

      var left = originalWords.Where(p => copyWords.All(p2 => p2 != p));

      return left.Aggregate((x, y) => x + " " + y);
    }

    #region OnGetFolderInfo

    protected override void OnGetFolderInfo()
    {
      base.OnGetFolderInfo();

      if (FolderType != FolderType.Other)
      {
        CanPlay = true;
      }

      ParentFolder?.RefreshType();
    }

    #endregion

    #region RefreshType

    public override void RefreshType()
    {
      base.RefreshType();

      if (FolderType != FolderType.Other)
      {
        CanPlay = true;
      }
    }

    #endregion

    #region CreateNewFileItem

    protected override PlayableFileViewModel CreateNewFileItem(FileInfo fileInfo)
    {
      return viewModelsFactory.Create<PlayableFileViewModel>(fileInfo);
    }

    #endregion

    #region OnLoadSubItems

    protected override void OnLoadSubItems()
    {
      base.OnLoadSubItems();

      RefreshPlayablityAndType();
    }

    #endregion

    #region RefreshPlayablityAndType

    private void RefreshPlayablityAndType()
    {
      CanPlay = SubItems.View.OfType<PlayableFolderViewModel<TFolderViewModel, TFileViewModel>>().Any(x => x.CanPlay) || SubItems.View.OfType<PlayableFileViewModel>().Any(x => x.CanPlay);
    }

    #endregion

    #region CreateNewFolderItem

    protected override FolderViewModel<PlayableFileViewModel> CreateNewFolderItem(FolderInfo directoryInfo)
    {
      var folderVm = viewModelsFactory.Create<TFolderViewModel>(directoryInfo);

      return viewModelsFactory.Create<PlayableFolderViewModel<TFolderViewModel, TFileViewModel>>(folderVm);
    }

    #endregion

    #region GetFiles

    public override Task<IEnumerable<FileInfo>> GetFiles(bool recursive = false)
    {
      return folderViewModel.GetFiles(recursive);
    }

    #endregion

    #region GetFolders

    public override Task<IEnumerable<FolderInfo>> GetFolders()
    {
      return folderViewModel.GetFolders();
    }

    #endregion

    public override void OnOpenContainingFolder()
    {
      folderViewModel.OnOpenContainingFolder();
    }

    public virtual void OnLoadNewItem()
    {

    }

    #endregion
  }

  public class FolderGrouping
  {
    public string Name { get; set; }

    public string NormalizedName { get; set; }
    public IEnumerable<SoundItem> Items { get; set; }
    public bool WasProccessed { get; set; }
  }
}
