using System;
using System.Linq;
using System.Windows;
using Logger;
using Microsoft.EntityFrameworkCore;
using Prism.Events;
using VCore.Modularity.Events;
using VCore.Modularity.RegionProviders;
using VCore.Standard.Factories.ViewModels;
using VCore.Standard.Helpers;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.DomainClasses.Video;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Interfaces.ViewModels;
using VPlayer.Core.Modularity.Regions;
using VPlayer.Core.ViewModels.Albums;
using VPlayer.Core.ViewModels.TvShow;
using VPlayer.Library.ViewModels.LibraryViewModels;
using VPlayer.Library.Views;
using VPlayer.WindowsPlayer.ViewModels;

namespace VPlayer.Library.ViewModels.AlbumsViewModels
{
  public class TvShowsViewModel : PlayableItemsViewModel<TvShowsView, TvShowViewModel, TvShow>, ITvShowsViewModel
  {
    private readonly ILogger logger;

    #region Constructors

    public TvShowsViewModel(
      IRegionProvider regionProvider,
      IViewModelsFactory viewModelsFactory,
      IStorageManager storageManager,
      LibraryCollection<TvShowViewModel, TvShow> libraryCollection,
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
    public override IQueryable<TvShow> LoadQuery => base.LoadQuery.Include(x => x.Episodes);
    public override string RegionName { get; protected set; } = RegionNames.LibraryContentRegion;
    public IEventAggregator EventAggregator { get; }

    #endregion Properties
    
  }
}