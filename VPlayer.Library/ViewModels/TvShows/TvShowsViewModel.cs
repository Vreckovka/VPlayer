using System;
using System.Linq;
using Logger;
using Microsoft.EntityFrameworkCore;
using Prism.Events;
using VCore.Modularity.RegionProviders;
using VCore.Standard.Factories.ViewModels;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Interfaces.ViewModels;
using VPlayer.Core.Modularity.Regions;
using VPlayer.Core.ViewModels.TvShows;
using VPlayer.Home.ViewModels.LibraryViewModels;
using VPlayer.Home.Views.TvShows;

namespace VPlayer.Home.ViewModels.TvShows
{
  public class TvShowsViewModel : PlayableItemsViewModel<TvShowsView, TvShowViewModel, AudioStorage.DomainClasses.Video.TvShow>, ITvShowsViewModel
  {
    private readonly ILogger logger;

    #region Constructors

    public TvShowsViewModel(
      IRegionProvider regionProvider,
      IViewModelsFactory viewModelsFactory,
      IStorageManager storageManager,
      LibraryCollection<TvShowViewModel, AudioStorage.DomainClasses.Video.TvShow> libraryCollection,
      IEventAggregator eventAggregator,
      ILogger logger)
      : base(regionProvider, viewModelsFactory, storageManager, libraryCollection, eventAggregator)
    {
      this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
      EventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
    }

    #endregion 

    #region Properties

    public override bool ContainsNestedRegions => false;
    public override string Header => "Tv shows";
    public override IQueryable<AudioStorage.DomainClasses.Video.TvShow> LoadQuery => base.LoadQuery.Include(x => x.Seasons.OrderBy(y => y.SeasonNumber))
                                                                                                   .ThenInclude(x => x.Episodes.OrderBy(y => y.EpisodeNumber))
                                                                                                   .ThenInclude(x => x.VideoItem);
    public override string RegionName { get; protected set; } = RegionNames.HomeContentRegion;
    public IEventAggregator EventAggregator { get; }

    #endregion Properties
    
  }
}