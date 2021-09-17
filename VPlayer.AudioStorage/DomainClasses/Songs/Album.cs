using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VPlayer.AudioStorage.DomainClasses
{
  public class Album : IDownloadableEntity, IUpdateable<Album>, INamedEntity
  {
    #region Constructors

    public Album()
    {
    }

    #endregion Constructors

    #region Properties

    public string AlbumFrontCoverFilePath { get; set; }

    public string AlbumFrontCoverURI { get; set; }

    [ForeignKey(nameof(Artist))]
    public int ArtistId { get; set; }
    public Artist Artist { get; set; }

    [Key] public int Id { get; set; }

    public string MusicBrainzId { get; set; }
    public string Name { get; set; }
    public string ReleaseDate { get; set; }
    public string NormalizedName { get; set; }

    public InfoDownloadStatus InfoDownloadStatus { get; set; }

    public virtual List<Song> Songs { get; set; } = new List<Song>();


    #endregion Properties

    #region Methods

    #region UpdateAlbum

    public void Update(Album album)
    {
      Name = album.Name;
      AlbumFrontCoverFilePath = album.AlbumFrontCoverFilePath;
      AlbumFrontCoverURI = album.AlbumFrontCoverURI;
      MusicBrainzId = album.MusicBrainzId;
      ReleaseDate = album.ReleaseDate;
      InfoDownloadStatus = album.InfoDownloadStatus;
      

      if (album.ArtistId != 0)
        ArtistId = album.ArtistId;


      if (album.Songs != null)
        Songs = album.Songs;
    }

    #endregion

    #region ToString

    public override string ToString()
    {
      return $"{Name}|{Artist}";
    }

    #endregion

    #endregion Methods
  }
}