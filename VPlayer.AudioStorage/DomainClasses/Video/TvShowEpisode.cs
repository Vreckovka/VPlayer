using VPlayer.AudioStorage.DomainClasses.Video;
using VPlayer.Core.ViewModels;

namespace VPlayer.AudioStorage.DomainClasses
{
  public class TvShowEpisode : IUpdateable<TvShowEpisode>, IPlayableModel, DownloadableEntity
  {
    public TvShowEpisode()
    {
      
    }

    public TvShowEpisode(TvShow tvShow)
    {
      TvShow = tvShow;
    }

    public TvShow TvShow { get; set; }
    public TvShowSeason TvShowSeason { get; set; }
    public string DiskLocation { get; set; }
    public int Duration { get; set; }
    public int Id { get; set; }
    public int Length { get; set; }
    public string Name { get; set; }
    public bool IsFavorite { get; set; }
    public InfoDownloadStatus InfoDownloadStatus { get; set; }
    public int EpisodeNumber { get; set; }

    public void Update(TvShowEpisode other)
    {
      Name = other.Name;
      InfoDownloadStatus = other.InfoDownloadStatus;
      IsFavorite = other.IsFavorite;
    }
  }
}