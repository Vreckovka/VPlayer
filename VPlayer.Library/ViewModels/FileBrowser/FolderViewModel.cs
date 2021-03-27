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
using VCore.Annotations;
using VCore.Standard.Factories.ViewModels;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Events;
using VPlayer.Core.ViewModels.TvShows;

namespace VPlayer.Library.ViewModels.FileBrowser
{
  public enum FolderType
  {
    Other,
    Video,
    Sound,
    Mixed,
  }

  public class FolderViewModel : PlayableWindowsItem<DirectoryInfo>
  {
    private readonly IViewModelsFactory viewModelsFactory;
    private readonly IStorageManager storageManager;

    public FolderViewModel([NotNull] DirectoryInfo directoryInfo, 
      IEventAggregator eventAggregator,
      [NotNull] IViewModelsFactory viewModelsFactory,
      IStorageManager storageManager) : base(directoryInfo, eventAggregator)
    {
      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));
      this.storageManager = storageManager;
      CanExpand = true;
    }

    #region FolderType

    private FolderType folderType;

    public FolderType FolderType
    {
      get { return folderType; }
      set
      {
        if (value != folderType)
        {
          folderType = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region GetFolderInfo

    public void GetFolderInfo()
    {
      string[] soundExtentions = new string[] { ".mp3", ".flac" };
      string[] videoExtentions = new string[] { ".mkv", ".avi", ".mp4" };


      FileInfo[] allFiles = Model.GetFiles();
      FileInfo[] soundFiles = allFiles.Where(f => soundExtentions.Contains(f.Extension.ToLower())).ToArray();
      FileInfo[] videoFiles = allFiles.Where(f => videoExtentions.Contains(f.Extension.ToLower())).ToArray();

      if (soundFiles.Length > 0 && videoFiles.Length > 0)
      {
        FolderType = FolderType.Mixed;
      }
      else if (soundFiles.Length > 0)
      {
        FolderType = FolderType.Sound;
      }
      else if (videoFiles.Length > 0)
      {
        FolderType = FolderType.Video;
      }

      var files = new List<FileInfo>();

      files.AddRange(soundFiles);
      files.AddRange(videoFiles);

      SubItems.AddRange(allFiles.Select(x => new FileViewModel(x)));

      if (allFiles.Length == 0)
      {
        CanExpand = false;
      }
    }

    #endregion

    #region OnExpanded

    protected override void OnExpanded(bool isExpandend)
    {
      if (isExpandend)
      {
        var directories = Model.GetDirectories();

        var direViewModels = directories.Select(x => viewModelsFactory.Create<FolderViewModel>(x)).ToList();

        SubItems.AddRange(direViewModels);

        foreach (var dir in direViewModels)
        {
          dir.GetFolderInfo();
        }
      }
    }

    #endregion

    public override void Play()
    {
      if(FolderType == FolderType.Video)
      {
        var videoFiles = SubItems.OfType<FileViewModel>();

        var videoItems = new List<VideoItem>();

        foreach(var item in videoFiles)
        {
          var existing = storageManager.GetRepository<VideoItem>().SingleOrDefault(x => x.DiskLocation == item.Model.FullName);

          if(existing == null)
          {
            var videoItem = new VideoItem()
            {
              Name = item.Model.Name,
              DiskLocation = item.Model.FullName
            };

            storageManager.StoreEntity<VideoItem>(videoItem, out var stored);

            videoItems.Add(stored);
          }
          else
          {
            videoItems.Add(existing);
          }
        }

        var data = new PlayItemsEventData<VideoItemInPlaylistViewModel>(videoItems.Select(x => viewModelsFactory.Create<VideoItemInPlaylistViewModel>(x)),EventAction.Play,this);

        eventAggregator.GetEvent<PlayItemsEvent<VideoItem, VideoItemInPlaylistViewModel>>().Publish(data);
      }
    }
  }

  public abstract class PlayableWindowsItem<TItem> : WindowsItem<TItem> where TItem : FileSystemInfo
  {
    protected readonly IEventAggregator eventAggregator;

    public PlayableWindowsItem([NotNull] TItem model, [NotNull] IEventAggregator eventAggregator) : base(model)
    {
      this.eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
    }

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

    public abstract void Play();
  }
}
