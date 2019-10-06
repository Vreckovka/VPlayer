using System.ComponentModel.DataAnnotations;

namespace VPlayer.Core.DomainClasses
{
  public class Song : INamedEntity
  {
    #region Constructors

    public Song()
    {
    }

    public Song(string name, Album album)
    {
      Name = name;
      Album = album;
    }

    #endregion Constructors

    #region Properties

    public virtual Album Album { get; set; }

    public string DiskLocation { get; set; }

    public int Duration { get; set; }

    [Key]
    public int Id { get; set; }

    public int Length { get; set; }
    public string MusicBrainzId { get; set; }
    public string Name { get; set; }

    #endregion Properties

    #region Methods

    public override string ToString()
    {
      return $"{Name}|{Album}";
    }

    #endregion Methods
  }
}