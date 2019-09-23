using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCore.Annotations;
using VCore.Factories;
using VCore.Modularity.RegionProviders;
using VPlayer.AudioStorage;
using VPlayer.Core.DomainClasses;
using VPlayer.Core.Interfaces.Storage;
using VPlayer.Core.Modularity.Regions;
using VPlayer.Core.ViewModels;
using VPlayer.Library.Views;

namespace VPlayer.Library.ViewModels.LibraryViewModels.ArtistsViewModels
{
    public class ArtistsViewModel : PlayableItemsViewModel<ArtistsView,ArtistViewModel, Artist>
    {

        public ArtistsViewModel(
            IRegionProvider regionProvider, 
            IViewModelsFactory viewModelsFactory,
            IStorageManager storageManager, 
            LibraryCollection<ArtistViewModel, Artist> libraryCollection) : 
            base(regionProvider, viewModelsFactory, storageManager, libraryCollection)
        {
        }

        public override string RegionName => RegionNames.LibraryContentRegion;
        public override bool ContainsNestedRegions => false;
        public override string Header => "Artists";

       
    }
}
