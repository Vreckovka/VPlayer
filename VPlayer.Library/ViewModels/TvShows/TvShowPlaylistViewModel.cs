using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Prism.Events;
using VCore.Annotations;
using VCore.Standard.Factories.ViewModels;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Events;
using VPlayer.Core.ViewModels.TvShows;

namespace VPlayer.Library.ViewModels.TvShows
{
  public class TvShowPlaylistViewModel : PlaylistViewModel<TvShowEpisodeInPlaylistViewModel, TvShowPlaylist, PlaylistTvShowEpisode>
  {
    #region Fields

    private readonly IStorageManager storage;
    private readonly IViewModelsFactory viewModelsFactory;

    #endregion

    #region Constructors

    public TvShowPlaylistViewModel(
      TvShowPlaylist model,
      IEventAggregator eventAggregator,
      [NotNull] IStorageManager storage,
      [NotNull] IViewModelsFactory viewModelsFactory) : base(model, eventAggregator, storage)
    {
      this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));
    }

    #endregion
   
    protected override void OnDetail()
    {
      throw new NotImplementedException();
    }

    #region GetItemsToPlay

    public override IEnumerable<TvShowEpisodeInPlaylistViewModel> GetItemsToPlay()
    {
      var playlist = storage.GetRepository<TvShowPlaylist>()
        .Include(x => x.PlaylistItems)
        .ThenInclude(x => x.TvShowEpisode)
        .ThenInclude(x => x.TvShow)
        .SingleOrDefault(x => x.Id == Model.Id);

      if(playlist != null)
      {
        var playListSong = playlist.PlaylistItems.OrderBy(x => x.OrderInPlaylist).Select(x => viewModelsFactory.Create<TvShowEpisodeInPlaylistViewModel>(x.TvShowEpisode));

        return playListSong;
      }

      return null;
    }

    #endregion

    #region PublishPlayEvent

    public override void PublishPlayEvent(IEnumerable<TvShowEpisodeInPlaylistViewModel> viewModels, EventAction eventAction)
    {
      var e = new PlayTvShowEventData(viewModels, eventAction, Model);

      eventAggregator.GetEvent<PlayTvShowEvent>().Publish(e);
    }

    #endregion


    public override void PublishAddToPlaylistEvent(IEnumerable<TvShowEpisodeInPlaylistViewModel> viewModels)
    {
      throw new NotImplementedException();
    }
  }
}