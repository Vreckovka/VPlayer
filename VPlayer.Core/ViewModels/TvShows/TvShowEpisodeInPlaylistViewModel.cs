using System;
using System.Collections.Generic;
using System.Linq;
using Prism.Events;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.DomainClasses.Video;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Events;
using VPlayer.Core.Interfaces.ViewModels;

namespace VPlayer.Core.ViewModels.TvShows
{
  public class TvShowEpisodeInPlaylistViewModel : VideoItemInPlaylistViewModel
  {
    public TvShowEpisodeInPlaylistViewModel(
      VideoItem model,
      TvShowEpisode tvShowEpisode,
      IEventAggregator eventAggregator,
      IStorageManager storageManager) : base(model, eventAggregator, storageManager)
    {
      TvShow = tvShowEpisode.TvShow ?? throw new ArgumentNullException(nameof(tvShowEpisode.TvShow));
      TvShowSeason = tvShowEpisode.TvShowSeason ?? throw new ArgumentNullException(nameof(tvShowEpisode.TvShowSeason));
      TvShowEpisode = tvShowEpisode;
    }

    public TvShowEpisode TvShowEpisode { get; }
    public TvShow TvShow { get; set; }
    public TvShowSeason TvShowSeason { get; set; }


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