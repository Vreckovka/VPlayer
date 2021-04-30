using System;
using System.Collections.Generic;
using System.Linq;
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
        return Model.Seasons?.Count.ToString();
      }
    }

    #endregion

    
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

    public override IEnumerable<TvShowEpisodeInPlaylistViewModel> GetItemsToPlay()
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
