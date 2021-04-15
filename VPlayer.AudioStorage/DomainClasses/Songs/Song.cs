using System.ComponentModel.DataAnnotations;
using VPlayer.Core.ViewModels;

namespace VPlayer.AudioStorage.DomainClasses
{
  public interface IUpdateable<TEntity> 
  {
    void Update(TEntity other);
  }

  public class Song : INamedEntity, IUpdateable<Song>, IFilePlayableModel
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

    public string Source { get; set; }

    public int Duration { get; set; }

    [Key]
    public int Id { get; set; }

    public int Length { get; set; }
    public string MusicBrainzId { get; set; }
    public string Name { get; set; }

    public string Chartlyrics_Lyric { get; set; }
    public string Chartlyrics_LyricId { get; set; }
    public string Chartlyrics_LyricCheckSum { get; set; }

    public string LRCLyrics { get; set; }

    public bool IsFavorite { get; set; }

    #endregion 

    #region Methods

    public override string ToString()
    {
      return $"{Name}|{Album}";
    }

    public void Update(Song other)
    {
      Name = other.Name;
      Chartlyrics_Lyric = other.Chartlyrics_Lyric;
      Chartlyrics_LyricCheckSum = other.Chartlyrics_LyricCheckSum;
      Chartlyrics_LyricId = other.Chartlyrics_LyricId;
      LRCLyrics = other.LRCLyrics;
      IsFavorite = other.IsFavorite;
    }

    #endregion Methods
  }
}