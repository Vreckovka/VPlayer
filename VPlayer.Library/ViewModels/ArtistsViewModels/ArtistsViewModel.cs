using VCore.Factories;
using VCore.Modularity.RegionProviders;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Interfaces.ViewModels;
using VPlayer.Core.Modularity.Regions;
using VPlayer.Core.ViewModels.Artists;
using VPlayer.Library.Views;

namespace VPlayer.Library.ViewModels.LibraryViewModels.ArtistsViewModels
{
  public class ArtistsViewModel : PlayableItemsViewModel<ArtistsView, ArtistViewModel, Artist>, IArtistsViewModel
  {
    #region Constructors

    public ArtistsViewModel(
      IRegionProvider regionProvider,
      IViewModelsFactory viewModelsFactory,
      IStorageManager storageManager,
      LibraryCollection<ArtistViewModel, Artist> libraryCollection) :
      base(regionProvider, viewModelsFactory, storageManager, libraryCollection)
    {
    }

    #endregion Constructors

    #region Properties

    public override bool ContainsNestedRegions => false;
    public override string Header => "Artists";
    public override string RegionName { get; protected set; } = RegionNames.LibraryContentRegion;

    #endregion Properties
  }
}