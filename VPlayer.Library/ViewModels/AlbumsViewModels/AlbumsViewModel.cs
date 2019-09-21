using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCore.Annotations;
using VCore.Factories;
using VCore.Modularity.RegionProviders;
using VPlayer.AudioStorage.Interfaces;
using VPlayer.Core.DomainClasses;
using VPlayer.Core.Modularity.Regions;
using VPlayer.Library.ViewModels.LibraryViewModels;
using VPlayer.Library.ViewModels.LibraryViewModels.ArtistsViewModels;
using VPlayer.Library.Views;

namespace VPlayer.Library.ViewModels.AlbumsViewModels
{
    public class AlbumsViewModel : PlayableItemsViewModel<AlbumsView, AlbumViewModel, Album>
    {
        public AlbumsViewModel(
            IRegionProvider regionProvider,
            IViewModelsFactory viewModelsFactory, 
            IStorageManager storageManager, 
            LibraryCollection<AlbumViewModel, Album> libraryCollection) 
            : base(regionProvider, viewModelsFactory, storageManager, libraryCollection)
        {
        }

        public override string RegionName => RegionNames.LibraryContentRegion;
        public override bool ContainsNestedRegions => false;
        public override string Header => "Albums";


       
    }
}
