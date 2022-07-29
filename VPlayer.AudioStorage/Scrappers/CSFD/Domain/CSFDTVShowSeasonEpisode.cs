using VPlayer.AudioStorage.DomainClasses;

namespace VPlayer.AudioStorage.Scrappers.CSFD.Domain
{
  public class CSFDTVShowSeasonEpisode : CSFDItem
  {
    public int? EpisodeNumber{ get; set; }
    public int? SeasonNumber { get; set; }
    public string TvShowUrl { get; set; }
  }

  public class CSFDTVShowSeasonEpisodeEntity : DomainEntity
  {
    public int? EpisodeNumber { get; set; }
    public int? SeasonNumber { get; set; }
    public string TvShowUrl { get; set; }


  }
}