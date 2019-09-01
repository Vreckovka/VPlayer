using System;
using System.Collections.Generic;
using System.Linq;
using PropertyChanged;
using VPlayer.AudioStorage.Models;
using VPlayer.Core.ViewModels;

namespace VPlayer.Library.ViewModels.ArtistsViewModels
{
   
    public class ArtistDetailViewModel : ViewModel
    {
        public Artist ActualArtist { get; set; }
        public ICollection<Album> Albums => ActualArtist?.Albums;

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
        public ArtistDetailViewModel(Artist artist)
        {
            ActualArtist = artist;

        }
    }
}
