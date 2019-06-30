using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gma.DataStructures.StringSearch;
using PropertyChanged;
using VPlayer.AudioStorage.Models;

namespace VPlayer.Library.ViewModels
{
    [AddINotifyPropertyChangedInterface]
    public class ArtistsViewModel
    {
        public IEnumerable<Artist> Artists { get; set; }
        public Trie<Artist> TrieArtist { get; set; } = new Trie<Artist>();
        public IEnumerable<Artist> SortedArtists { get; set; }
        public ArtistsViewModel()
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

            SortedArtists = artists.OrderBy(x => x.Name).ToList();

            foreach (var artist in SortedArtists)
            {
                TrieArtist.Add(artist.Name.ToLower(), artist);
            }

            Artists = SortedArtists;

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
