using System;
using System.Collections.Generic;
using System.Linq;
using VCore.Modularity.RegionProviders;
using VCore.ViewModels;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.Core.Modularity.Regions;
using VPlayer.Core.ViewModels.Artists;
using VPlayer.Library.Views;

namespace VPlayer.Library.ViewModels.ArtistsViewModels
{
  public class ArtistDetailViewModel : RegionViewModel<ArtistDetailView>
  {
    #region Constructors

    public ArtistDetailViewModel(IRegionProvider regionProvider, ArtistViewModel artist) : base(regionProvider)
    {
      ActualArtist = artist;
    }

    #endregion Constructors

    #region Properties

    public ArtistViewModel ActualArtist { get; set; }
    public ICollection<Album> Albums => ActualArtist?.Model.Albums;
    public override bool ContainsNestedRegions => false;
    public override string RegionName { get; protected set; } = RegionNames.LibraryContentRegion;

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

    #endregion Properties
  }
}