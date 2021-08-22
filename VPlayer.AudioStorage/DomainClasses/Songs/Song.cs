using System.ComponentModel.DataAnnotations;
using VPlayer.Core.ViewModels;

namespace VPlayer.AudioStorage.DomainClasses
{
  public class SoundItem : PlaybleItem, IUpdateable<SoundItem>
  {
    public void Update(SoundItem other)
    {
      base.Update(other);
    }
  }

  public class Song : DomainEntity, IUpdateable<Song>, IPlayableModel
  {
    #region Constructors

    public Song()
    {
    }

    public Song(Album album)
    {
      Album = album;
    }

    #endregion Constructors

    #region Properties

    public Album Album { get; set; }
    public SoundItem SoundItem { get; set; }
    
    public string MusicBrainzId { get; set; }
    
    public string Chartlyrics_Lyric { get; set; }
    public string Chartlyrics_LyricId { get; set; }
    public string Chartlyrics_LyricCheckSum { get; set; }

    public string LRCLyrics { get; set; }
    public string UPnPPath { get; set; }



    public string Source
    {
      get { return SoundItem?.Source; }
      set
      {
        if (SoundItem != null)
          SoundItem.Source = value;
      }
    }

    public int Duration
    {
      get
      {
        if (SoundItem != null)
          return SoundItem.Duration;


        return 0;
      }
      set { if (SoundItem != null) SoundItem.Duration = value; }
    }

    public int Length
    {
      get
      {
        if (SoundItem != null)
          return SoundItem.Length;

        return 0;
      }
      set { if (SoundItem != null) SoundItem.Length = Length; }
    }

    public string Name
    {
      get { return SoundItem?.Name; }
      set { if (SoundItem != null) SoundItem.Name = Name; }
    }

    public bool IsFavorite
    {
      get
      {
        if (SoundItem != null)
          return SoundItem.IsFavorite;

        return false;
      }
      set { if (SoundItem != null) SoundItem.IsFavorite = IsFavorite; }
    }

    #endregion 

    #region Methods

    public override string ToString()
    {
      return $"{SoundItem.Name}|{Album}";
    }

    public void Update(Song other)
    {
      if (other.SoundItem != null)
        SoundItem?.Update(other.SoundItem);

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