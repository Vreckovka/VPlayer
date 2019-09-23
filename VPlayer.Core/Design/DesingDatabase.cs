using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using VCore.ViewModels;
using VPlayer.Core.DomainClasses;

namespace VPlayer.Core.Desing
{
  public class DesingDatabase : ViewModel
  {
    private static DesingDatabase instance;
    public static DesingDatabase Instance
    {
      get
      {
        if (instance == null)
        {
          return new DesingDatabase();
        }

        return instance;
      }
    }

    public DesingDatabase()
    {
      ServicePointManager.Expect100Continue = true;
      ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

      CreateArtists();
      CreateAlbums();
      CreateSongs();

      WireSongsToAlbums();
    }

    #region CreateArtists

    private void CreateArtists()
    {
      Artists = new List<Artist>()
      {
        new Artist()
        {
          Name = "Metallica",
          Id = 0,
          ArtistCover =
            UrlToByteArray(
              "https://www.desktopbackground.org/download/800x600/2014/05/08/759063_metallica-logo-wallpapers-wallpapers-cave_1280x1024_h.jpg")
        },
        new Artist()
        {
          Name = "Ac/Dc",
          Id = 1,
          ArtistCover = UrlToByteArray(
            "https://i.pinimg.com/originals/2c/61/47/2c6147ce01b3cc93b0353ac42fece305.jpg")
        }
      };
    }

    #endregion

    #region CreateAlbums

    private void CreateAlbums()
    {
      Albums = new List<Album>()
      {
        new Album()
        {
          Name = "Black album",
          ReleaseDate = "1999",
          Artist = Artists[0],
          AlbumFrontCoverBLOB = UrlToByteArray(
            "https://consequenceofsound.net/wp-content/uploads/2019/08/Metallica-Black-Album.jpg?quality=80&w=807")
        },
        new Album()
        {
          Name = "Hardwired to self destruct",
          ReleaseDate = "1998",
          Artist = Artists[0],
          AlbumFrontCoverBLOB = UrlToByteArray(
            "https://www.nuclearblast.de/static/articles/253/253456.jpg/1000x1000.jpg")
        },
        new Album()
        {
          Name = "LET THERE BE ROCK",
          ReleaseDate = "1998",
          Artist = Artists[1],
          AlbumFrontCoverBLOB = UrlToByteArray(
            "https://cdn.gbposters.com/media/catalog/product/cache/1/image/9df78eab33525d08d6e5fb8d27136e95/a/c/acdc-let-there-be-rock-framed-album-print-1.11.jpg")
        },new Album()
        {
          Name = "THE RAZORS EDGE",
          ReleaseDate = "1997",
          Artist = Artists[1],
          AlbumFrontCoverBLOB = UrlToByteArray(
            "https://i.pinimg.com/originals/68/c5/16/68c51611d032496ea61ecb3b473cc918.jpg")
        }
      };
    }

    #endregion

    #region CreateSongs

    public void CreateSongs()
    {
      Songs = new List<Song>();
      Random random = new Random();
      foreach (var album in Albums)
      {
        for (int i = 0; i < 5; i++)
        {
          var song = new Song()
          {
            Name = $"Song {i}",
            Length = 2,
            Duration = random.Next(400,800),
            DiskLocation = "DISK LOCATION",
            Album = album
          };

          Songs.Add(song);
        }
      }
    }

    #endregion

    #region WireSongsToAlbums

    private void WireSongsToAlbums()
    {
      foreach (var album in Albums)
      {
        album.Songs = Songs.Where(x => x.Album == album).ToList();
      }
    }

    #endregion

    #region UrlToByteArray

    private byte[] UrlToByteArray(string url)
    {
      var webClient = new WebClient();
      return webClient.DownloadData(url);
    }

    #endregion

    public List<Artist> Artists { get; set; }
    public List<Album> Albums { get; set; }
    public List<Song> Songs { get; set; }
  }
}
