using VPlayer.Core.ViewModels;

namespace VPlayer.AudioStorage.DomainClasses
{
  public class VideoItem : DomainEntity, IUpdateable<VideoItem>, IPlayableModel, INamedEntity
  {
    public string DiskLocation { get; set; }
    public int Duration { get; set; }
    public int Length { get; set; }
    public string Name { get; set; }
    public bool IsFavorite { get; set; }
    public string AspectRatio { get; set; }
    public int? AudioTrack { get; set; }
    public int? SubtitleTrack { get; set; }


    public void Update(VideoItem other)
    {
      DiskLocation = other.DiskLocation;
      Duration = other.Duration;
      Length = other.Length;
      Name = other.Name;
      IsFavorite = other.IsFavorite;
      AspectRatio = other.AspectRatio;
      AudioTrack = other.AudioTrack;
      SubtitleTrack = other.SubtitleTrack;
    }
  }
}