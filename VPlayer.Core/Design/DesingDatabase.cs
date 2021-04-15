using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using VCore.Standard;
using VCore.ViewModels;
using VPlayer.AudioStorage.DomainClasses;

namespace VPlayer.Core.Design
{
  public class DesingDatabase : ViewModel
  {
    #region Fields

    private static DesingDatabase instance;

    #endregion Fields

    #region Constructors

    public DesingDatabase()
    {
      ServicePointManager.Expect100Continue = true;
      ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

      CreateArtists();
      CreateAlbums();
      CreateSongs();

      WireSongsToAlbums();
    }

    #endregion Constructors

    #region Properties

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

    #endregion Properties

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
           
              "https://www.desktopbackground.org/download/800x600/2014/05/08/759063_metallica-logo-wallpapers-wallpapers-cave_1280x1024_h.jpg"
        },
        new Artist()
        {
          Name = "Ac/Dc",
          Id = 1,
          ArtistCover = 
            "https://i.pinimg.com/originals/2c/61/47/2c6147ce01b3cc93b0353ac42fece305.jpg"
        }
      };
    }

    #endregion CreateArtists

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
          AlbumFrontCoverFilePath = 
            "https://consequenceofsound.net/wp-content/uploads/2019/08/Metallica-Black-Album.jpg?quality=80&w=807"
        },
        new Album()
        {
          Name = "Hardwired to self destruct",
          ReleaseDate = "1998",
          Artist = Artists[0],
          AlbumFrontCoverFilePath =
            "https://www.nuclearblast.de/static/articles/253/253456.jpg/1000x1000.jpg"
        },
        new Album()
        {
          Name = "LET THERE BE ROCK",
          ReleaseDate = "1998",
          Artist = Artists[1],
          AlbumFrontCoverFilePath = 
            "https://cdn.gbposters.com/media/catalog/product/cache/1/image/9df78eab33525d08d6e5fb8d27136e95/a/c/acdc-let-there-be-rock-framed-album-print-1.11.jpg"
        },new Album()
        {
          Name = "THE RAZORS EDGE",
          ReleaseDate = "1997",
          Artist = Artists[1],
          AlbumFrontCoverFilePath =
            "https://i.pinimg.com/originals/68/c5/16/68c51611d032496ea61ecb3b473cc918.jpg"
        }
      };
    }

    #endregion CreateAlbums

    #region CreateSongs

    public void CreateSongs()
    {
      Songs = new List<Song>();
      Random random = new Random();
      foreach (var album in Albums)
      {
        for (int i = 0; i < random.Next(5, 10); i++)
        {
          var song = new Song()
          {
            Name = $"Song  with really long name {i}",
            Length = 2,
            Duration = random.Next(400, 800),
            Source = "DISK LOCATION",
            Album = album
          };

          Songs.Add(song);
        }

        Songs.Add(new Song()
        {
          Name = $"Song with really long name wich should not fit in",
          Length = 2,
          Duration = random.Next(400, 800),
          Source = "DISK LOCATION",
          Album = album
        });
      }
    }

    #endregion CreateSongs

    #region WireSongsToAlbums

    private void WireSongsToAlbums()
    {
      foreach (var album in Albums)
      {
        album.Songs = Songs.Where(x => x.Album == album).ToList();
      }
    }

    #endregion WireSongsToAlbums

    #region UrlToByteArray

    private byte[] UrlToByteArray(string url)
    {
      var webClient = new WebClient();
      return webClient.DownloadData(url);
    }

    #endregion UrlToByteArray

    public List<Album> Albums { get; set; }
    public List<Artist> Artists { get; set; }
    public List<Song> Songs { get; set; }
  }
}