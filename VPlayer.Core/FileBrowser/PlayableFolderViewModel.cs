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
using VCore.Standard.Modularity.Interfaces;
using VCore.Standard.ViewModels.WindowsFile;
using VCore.WPF;
using VCore.WPF.Misc;
using VCore.WPF.ViewModels.WindowsFiles;
using VPlayer.AudioStorage.AudioDatabase;
using VPlayer.AudioStorage.DataLoader;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.DomainClasses.Video;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Events;
using VPlayer.Core.ViewModels;
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


    #region PinnedItem

    private PinnedItem pinnedItem;

    public PinnedItem PinnedItem
    {
      get { return pinnedItem; }
      set
      {
        if (value != pinnedItem)
        {
          pinnedItem = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region IsPinned

    private bool isPinned;

    public bool IsPinned
    {
      get { return isPinned; }
      set
      {
        if (value != isPinned)
        {
          isPinned = value;
          RaisePropertyChanged();
          OnIsPinnedChanged(isPinned);
        }
      }
    }

    protected virtual void OnIsPinnedChanged(bool newValue)
    {

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

    #region PinItem

    private ActionCommand pinItem;

    public ICommand PinItem
    {
      get
      {
        if (pinItem == null)
        {
          pinItem = new ActionCommand(OnPinItem);
        }

        return pinItem;
      }
    }

    public async void OnPinItem()
    {
      var foundItem = storageManager.GetTempRepository<PinnedItem>().SingleOrDefault(x => x.Description == Model.Indentificator &&
                                                                                          x.PinnedType == GetPinnedType());

      if (foundItem == null)
      {
        var newPinnedItem = new PinnedItem();
        newPinnedItem.Description = Model.Indentificator;
        newPinnedItem.PinnedType = GetPinnedType();

        var item = await Task.Run(() => storageManager.AddPinnedItem(newPinnedItem));

        PinnedItem = item;
      }
    }

    protected PinnedType GetPinnedType()
    {
      return FolderType == FolderType.Sound ? PinnedType.SoundFolder : FolderType == FolderType.Video ? PinnedType.VideoFolder : PinnedType.None;
    }

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
          var vms = GetItemsToPlay<VideoItem, VideoItemInPlaylistViewModel>(itemsInFolder, playableFiles, FileType.Video);

          var vmsWithSeries = vms.Select(x => new
          {
            item = x,
            tvshowNumber = DataLoader.GetTvShowSeriesNumber(x.Name)
          }).ToList();


          if (vmsWithSeries.All(x => x.tvshowNumber == null))
          {
            var yearOrder = vms.Select(x => new
            {
              item = x,
              tvshowNumber = DataLoader.GetYear(x.Name)
            }).ToList();

            vms = yearOrder.OrderBy(x => x.tvshowNumber?.YearNumber).Select(x => x.item).ToList();
          }
          else
          {
            vms = vmsWithSeries.OrderBy(x => x.tvshowNumber?.SeasonNumber).ThenBy(x => x.tvshowNumber?.EpisodeNumber).Select(x => x.item).ToList();
          }


          var data = new PlayItemsEventData<VideoItemInPlaylistViewModel>(vms, eventAction, this);

          eventAggregator.GetEvent<PlayItemsEvent<VideoItem, VideoItemInPlaylistViewModel>>().Publish(data);
        }
        else if (FolderType == FolderType.Sound)
        {
          var vms = GetItemsToPlay<SoundItem, SoundItemInPlaylistViewModel>(itemsInFolder, playableFiles, FileType.Sound);

          var data = new PlayItemsEventData<SoundItemInPlaylistViewModel>(vms, eventAction, this);

          eventAggregator.GetEvent<PlayItemsEvent<SoundItem, SoundItemInPlaylistViewModel>>().Publish(data);
        }
      }
      catch (TaskCanceledException) { }
      catch (OperationCanceledException) { }
      finally
      {
        VSynchronizationContext.PostOnUIThread(() =>
        {
          LoadingMessage = null;
          isLoadedSubject.OnNext(false);
          cancellationTokenSource?.Cancel();
          cancellationTokenSource = null;
        });
      }
    }

    #endregion

    private List<TViewModel> GetItemsToPlay<TModel, TViewModel>(
      List<PlayableFileViewModel> itemsInFolder, 
      List<PlayableFileViewModel> playableFiles, 
      FileType fileType)
      where TModel : PlayableItem, IUpdateable<TModel>, new()
      where TViewModel : FileItemInPlayList<TModel>
    {
      itemsInFolder = itemsInFolder.Where(x => x.FileType == fileType).ToList();

      var playableFilesList = playableFiles.Concat(itemsInFolder).Where(x => x.FileType == fileType).ToList();

      var entityItems = storageManager.GetTempRepository<TModel>().Include(x => x.FileInfoEntity)
        .Where(x => playableFilesList.Select(y => y.Model.Indentificator)
          .Contains(x.FileInfoEntity.Indentificator)).ToList();


      var videoItemsIds = entityItems.Select(y => y.Source);

      var notExisting = playableFilesList.Where(x => !videoItemsIds.Contains(x.Model.Indentificator)).ToList();

      if (notExisting.Count > 0)
      {
        foreach (var item in notExisting.Select(x => x.Model))
        {
          var fileInfo = new FileInfoEntity(item.FullName, item.Source)
          {
            Length = item.Length,
            Indentificator = item.Indentificator,
            Name = item.Name,
          };

          var videoItem = new TModel()
          {
            Name = item.Name,
            Source = item.Source,
            FileInfoEntity = fileInfo
          };

          storageManager.StoreEntity(videoItem, out var stored);

          entityItems.Add(stored);

        }
      }

      List<TModel> finalEntityItems = new List<TModel>();
      var numberStringComparer = new NumberStringComparer();

      if (!IsRecursive)
      {
        var acutalFolderfilesOrdered = itemsInFolder.OrderBy(x => x.Name, numberStringComparer);

        foreach (var file in acutalFolderfilesOrdered)
        {
          var soundItem = entityItems.SingleOrDefault(x => x.FileInfoEntity.Indentificator == file.Path);

          if (soundItem != null)
          {
            finalEntityItems.Add(soundItem);
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
            var soundItem = entityItems.SingleOrDefault(x => x.FileInfoEntity.Indentificator == file.Path);

            if (soundItem != null)
            {
              finalEntityItems.Add(soundItem);
            }
          }
        }
      }
      else
      {
        finalEntityItems = entityItems;
      }

      var vms = finalEntityItems.Select(x => viewModelsFactory.Create<TViewModel>(x)).ToList();

      var rx = new RxObservableCollection<TViewModel>();
      rx.AddRange(vms);

      rx.ItemUpdated.Where(x => x.EventArgs.PropertyName == nameof(FileItemInPlayList<TModel>.IsInPlaylist)).Subscribe((updatedItem) =>
      {
        var vm = ((VideoItemInPlaylistViewModel)updatedItem.Sender);

        var file = playableFiles.SingleOrDefault(x => x.Model.Source == vm.Model.Source);

        if (file != null)
        {
          file.IsInPlaylist = vm.IsInPlaylist;
        }

        IsInPlaylist = rx.Any(x => x.IsInPlaylist);
      }).DisposeWith(this);

    

      return vms;
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
