using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCore.Annotations;
using VCore.Factories;
using VCore.Modularity.RegionProviders;
using VCore.ViewModels;
using VCore.ViewModels.Navigation;
using VPlayer.AudioStorage;
using VPlayer.Core.Modularity.Regions;
using VPlayer.Library.Views;

namespace VPlayer.Library.ViewModels.LibraryViewModels.ArtistsViewModels
{
  public class ArtistsViewModel : RegionViewModel<ArtistsView>, INavigationItem
  {
    private readonly IViewModelsFactory viewModelsFactory;

    public ArtistsViewModel(IRegionProvider regionProvider,
      [NotNull] ArtistsCollection artistsCollection, [NotNull] IViewModelsFactory viewModelsFactory) : base(regionProvider)
    {
      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));
      ArtistsCollection = artistsCollection ?? throw new ArgumentNullException(nameof(artistsCollection));

      StorageManager.ArtistStored += StorageManager_ArtistStored; ;
    }

    private void StorageManager_ArtistStored(object sender, AudioStorage.Models.Artist e)
    {
      ArtistsCollection.Add(viewModelsFactory.Create<ArtistViewModel>(e));
    }

    public override string RegionName => RegionNames.LibraryContentRegion;
    public override bool ContainsNestedRegions => false;
    public string Header => "Artists";

    public ArtistsCollection ArtistsCollection { get; set; }

    public override void OnActivation(bool firstActivation)
    {
      base.OnActivation(firstActivation);

      if (firstActivation)
        ArtistsCollection.LoadData();
    }
  }
}
