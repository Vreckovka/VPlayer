using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace VPlayer.Core.DomainClasses
{
  public class Artist : INamedEntity
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

    public virtual List<Album> Albums { get; set; }

    public byte[] ArtistCover { get; set; }

    [Key]
    public int Id { get; set; }

    public string MusicBrainzId { get; set; }
    public string Name { get; set; }

    #endregion Properties

    #region Methods

    public override string ToString()
    {
      return Name;
    }

    #endregion Methods
  }
}