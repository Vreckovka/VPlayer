using System.Collections.Generic;
using System.IO;
using Prism.Events;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Events;

namespace VPlayer.Core.ViewModels.TvShows
{
  public class VideoItemInPlaylistViewModel : ItemInPlayList<VideoItem>
  {
    public VideoItemInPlaylistViewModel(VideoItem model, IEventAggregator eventAggregator, IStorageManager storageManager) : base(model, eventAggregator, storageManager)
    {
      Description = Path.GetDirectoryName(model.DiskLocation);
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
  }
}