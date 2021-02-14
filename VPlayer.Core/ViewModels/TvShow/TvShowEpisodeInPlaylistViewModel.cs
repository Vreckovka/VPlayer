using System;
using System.Collections.Generic;
using Prism.Events;
using VCore.Annotations;
using VCore.Standard;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.Core.Events;

namespace VPlayer.Core.ViewModels.TvShow
{
  public class TvShowEpisodeInPlaylistViewModel : ItemInPlayList<TvShowEpisode>
  {
    public TvShowEpisodeInPlaylistViewModel(TvShowEpisode model, [NotNull] IEventAggregator eventAggregator) : base(model, eventAggregator)
    {
    }

    public AudioStorage.DomainClasses.Video.TvShow TvShow { get; set; }

    protected override void UpdateIsFavorite(bool value)
    {
    }

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