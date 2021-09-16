using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using Prism.Events;
using VCore;
using VCore.Standard.Factories.ViewModels;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.DomainClasses.Video;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Events;
using VPlayer.Core.Factories;
using VPlayer.Core.Modularity.Regions;

namespace VPlayer.Core.ViewModels.TvShows
{
  public class TvShowViewModel : PlayableViewModelWithThumbnail<TvShowEpisodeInPlaylistViewModel, TvShow>
  {
    private readonly IStorageManager storage;
    private readonly IVPlayerViewModelsFactory viewModelsFactory;
    private readonly IVPlayerRegionProvider vPlayerRegionProvider;

    public TvShowViewModel(
      TvShow model,
      IEventAggregator eventAggregator,
       IStorageManager storage,
       IVPlayerViewModelsFactory viewModelsFactory,
       IVPlayerRegionProvider vPlayerRegionProvider
      ) : base(model, eventAggregator)
    {
      this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));
      this.vPlayerRegionProvider = vPlayerRegionProvider ?? throw new ArgumentNullException(nameof(vPlayerRegionProvider));
    }

    #region BottomText

    public override string BottomText
    {
      get
      {
        return Model.Seasons?.Count.ToString() + " seasons";
      }
    }

    #endregion

    public override string BottomPathData => "M520 464H120C106.7 464 96 474.7 96 488C96 501.3 106.7 512 120 512h400c13.25 0 24-10.75 24-24C544 474.7 533.3 464 520 464zM576 0H64C28.65 0 0 28.65 0 64v288c0 35.35 28.65 64 64 64h512c35.35 0 64-28.65 64-64V64C640 28.65 611.3 0 576 0zM592 352c0 8.822-7.178 16-16 16H64c-8.822 0-16-7.178-16-16V64c0-8.822 7.178-16 16-16h512c8.822 0 16 7.178 16 16V352z";

    public override string ImageThumbnail => Model.PosterPath;

    #region Update

    public override void Update(TvShow updateItem)
    {
      Model.Update(updateItem);

      RaisePropertyChanges();
    }

    #endregion

    #region OnDetail

    protected override void OnDetail()
    {
      vPlayerRegionProvider.ShowTvShowDetail(this);
    }

    #endregion

    #region GetItemsToPlay

    public override Task<IEnumerable<TvShowEpisodeInPlaylistViewModel>> GetItemsToPlay()
    {
      return Task.Run(() =>
      {
        var tvShow = storage
          .GetRepository<TvShow>()
          .Include(x => x.Seasons).ThenInclude(x => x.Episodes).ThenInclude(x => x.VideoItem).SingleOrDefault(x => x.Id == ModelId);

        if (tvShow != null)
        {
          var tvShowEpisodes = tvShow.Seasons.OrderBy(x => x.SeasonNumber)
            .Select(x => x.Episodes.OrderBy(x => x.EpisodeNumber).Select(y => viewModelsFactory.CreateTvShowEpisodeInPlayList(y.VideoItem, y)))
            .SelectMany(x => x);


          return tvShowEpisodes;
        }

        return null;
      });
    }

    #endregion

    #region PublishPlayEvent

    public override void PublishPlayEvent(IEnumerable<TvShowEpisodeInPlaylistViewModel> viewModels, EventAction eventAction)
    {
      var e = new PlayItemsEventData<TvShowEpisodeInPlaylistViewModel>(viewModels, eventAction, this);

      eventAggregator.GetEvent<PlayItemsEvent<VideoItem, TvShowEpisodeInPlaylistViewModel>>().Publish(e);
    }

    #endregion



    public override void PublishAddToPlaylistEvent(IEnumerable<TvShowEpisodeInPlaylistViewModel> viewModels)
    {
      throw new NotImplementedException();
    }


  }
}
