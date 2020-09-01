using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace VPlayer.AudioStorage.DomainClasses
{
  public class Artist : DomainEntity , DownloadableEntity
  {
    #region Constructors

    public Artist()
    { }

    public Artist(string name)
    {
      Name = name;
    }

    #endregion Constructors

    #region Properties

    public int? AlbumIdCover { get; set; }
    public virtual ICollection<Album> Albums { get; set; }
    public byte[] ArtistCover { get; set; }
    public string MusicBrainzId { get; set; }
    public string Name { get; set; }

    public InfoDownloadStatus InfoDownloadStatus { get; set; }

    #endregion Properties

    #region Methods

    public override string ToString()
    {
      return Name;
    }

    #endregion Methods

   
  }
}