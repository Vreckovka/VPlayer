using System;
using System.ComponentModel.DataAnnotations;
using VCore.WPF.ViewModels.WindowsFiles;
using VPlayer.Core.ViewModels;

namespace VPlayer.AudioStorage.DomainClasses
{
  [Serializable]
  public class SoundFileInfo : FileInfo, IEntity, IUpdateable<SoundFileInfo>
  {
    public SoundFileInfo(string fullName, string source) : base(fullName, source)
    {
    }

    [Key]
    public int Id { get; set; }
    public DateTime? Created { get; set; }
    public DateTime? Modified { get; set; }

    public void Update(SoundFileInfo other)
    {
      ((FileInfo) this).Update(other);

      Id = other.Id;
      Created = other.Created;
      Modified = other.Modified;
      
    }
  }

  public class SoundItem : PlaybleItem, IUpdateable<SoundItem>
  {
    public void Update(SoundItem other)
    {
      base.Update(other);
    }

    public SoundFileInfo FileInfo { get; set; }
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

    public string NormalizedName
    {
      get
      {
        return SoundItem?.NormalizedName;
      }
      set
      {
        if (SoundItem != null)
          SoundItem.NormalizedName = value;
      }
    }


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