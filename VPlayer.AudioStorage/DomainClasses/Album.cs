using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace VPlayer.AudioStorage.DomainClasses
{
  public enum InfoDownloadStatus
  {
    Waiting,
    Downloading,
    Downloaded,
    Failed,
    UnableToFind
  }

  public class Album : DownloadableEntity
  {
    #region Constructors

    public Album()
    {
    }

    #endregion Constructors

    #region Properties

    public string AlbumFrontCoverFilePath { get; set; }

    public string AlbumFrontCoverURI { get; set; }

    public virtual Artist Artist { get; set; }

    [Key] public int Id { get; set; }

    public string MusicBrainzId { get; set; }
    public string Name { get; set; }
    public string ReleaseDate { get; set; }

    public InfoDownloadStatus InfoDownloadStatus { get; set; }

    public virtual List<Song> Songs { get; set; }
   

    #endregion Properties

    #region Methods

    public override string ToString()
    {
      return $"{Name}|{Artist}";
    }

    public void UpdateAlbum(Album album)
    {
      Name = album.Name;
      AlbumFrontCoverFilePath = album.AlbumFrontCoverFilePath;
      AlbumFrontCoverURI = album.AlbumFrontCoverURI;
      MusicBrainzId = album.MusicBrainzId;
      ReleaseDate = album.ReleaseDate;
      InfoDownloadStatus = album.InfoDownloadStatus;
    }

    #endregion Methods
  }
}