namespace VPlayer.AudioStorage.DomainClasses
{
  public abstract class VideoItem : DomainEntity
  {
    public string DiskLocation { get; set; }
    public int Duration { get; set; }
    public int Length { get; set; }
    public string Name { get; set; }
    public bool IsFavorite { get; set; }
    public InfoDownloadStatus InfoDownloadStatus { get; set; }
    public string AspectRatio { get; set; }

    public int? AudioTrack { get; set; }
    public int? Subtitles { get; set; }
  }
}