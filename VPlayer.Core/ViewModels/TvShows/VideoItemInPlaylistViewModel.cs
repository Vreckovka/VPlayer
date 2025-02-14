using System.Collections.Generic;
using System.IO;
using Prism.Events;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.DomainClasses.IPTV;
using VPlayer.AudioStorage.DomainClasses.Video;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Events;
using VPlayer.Core.ViewModels.SoundItems;

namespace VPlayer.Core.ViewModels.TvShows
{

  public class OnePieceEpisodeData
  {
    public int EpisodeNumber { get; set; }
    public string StartTime { get; set; }
    public string Name { get; set; }
    public string Chapters { get; set; }
    public bool IsFiller { get; set; }
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

    #region FillerData

    private OnePieceEpisodeData fillerData;

    public OnePieceEpisodeData FillerData
    {
      get { return fillerData; }
      set
      {
        if (value != fillerData)
        {
          fillerData = value;
          RaisePropertyChanged();
        }
      }
    }
    #endregion




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