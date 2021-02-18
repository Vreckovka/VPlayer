using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Prism.Events;
using VCore.Annotations;
using VCore.Standard.Factories.ViewModels;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Events;
using VPlayer.Core.ViewModels.Artists;

namespace VPlayer.Core.ViewModels.TvShow
{
  public class TvShowViewModel : PlayableViewModelWithThumbnail<TvShowEpisodeInPlaylistViewModel, AudioStorage.DomainClasses.Video.TvShow>
  {
    private readonly IStorageManager storage;
    private readonly IViewModelsFactory viewModelsFactory;

    public TvShowViewModel(
      AudioStorage.DomainClasses.Video.TvShow model, 
      IEventAggregator eventAggregator, 
      [NotNull] IStorageManager storage,
      [NotNull] IViewModelsFactory viewModelsFactory
      ) : base(model, eventAggregator)
    {
      this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));
    }

    public override void Update(AudioStorage.DomainClasses.Video.TvShow updateItem)
    {
      throw new NotImplementedException();
    }

    protected override void OnDetail()
    {
      throw new NotImplementedException();
    }

    #region GetItemsToPlay

    public override IEnumerable<TvShowEpisodeInPlaylistViewModel> GetItemsToPlay()
    {
      var tvShow = storage
        .GetRepository<AudioStorage.DomainClasses.Video.TvShow>()
        .Include(x => x.Episodes).SingleOrDefault(x => x.Id == ModelId);

      if (tvShow != null)
      {
        var tvShowEpisodes = tvShow.Episodes.Select(x => viewModelsFactory.Create<TvShowEpisodeInPlaylistViewModel>(x)).ToList();

        tvShowEpisodes.ForEach(x => x.TvShow = this.Model);

        return tvShowEpisodes;
      }

      return null;
    }

    #endregion

    public override void PublishPlayEvent(IEnumerable<TvShowEpisodeInPlaylistViewModel> viewModels, EventAction eventAction)
    {
      var e = new PlayTvShowEventData(viewModels, eventAction, this);

      eventAggregator.GetEvent<PlayTvShowEvent>().Publish(e);
    }

    public override void PublishAddToPlaylistEvent(IEnumerable<TvShowEpisodeInPlaylistViewModel> viewModels)
    {
      throw new NotImplementedException();
    }

    public override string BottomText { get; }
    public override string ImageThumbnail { get; }

   
  }
}
