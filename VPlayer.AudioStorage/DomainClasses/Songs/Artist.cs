using System.Collections.Generic;
using VCore;
using VCore.Standard.Modularity.Interfaces;

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
    public virtual ICollection<Album> Albums { get; set; } = new List<Album>();
    public string ArtistCover { get; set; }
    public string MusicBrainzId { get; set; }

    private string name;
    public string Name
    {
      get
      {
        return StringHelper.ToTitleCase(name);
      }
      set
      {
        if(value != name)
        {
          name = StringHelper.ToTitleCase(value);
        }
      }
    }

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