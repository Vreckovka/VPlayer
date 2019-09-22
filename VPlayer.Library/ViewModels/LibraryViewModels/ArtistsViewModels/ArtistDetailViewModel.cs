using System;
using System.Collections.Generic;
using System.Linq;
using VCore.Modularity.RegionProviders;
using VCore.ViewModels;
using VPlayer.Core.DomainClasses;
using VPlayer.Core.Modularity.Regions;
using VPlayer.Library.Views;

namespace VPlayer.Library.ViewModels.LibraryViewModels.ArtistsViewModels
{
   
    public class ArtistDetailViewModel : RegionViewModel<ArtistDetailView>
    {
        public ArtistDetailViewModel(IRegionProvider regionProvider, ArtistViewModel artist) : base(regionProvider)
        {
            ActualArtist = artist;
        }

        public override string RegionName => RegionNames.LibraryContentRegion;
        public override bool ContainsNestedRegions => false;

        public ArtistViewModel ActualArtist { get; set; }
        public ICollection<Album> Albums => ActualArtist?.Model.Albums;

        public IEnumerable<Song> Songs
        {
            get { return Albums?.SelectMany(d => d.Songs).ToList(); }
        }
        public TimeSpan TotalLength
        {
            get
            {
                if (Albums != null)
                    return TimeSpan.FromSeconds((from x in Albums
                                             select
                                             (
                                                 from y in x.Songs select y.Length
                                             ).Sum()).Sum());

                return TimeSpan.FromSeconds(0);
            }

        }
        public int TotalNumberOfSongs
        {
            get
            {
                if (Albums != null)
                    return (from x in Albums select x.Songs.Count()).Sum();
                else
                    return 0;
            }
        }
    }
}
