using System;
using System.Collections.Generic;
using System.Linq;
using Gma.DataStructures.StringSearch;
using Prism.Events;
using Prism.Ioc;
using Prism.Regions;
using VCore.Factories;
using VPlayer.AudioStorage.Models;
using VPlayer.Library.Views;
using VPlayer.Library.VirtualList;


namespace VPlayer.Library.ViewModels.LibraryViewModels.ArtistsViewModels
{
  public class ArtistsCollection : LibraryCollection<ArtistViewModel>
  {
    public ArtistsCollection(IViewModelsFactory viewModelsFactory) : base(viewModelsFactory)
    {
    }

    public sealed override void LoadData()
    {
      List<Artist> artists = new List<Artist>();

      using (var storage = AudioStorage.StorageManager.GetStorage())
      {
        var storageArtist = storage.Artists;

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
                                   x.ArtistCover = (from y in x.Albums
                                                    where y.AlbumFrontCoverBLOB != null
                                                    select y.AlbumFrontCoverBLOB).FirstOrDefault();
                                   return x;
                                 });

        artists.AddRange(artistsWithAlbumId);
        artists.AddRange(artistsWithoutAlbumId);

        Items = artists.Select(x => ViewModelsFactory.Create<ArtistViewModel>(x));
      }
    }
  }
}
