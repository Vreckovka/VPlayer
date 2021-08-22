using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Prism.Events;
using VCore;
using VCore.Standard.Factories.ViewModels;
using VCore.Standard.ViewModels.WindowsFile;
using VCore.WPF.ViewModels.WindowsFiles;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Events;
using VPlayer.Core.ViewModels.TvShows;
using FileInfo = VCore.WPF.ViewModels.WindowsFiles.FileInfo;

namespace VPlayer.Core.FileBrowser
{
  public class PlayableFolderViewModel<TFolderViewModel, TFileViewModel> : FolderViewModel<PlayableFileViewModel>
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

    #region CanPlay

    private bool canPlay;

    public bool CanPlay
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

    #region Commands

    #region PlayButton

    private ActionCommand playButton;

    public ICommand PlayButton
    {
      get
      {
        if (playButton == null)
        {
          playButton = new ActionCommand(Play);
        }

        return playButton;
      }
    }

    #endregion

    #endregion

    #region Play

    public void Play()
    {
      if (FolderType != FolderType.Other && FolderType != FolderType.Mixed)
      {
        LoadSubFolders(this);

        var playableFiles = SubItems.ViewModels.SelectMany(x => x.SubItems.ViewModels).OfType<FileViewModel>().ToList();

        if (FolderType == FolderType.Video)
        {
          playableFiles.AddRange(SubItems.ViewModels.OfType<PlayableFileViewModel>().Where(x => x.FileType == FileType.Video));

          var videoItems = new List<VideoItem>();

          foreach (var item in playableFiles)
          {
            var existing = storageManager.GetRepository<VideoItem>().SingleOrDefault(x => x.Source == item.Model.Source);

            if (existing == null)
            {
              var videoItem = new VideoItem()
              {
                Name = item.Model.Name,
                Source = item.Model.Source
              };

              storageManager.StoreEntity(videoItem, out var stored);

              videoItems.Add(stored);
            }
            else
            {
              videoItems.Add(existing);
            }
          }

          var data = new PlayItemsEventData<VideoItemInPlaylistViewModel>(videoItems.Select(x => viewModelsFactory.Create<VideoItemInPlaylistViewModel>(x)), EventAction.Play, this);

          eventAggregator.GetEvent<PlayItemsEvent<VideoItem, VideoItemInPlaylistViewModel>>().Publish(data);
        }
        else if (FolderType == FolderType.Sound)
        {
          playableFiles.AddRange(SubItems.ViewModels.OfType<PlayableFileViewModel>().Where(x => x.FileType == FileType.Sound));

          var videoItems = new List<SoundItem>();

          foreach (var item in playableFiles)
          {
            var existing = storageManager.GetRepository<SoundItem>().SingleOrDefault(x => x.Source == item.Model.Source);

            if (existing == null)
            {
              var videoItem = new SoundItem()
              {
                Name = item.Model.Name,
                Source = item.Model.Source
              };

              storageManager.StoreEntity(videoItem, out var stored);

              videoItems.Add(stored);
            }
            else
            {
              videoItems.Add(existing);
            }
          }

          var data = new PlayItemsEventData<SoundItemInPlaylistViewModel>(videoItems.Select(x => viewModelsFactory.Create<SoundItemInPlaylistViewModel>(x)), EventAction.Play, this);

          eventAggregator.GetEvent<PlayItemsEvent<SoundItem, SoundItemInPlaylistViewModel>>().Publish(data);
        }
      }
    }

    #endregion

    #region OnGetFolderInfo

    protected override void OnGetFolderInfo()
    {
      base.OnGetFolderInfo();

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

    public override Task<IEnumerable<FileInfo>> GetFiles()
    {
      return folderViewModel.GetFiles();
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
  }
}
