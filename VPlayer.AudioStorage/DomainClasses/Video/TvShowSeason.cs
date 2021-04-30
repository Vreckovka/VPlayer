using System.Collections.Generic;
using System.Linq;

namespace VPlayer.AudioStorage.DomainClasses.Video
{
  public class TvShowSeason : DomainEntity, IDownloadableEntity, IUpdateable<TvShowSeason>
  {
    public TvShowSeason()
    {

    }
    public TvShowSeason(TvShow tvShow)
    {
      TvShow = tvShow;
    }

    public TvShow TvShow { get; set; }
    public int SeasonNumber { get; set; }
    public virtual List<TvShowEpisode> Episodes { get; set; }
    public string CsfdUrl { get; set; }
    public string PosterPath { get; set; }
    public string Name { get; set; }

    public InfoDownloadStatus InfoDownloadStatus { get; set; }

    public void Update(TvShowSeason other)
    {
      Name = other.Name;
      InfoDownloadStatus = other.InfoDownloadStatus;
      CsfdUrl = other.CsfdUrl;
      PosterPath = other.PosterPath;

      if (other.Episodes != null && Episodes != null)
      {
        foreach (var episode in other.Episodes)
        {
          var mineEpisode = Episodes.SingleOrDefault(x => x.Id == episode.Id);

          if (mineEpisode != null)
            mineEpisode.Update(episode);
        }

        var notIn = other.Episodes.Where(x => x.Id == 0);

        Episodes.AddRange(notIn);

      }
    }
  }
}