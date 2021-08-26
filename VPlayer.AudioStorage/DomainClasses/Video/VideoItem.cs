using VPlayer.Core.ViewModels;

namespace VPlayer.AudioStorage.DomainClasses
{
  public class PlaybleItem : DomainEntity, IUpdateable<PlaybleItem>, IFilePlayableModel,INamedEntity
  {
    public string Source { get; set; }
    public int Duration { get; set; }
    public int Length { get; set; }
    public string Name { get; set; }
    public string NormalizedName { get; set; }
    public bool IsFavorite { get; set; }

    public void Update(PlaybleItem other)
    {
      Source = other.Source;
      Duration = other.Duration;
      Length = other.Length;
      Name = other.Name;
      NormalizedName = other.NormalizedName;
      IsFavorite = other.IsFavorite;
    }
  }

  public class VideoItem : PlaybleItem, IUpdateable<VideoItem>
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