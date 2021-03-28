using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Prism.Events;
using VCore;
using VCore.Standard.Factories.ViewModels;
using VCore.Standard.ViewModels.TreeView;
using VCore.Standard.ViewModels.WindowsFile;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Events;
using VPlayer.Core.ViewModels.TvShows;

namespace VPlayer.Library.ViewModels.FileBrowser
{
  public class PlayableFolderViewModel : FolderViewModel
  {
    private readonly IEventAggregator eventAggregator;
    private readonly IStorageManager storageManager;

    public PlayableFolderViewModel(
      DirectoryInfo directoryInfo,
      IEventAggregator eventAggregator,
      IViewModelsFactory viewModelsFactory,
      IStorageManager storageManager) : base(directoryInfo, viewModelsFactory)
    {
      this.eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
      this.storageManager = storageManager ?? throw new ArgumentNullException(nameof(storageManager));
    }

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
      if (FolderType == FolderType.Video)
      {
        LoadSubFolders(this);

        var videoFiles = SubItems.SelectMany(x => x.SubItems).OfType<FileViewModel>().ToList();
        videoFiles.AddRange(SubItems.OfType<FileViewModel>());

        var videoItems = new List<VideoItem>();

        foreach (var item in videoFiles)
        {
          var existing = storageManager.GetRepository<VideoItem>().SingleOrDefault(x => x.DiskLocation == item.Model.FullName);

          if (existing == null)
          {
            var videoItem = new VideoItem()
            {
              Name = item.Model.Name,
              DiskLocation = item.Model.FullName
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
    }

    #endregion

    protected override FileViewModel CreateNewFileItem(FileInfo fileInfo)
    {
      return viewModelsFactory.Create<PlayableFileViewModel>(fileInfo);
    }

    protected override FolderViewModel CreateNewFolderItem(DirectoryInfo directoryInfo)
    {
      return viewModelsFactory.Create<PlayableFolderViewModel>(directoryInfo);
    }
  }
}
