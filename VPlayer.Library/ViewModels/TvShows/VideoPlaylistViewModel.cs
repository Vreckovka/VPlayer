using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Prism.Events;
using VCore.WPF.Interfaces.Managers;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.DomainClasses.Video;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Events;
using VPlayer.Core.Factories;
using VPlayer.Core.ViewModels.TvShows;
using VPlayer.Library.ViewModels;

namespace VPlayer.Home.ViewModels.TvShows
{
  public class VideoPlaylistViewModel : FilePlaylistViewModel<VideoItemInPlaylistViewModel, VideoFilePlaylist, PlaylistVideoItem>
  {
    #region Fields

    private readonly IStorageManager storage;
    private readonly IVPlayerViewModelsFactory viewModelsFactory;

    #endregion

    #region Constructors

    public VideoPlaylistViewModel(
      VideoFilePlaylist model,
      IEventAggregator eventAggregator,
      IStorageManager storage,
      IVPlayerViewModelsFactory viewModelsFactory,
      IWindowManager windowManager) : base(model, eventAggregator, storage, windowManager)
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

    public override Task<IEnumerable<VideoItemInPlaylistViewModel>> GetItemsToPlay()
    {
      return Task.Run(() =>
      {
        var playlist = storage.GetTempRepository<VideoFilePlaylist>()
          .Include(x => x.PlaylistItems)
          .ThenInclude(x => x.ReferencedItem)
          .SingleOrDefault(x => x.Id == Model.Id);

        if (playlist != null)
        {
          Model = playlist;

          var playlistItems = playlist.PlaylistItems.OrderBy(x => x.OrderInPlaylist).ToList();

          var list = new List<TvShowEpisodeInPlaylistViewModel>();

          var fristEpisode = storage.GetTempRepository<TvShowEpisode>().Where(x => x.VideoItem.Id == playlistItems[0].IdReferencedItem).Include(x => x.TvShow).SingleOrDefault();

          if (fristEpisode != null)
          {

            var tvShows = storage.GetTempRepository<TvShow>()
              .Where(x => x.Id == fristEpisode.TvShow.Id)
              .Include(x => x.Seasons)
              .ThenInclude(x => x.Episodes)
              .ThenInclude(x => x.VideoItem).Single();

            var tvShowEpisodes = tvShows.Seasons.SelectMany(x => x.Episodes).ToList();

            foreach (var item in playlistItems)
            {
              var tvShowEpisode = tvShowEpisodes.SingleOrDefault(x => x.VideoItem.Id == item.ReferencedItem.Id);

              // Tv show's video item is not valid
              if (tvShowEpisode == null)
              {
                return playlistItems.Select(x => viewModelsFactory.Create<VideoItemInPlaylistViewModel>(x.ReferencedItem));
              }

              list.Add(viewModelsFactory.Create<TvShowEpisodeInPlaylistViewModel>(tvShowEpisode.VideoItem, tvShowEpisode));
            }

            return list;
          }
          else
          {
            return playlistItems.Select(x => viewModelsFactory.Create<VideoItemInPlaylistViewModel>(x.ReferencedItem));
          }
        }

        return null;
      });

    }

    #endregion

    #region PublishPlayEvent

    public override void PublishPlayEvent(IEnumerable<VideoItemInPlaylistViewModel> viewModels, EventAction eventAction)
    {
      var e = new PlayItemsEventData<VideoItemInPlaylistViewModel>(viewModels, eventAction, Model.IsShuffle, Model.IsReapting, null, Model);

      eventAggregator.GetEvent<PlayItemsEvent<VideoItem, VideoItemInPlaylistViewModel>>().Publish(e);
    }

    #endregion


    public override void PublishAddToPlaylistEvent(IEnumerable<VideoItemInPlaylistViewModel> viewModels)
    {
      throw new NotImplementedException();
    }

    protected override PinnedType GetPinnedType(VideoFilePlaylist model)
    {
      return PinnedType.VideoPlaylist;
    }
  }
}