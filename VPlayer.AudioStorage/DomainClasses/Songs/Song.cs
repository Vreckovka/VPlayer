using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VCore.Standard.Modularity.Interfaces;
using VCore.WPF.ViewModels.WindowsFiles;
using VPlayer.AudioStorage.DomainClasses.Video;
using VPlayer.Core.ViewModels;

namespace VPlayer.AudioStorage.DomainClasses
{


  [Serializable]
  public class FileInfoEntity : FileInfo, IEntity, IUpdateable<FileInfoEntity>
  {
    public FileInfoEntity()
    {
    }

    public FileInfoEntity(string fullName, string source) : base(fullName, source)
    {
    }

    [Key]
    public int Id { get; set; }
    public DateTime? Created { get; set; }
    public DateTime? Modified { get; set; }

    public void Update(FileInfoEntity other)
    {
      ((FileInfo)this).Update(other);

      Id = other.Id;
      Created = other.Created;
      Modified = other.Modified;
      Artist = other.Artist;
      Album = other.Album;
    }

    public override string ToString()
    {
      return $"{Id}{Artist}{Album}";
    }
  }

  [Serializable]
  public class SoundItem : PlayableItem, IUpdateable<SoundItem>
  {
    public void Update(SoundItem other)
    {
      base.Update(other);

      IsAutomaticLyricsFindEnabled = other.IsAutomaticLyricsFindEnabled;
    }

    #region Name

    [NotMapped]
    public override string Name
    {
      get
      {
        if (string.IsNullOrEmpty(FileInfoEntity?.Title))
          return FileInfoEntity?.Name;
        else
          return FileInfoEntity?.Title;
      } 

    }

    #endregion

    #region Source

    [NotMapped]
    public override string Source
    {
      get
      {
        return FileInfoEntity?.Source;
      }
    }

    #endregion

    #region Length

    [NotMapped]
    public override long Length
    {
      get
      {
        if (FileInfoEntity != null)
        {
          return FileInfoEntity.Length;
        }

        return 0;
      }

    }

    #endregion

    public bool IsAutomaticLyricsFindEnabled { get; set; } = true;

    public SoundItem Copy()
    {
      return new SoundItem()
      {
        Created = Created,
        Duration = Duration,
        FileInfoEntity = FileInfoEntity,
        Id = Id,
        IsAutomaticLyricsFindEnabled = IsAutomaticLyricsFindEnabled,
        IsFavorite = IsFavorite,
        Length = Length,
        Modified = Modified,
        Name = Name,
        NormalizedName = NormalizedName,
        Source = Source,
        TimePlayed = TimePlayed
      };
    }

  }

  public class Song : DomainEntity, IUpdateable<Song>, IPlayableModel<SoundItem>
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

    [ForeignKey(nameof(ItemModel))]
    public int ItemModelId { get; set; }
    public SoundItem ItemModel { get; set; }

    public string MusicBrainzId { get; set; }

    public string Chartlyrics_Lyric { get; set; }
    public string Chartlyrics_LyricId { get; set; }
    public string Chartlyrics_LyricCheckSum { get; set; }

    public string LRCLyrics { get; set; }
    public string UPnPPath { get; set; }
    

    [NotMapped]
    public string Source
    {
      get { return ItemModel?.Source; }
    }

    [NotMapped]
    public int Duration
    {
      get
      {
        if (ItemModel != null)
          return ItemModel.Duration;


        return 0;
      }

    }

    [NotMapped]
    public long Length
    {
      get
      {
        if (ItemModel != null)
          return ItemModel.Length;

        return 0;
      }
    }

    [NotMapped]
    public string Name
    {
      get { return ItemModel?.Name; }
    }

    [NotMapped]
    public bool IsFavorite
    {
      get
      {
        if (ItemModel != null)
          return ItemModel.IsFavorite;

        return false;
      }
    }

    #endregion 

    #region Methods

    public override string ToString()
    {
      return $"{ItemModel?.Name}|{Album}";
    }

    public void Update(Song other)
    {
      if (other.ItemModel != null)
        ItemModel?.Update(other.ItemModel);


      Chartlyrics_Lyric = other.Chartlyrics_Lyric;
      Chartlyrics_LyricCheckSum = other.Chartlyrics_LyricCheckSum;
      Chartlyrics_LyricId = other.Chartlyrics_LyricId;
      LRCLyrics = other.LRCLyrics;
    }

    public Song Copy()
    {
      return new Song()
      {
        Album = Album,
        Chartlyrics_Lyric = Chartlyrics_Lyric,
        Chartlyrics_LyricCheckSum = Chartlyrics_LyricCheckSum,
        Chartlyrics_LyricId = Chartlyrics_LyricId,
        Created = Created,
        Id = Id,
        ItemModel = ItemModel?.Copy(),
        LRCLyrics = LRCLyrics,
        Modified = Modified,
        MusicBrainzId = MusicBrainzId,
        UPnPPath = UPnPPath
      };
    }

    #endregion 
  }
}