using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Prism.Events;
using VCore.Standard.Factories.ViewModels;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.DomainClasses.Video;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Events;
using VPlayer.Core.Factories;
using VPlayer.Core.ViewModels.TvShows;

namespace VPlayer.Library.ViewModels.TvShows
{
  public class VideoPlaylistViewModel : PlaylistViewModel<VideoItemInPlaylistViewModel, VideoPlaylist, PlaylistVideoItem>
  {
    #region Fields

    private readonly IStorageManager storage;
    private readonly IVPlayerViewModelsFactory viewModelsFactory;

    #endregion

    #region Constructors

    public VideoPlaylistViewModel(
      VideoPlaylist model,
      IEventAggregator eventAggregator,
     IStorageManager storage,
       IVPlayerViewModelsFactory viewModelsFactory) : base(model, eventAggregator, storage)
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

    public override IEnumerable<VideoItemInPlaylistViewModel> GetItemsToPlay()
    {
      var playlist = storage.GetRepository<VideoPlaylist>()
        .Include(x => x.PlaylistItems)
        .ThenInclude(x => x.VideoItem)
        .SingleOrDefault(x => x.Id == Model.Id);


     

      if (playlist != null)
      {
        var playlistItems = playlist.PlaylistItems.OrderBy(x => x.OrderInPlaylist).ToList();

        var list = new List<TvShowEpisodeInPlaylistViewModel>();

        var fristEpisode = storage.GetRepository<TvShowEpisode>().Where(x => x.VideoItem.Id == playlistItems[0].IdVideoItem).Include(x => x.TvShow).SingleOrDefault();

        if (fristEpisode != null)
        {

          var tvShows = storage.GetRepository<TvShow>()
            .Where(x => x.Id == fristEpisode.TvShow.Id)
            .Include(x => x.Seasons)
            .ThenInclude(x => x.Episodes)
            .ThenInclude(x => x.VideoItem).Single();

          var tvShowEpisodes = tvShows.Seasons.SelectMany(x => x.Episodes).ToList();

          foreach (var item in playlistItems)
          {
            var tvShowEpisode = tvShowEpisodes.Single(x => x.VideoItem.Id == item.VideoItem.Id);

            list.Add(viewModelsFactory.Create<TvShowEpisodeInPlaylistViewModel>(tvShowEpisode.VideoItem, tvShowEpisode));
          }

          return list;
        }
        else
        {
          return playlistItems.Select(x => viewModelsFactory.Create<VideoItemInPlaylistViewModel>(x.VideoItem));
        }
      }

      return null;
    }

    #endregion

    #region PublishPlayEvent

    public override void PublishPlayEvent(IEnumerable<VideoItemInPlaylistViewModel> viewModels, EventAction eventAction)
    {
      var e = new PlayItemsEventData<VideoItemInPlaylistViewModel>(viewModels, eventAction, Model);

      eventAggregator.GetEvent<PlayItemsEvent<VideoItem, VideoItemInPlaylistViewModel>>().Publish(e);
    }

    #endregion


    public override void PublishAddToPlaylistEvent(IEnumerable<VideoItemInPlaylistViewModel> viewModels)
    {
      throw new NotImplementedException();
    }
  }
}