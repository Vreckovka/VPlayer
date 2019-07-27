using System.Collections.Generic;
using System.Linq;
using Gma.DataStructures.StringSearch;
using Prism.Events;
using Prism.Ioc;
using Prism.Regions;
using VPlayer.AudioStorage.Models;
using VPlayer.Core.ViewModels;
using VPlayer.Library.ViewModels.ArtistsViewModels;
using VPlayer.Library.Views;


//TODO: PLAYABLE COLLECTION
//TODO: PRISM REGION NAVIGATION  
namespace VPlayer.Library.ViewModels
{
    public class ArtistsViewModel : ModuleViewModel
    {
        public IEnumerable<ArtistViewModel> Artists { get; set; }
        public Trie<ArtistViewModel> TrieArtist { get; set; } = new Trie<ArtistViewModel>();
        public IEnumerable<ArtistViewModel> SortedArtists { get; set; }
        public ArtistsViewModel(IEventAggregator eventAggregator)
        {
            List<Artist> artists = new List<Artist>();

            var storageArtist = AudioStorage.StorageManager.GetStorage().Artists.ToList();
            artists = storageArtist.Where(x => x.ArtistCover != null).ToList();

            var artistsWithAlbumId = (from x in storageArtist
                                      where x.ArtistCover == null
                                      where x.AlbumIdCover != null
                                      select x).Select(x =>
                                      {
                                          x.ArtistCover = (from y in x.Albums
                                                           where y.AlbumId == x.AlbumIdCover
                                                           select y.AlbumFrontCoverBLOB).SingleOrDefault();
                                          return x;
                                      });



            var artistsWithoutAlbumId = (from x in storageArtist
                                         where x.ArtistCover == null
                                         where x.AlbumIdCover == null
                                         select x).Select(x =>
                                         {
                                             x.ArtistCover = (from y in x.Albums where y.AlbumFrontCoverBLOB != null
                                                              select y.AlbumFrontCoverBLOB).FirstOrDefault();
                                             return x;
                                         });

            artists.AddRange(artistsWithAlbumId);
            artists.AddRange(artistsWithoutAlbumId);

            SortedArtists = artists.OrderBy(x => x.Name).Select(x => new ArtistViewModel(x, eventAggregator));

            foreach (var artist in SortedArtists)
            {
                TrieArtist.Add(artist.Model.Name.ToLower(), artist);
            }

            Artists = SortedArtists;

        }

        public ArtistsViewModel()
        {
        }

        public override void OnInitialized(IContainerProvider containerProvider)
        {
            base.OnInitialized(containerProvider);

            var regionManager = containerProvider.Resolve<IRegionManager>();
            regionManager.RegisterViewWithRegion("LibraryContentRegion", typeof(ArtistsView));
        }

        public void SetArtistsByName(string name)
        {
            if (name != "")
                Artists = TrieArtist.Retrieve(name);
            else
                Artists = SortedArtists;
        }
    }
}
