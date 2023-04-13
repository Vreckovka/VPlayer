using System;
using System.ComponentModel.DataAnnotations.Schema;
using VCore.Standard.Modularity.Interfaces;
using VPlayer.Core.ViewModels;

namespace VPlayer.AudioStorage.DomainClasses.Video
{
  [Serializable]
  public class PlayableItem : DomainEntity, IUpdateable<PlayableItem>, IFilePlayableModel, INamedEntity
  {
    #region Source

    [NotMapped]
    public string Source
    {
      get
      {
        return FileInfoEntity?.Source;
      }
      set
      {
        if (FileInfoEntity != null)
          FileInfoEntity.Source = value;
      }
    }

    #endregion
    public virtual long Length { get; set; }
    public virtual string Name { get; set; }

    public int Duration { get; set; }
    public string NormalizedName { get; set; }
    public bool IsFavorite { get; set; }

    public TimeSpan TimePlayed { get; set; }
    public bool IsPrivate { get; set; }

    public FileInfoEntity FileInfoEntity { get; set; }

    public void Update(PlayableItem other)
    {
      if (FileInfoEntity == null)
      {
        FileInfoEntity = other.FileInfoEntity;
      }
      else
        FileInfoEntity.Update(other.FileInfoEntity);


      Source = other.Source;
      Duration = other.Duration;
      Length = other.Length;
      Name = other.Name;
      NormalizedName = other.NormalizedName;
      IsFavorite = other.IsFavorite;
      TimePlayed = other.TimePlayed;
      IsPrivate = other.IsPrivate;
    }
  }

  [Serializable]
  public class VideoItem : PlayableItem, IUpdateable<VideoItem>
  {
    public string AspectRatio { get; set; }
    public string CropRatio { get; set; }
    public int? AudioTrack { get; set; }
    public int? SubtitleTrack { get; set; }

   

    public void Update(VideoItem other)
    {
      base.Update(other);

      AspectRatio = other.AspectRatio;
      CropRatio = other.CropRatio;
      AudioTrack = other.AudioTrack;
      SubtitleTrack = other.SubtitleTrack;
    }
  }
}