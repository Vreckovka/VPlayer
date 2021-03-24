using System;
using System.Collections.Generic;
using System.Linq;
using Prism.Events;
using VCore.Annotations;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.DomainClasses.Video;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Events;
using VPlayer.Core.Interfaces.ViewModels;

namespace VPlayer.Core.ViewModels.TvShows
{
  public class TvShowEpisodeInPlaylistViewModel : ItemInPlayList<TvShowEpisode>
  {
    public TvShowEpisodeInPlaylistViewModel(
      TvShowEpisode model,
      IEventAggregator eventAggregator,
      IStorageManager storageManager) : base(model, eventAggregator, storageManager)
    {
      TvShow = model.TvShow ?? throw new ArgumentNullException(nameof(model.TvShow));
      TvShowSeason = model.TvShowSeason ?? throw new ArgumentNullException(nameof(model.TvShowSeason));
    }

    public TvShow TvShow { get; set; }
    public TvShowSeason TvShowSeason { get; set; }
  

    protected override void OnActualPositionChanged(float value)
    {
    }

    protected override void OnIsPlayingChanged(bool value)
    {
    }

    protected override void PublishPlayEvent()
    {
      eventAggregator.GetEvent<PlaySongsFromPlayListEvent<TvShowEpisodeInPlaylistViewModel>>().Publish(this);
    }

    #region PublishRemoveFromPlaylist

    protected override void PublishRemoveFromPlaylist()
    {
      var songs = new List<TvShowEpisodeInPlaylistViewModel>() { this };

      var args = new RemoveFromPlaylistEventArgs<TvShowEpisodeInPlaylistViewModel>()
      {
        DeleteType = DeleteType.SingleFromPlaylist,
        ItemsToRemove = songs
      };

      eventAggregator.GetEvent<RemoveFromPlaylistEvent<TvShowEpisodeInPlaylistViewModel>>().Publish(args);
    }

    #endregion
  }
}