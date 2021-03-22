using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Prism.Events;
using VCore.Annotations;
using VCore.Standard.Factories.ViewModels;
using VPlayer.AudioStorage.DomainClasses.Video;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Events;
using VPlayer.Core.Modularity.Regions;

namespace VPlayer.Core.ViewModels.TvShows
{
  public class TvShowViewModel : PlayableViewModelWithThumbnail<TvShowEpisodeInPlaylistViewModel, TvShow>
  {
    private readonly IStorageManager storage;
    private readonly IViewModelsFactory viewModelsFactory;
    private readonly IVPlayerRegionProvider vPlayerRegionProvider;

    public TvShowViewModel(
      TvShow model,
      IEventAggregator eventAggregator,
      [NotNull] IStorageManager storage,
      [NotNull] IViewModelsFactory viewModelsFactory,
      [NotNull] IVPlayerRegionProvider vPlayerRegionProvider
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
        return Model.Episodes?.Count.ToString();
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
        .GetRepository<AudioStorage.DomainClasses.Video.TvShow>()
        .Include(x => x.Episodes).SingleOrDefault(x => x.Id == ModelId);

      if (tvShow != null)
      {
        var tvShowEpisodes = tvShow.Episodes.
          OrderBy(x => x.SeasonNumber)
          .ThenBy(x => x.EpisodeNumber)
          .Select(x => viewModelsFactory.Create<TvShowEpisodeInPlaylistViewModel>(x)).ToList();

        tvShowEpisodes.ForEach(x => x.TvShow = this);

        return tvShowEpisodes;
      }

      return null;
    }

    #endregion

    #region PublishPlayEvent

    public override void PublishPlayEvent(IEnumerable<TvShowEpisodeInPlaylistViewModel> viewModels, EventAction eventAction)
    {
      var e = new PlayTvShowEventData(viewModels, eventAction, this);

      eventAggregator.GetEvent<PlayTvShowEvent>().Publish(e);
    }

    #endregion

  

    public override void PublishAddToPlaylistEvent(IEnumerable<TvShowEpisodeInPlaylistViewModel> viewModels)
    {
      throw new NotImplementedException();
    } 

  
  }
}
