using PropertyChanged;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace VPlayer.Core.DomainClasses
{
  [AddINotifyPropertyChangedInterface]
  public class Album : INamedEntity
  {
    #region Constructors

    public Album()
    {
    }

    #endregion Constructors

    #region Properties

    public byte[] AlbumFrontCoverBLOB { get; set; }
    public string AlbumFrontCoverURI { get; set; }
    public virtual Artist Artist { get; set; }
    [Key] public int Id { get; set; }
    public string MusicBrainzId { get; set; }
    public string Name { get; set; }
    public string ReleaseDate { get; set; }
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
      AlbumFrontCoverBLOB = album.AlbumFrontCoverBLOB;
      AlbumFrontCoverURI = album.AlbumFrontCoverURI;
      MusicBrainzId = album.MusicBrainzId;
      ReleaseDate = album.ReleaseDate;
    }

    #endregion Methods
  }
}