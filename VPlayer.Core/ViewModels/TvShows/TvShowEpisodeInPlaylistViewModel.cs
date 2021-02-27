using System;
using System.Collections.Generic;
using System.Linq;
using Prism.Events;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Events;
using VPlayer.Core.Interfaces.ViewModels;

namespace VPlayer.Core.ViewModels.TvShows
{
  public class TvShowEpisodeInPlaylistViewModel : ItemInPlayList<TvShowEpisode>
  {
    private readonly ITvShowsViewModel tvShowsViewModel;

    public TvShowEpisodeInPlaylistViewModel(
      TvShowEpisode model,
      IEventAggregator eventAggregator,
      ITvShowsViewModel tvShowsViewModel,
      IStorageManager storageManager) : base(model, eventAggregator, storageManager)
    {
      this.tvShowsViewModel = tvShowsViewModel ?? throw new ArgumentNullException(nameof(tvShowsViewModel));
    }

    public TvShowViewModel TvShow { get; set; }

    public override async void Initialize()
    {
      base.Initialize();

      if (TvShow == null)
        TvShow = (await tvShowsViewModel.GetViewModelsAsync()).SingleOrDefault(x => x.ModelId == Model.TvShow.Id);
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