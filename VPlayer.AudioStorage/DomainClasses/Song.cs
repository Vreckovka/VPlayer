using System.ComponentModel.DataAnnotations;

namespace VPlayer.Core.DomainClasses
{
  public class Song : INamedEntity
  {
    public Song() { }
    public Song(string name, Album album)
    {
      Name = name;
      Album = album;
    }


    [Key]
    public int Id { get; set; }
    public string Name { get; set; }
    public string DiskLocation { get; set; }
    public int Length { get; set; }

    public string MusicBrainzId { get; set; }
    public virtual Album Album { get; set; }

    public int Duration { get; set; }
    public override string ToString()
    {
      return $"{Name}|{Album}";
    }
  }
}

