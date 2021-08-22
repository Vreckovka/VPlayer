using System.Collections.Generic;
using Prism.Events;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Events;

namespace VPlayer.Core.ViewModels.TvShows
{
  public class SoundItemInPlaylistViewModel : FileItemInPlayList<SoundItem>
  {
    public SoundItemInPlaylistViewModel(SoundItem model, IEventAggregator eventAggregator, IStorageManager storageManager) : base(model, eventAggregator, storageManager)
    {
    }

    protected override void PublishPlayEvent()
    {
      eventAggregator.GetEvent<PlaySongsFromPlayListEvent<SoundItemInPlaylistViewModel>>().Publish(this);
    }

    protected override void PublishRemoveFromPlaylist()
    {
      var songs = new List<SoundItemInPlaylistViewModel>() { this };

      var args = new RemoveFromPlaylistEventArgs<SoundItemInPlaylistViewModel>()
      {
        DeleteType = DeleteType.SingleFromPlaylist,
        ItemsToRemove = songs
      };

      eventAggregator.GetEvent<RemoveFromPlaylistEvent<SoundItemInPlaylistViewModel>>().Publish(args);
    }

    #region Update

    public void Update(SoundItem soundItem)
    {
      RaisePropertyChanged(nameof(Name));
    }

    #endregion
  }
}