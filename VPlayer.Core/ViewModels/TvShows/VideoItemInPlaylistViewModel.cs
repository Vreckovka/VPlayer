using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using Prism.Events;
using VCore.Standard;
using VCore.WPF.Misc;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.DomainClasses.IPTV;
using VPlayer.AudioStorage.DomainClasses.Video;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.AudioStorage.Scrappers.CSFD.Domain;
using VPlayer.Core.Events;
using VPlayer.Core.ViewModels.SoundItems;

namespace VPlayer.Core.ViewModels.TvShows
{
  public class CSFDItemViewModel : ViewModel<CSFDItem>
  {
    public CSFDItemViewModel(CSFDItem model) : base(model)
    {
    }

    #region OpenCsfd

    private ActionCommand openCsfd;
    public ICommand OpenCsfd
    {
      get
      {
        return openCsfd ??= new ActionCommand(OnOpenCsfd);
      }
    }

    private void OnOpenCsfd()
    {
      if (!string.IsNullOrEmpty(Model.Url))
      {
        Process.Start(new ProcessStartInfo()
        {
          FileName = Model.Url,
          UseShellExecute = true,
          Verb = "open"
        });
      }
    }

    #endregion
  }

  public class VideoItemInPlaylistViewModel : FileItemInPlayList<VideoItem>
  {
    public VideoItemInPlaylistViewModel(VideoItem model, IEventAggregator eventAggregator, IStorageManager storageManager) : base(model, eventAggregator, storageManager)
    {
      Description = Path.GetDirectoryName(model.Source);
    }

    protected override void PublishPlayEvent()
    {
      eventAggregator.GetEvent<PlaySongsFromPlayListEvent<VideoItemInPlaylistViewModel>>().Publish(this);
    }

    #region Description

    private string description;

    public string Description
    {
      get { return description; }
      set
      {
        if (value != description)
        {
          description = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region CSFDItem

    private CSFDItemViewModel cSFDItem;

    public CSFDItemViewModel CSFDItem
    {
      get { return cSFDItem; }
      set
      {
        if (value != cSFDItem)
        {
          cSFDItem = value;
          ExtraData = value;

          RaisePropertyChanged();
          RaiseNotifyPropertyChanged(nameof(ImagePath));
        }
      }
    }

    #endregion

    public bool IsStream { get; set; }

    public string ImagePath => CSFDItem?.Model?.ImagePath;

    protected override void PublishRemoveFromPlaylist()
    {
      var songs = new List<VideoItemInPlaylistViewModel>() { this };

      var args = new RemoveFromPlaylistEventArgs<VideoItemInPlaylistViewModel>()
      {
        DeleteType = DeleteType.SingleFromPlaylist,
        ItemsToRemove = songs
      };

      eventAggregator.GetEvent<RemoveFromPlaylistEvent<VideoItemInPlaylistViewModel>>().Publish(args);
    }

    protected override void PublishDeleteFile()
    {
      var songs = new List<VideoItemInPlaylistViewModel>() { this };

      var args = new RemoveFromPlaylistEventArgs<VideoItemInPlaylistViewModel>()
      {
        DeleteType = DeleteType.File,
        ItemsToRemove = songs
      };

      eventAggregator.GetEvent<RemoveFromPlaylistEvent<VideoItemInPlaylistViewModel>>().Publish(args);
    }

    protected override void OnDownloadInfo()
    {
      eventAggregator.GetEvent<DownloadInfoEvent<VideoItemInPlaylistViewModel>>().Publish(this);
    }
  }
}