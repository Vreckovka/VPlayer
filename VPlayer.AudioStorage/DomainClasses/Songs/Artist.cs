using System.Collections.Generic;

namespace VPlayer.AudioStorage.DomainClasses
{
  public class Artist : DomainEntity , IDownloadableEntity, IUpdateable<Artist>, INamedEntity
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
    public string ArtistCover { get; set; }
    public string MusicBrainzId { get; set; }
    public string Name { get; set; }

    public string NormalizedName { get; set; }

    public InfoDownloadStatus InfoDownloadStatus { get; set; }

    #endregion Properties

    #region Methods

    public void Update(Artist other)
    {
      Name = other.Name;
      MusicBrainzId = other.MusicBrainzId;
      AlbumIdCover = other.AlbumIdCover;
      InfoDownloadStatus = other.InfoDownloadStatus;
    }

    public override string ToString()
    {
      return Name;
    }

    #endregion Methods

   
  }
}