namespace VPlayer.AudioStorage.DomainClasses
{
  public abstract class VideoItem : DomainEntity, IUpdateable<VideoItem>
  {
    public string DiskLocation { get; set; }
    public int Duration { get; set; }
    public int Length { get; set; }
    public string Name { get; set; }
    public bool IsFavorite { get; set; }
    public InfoDownloadStatus InfoDownloadStatus { get; set; }
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
      InfoDownloadStatus = other.InfoDownloadStatus;
      AspectRatio = other.AspectRatio;
      AudioTrack = other.AudioTrack;
      SubtitleTrack = other.SubtitleTrack;
    }
  }
}