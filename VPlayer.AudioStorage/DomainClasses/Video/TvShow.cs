using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VPlayer.AudioStorage.DomainClasses.Video
{
  public class TvShow : DomainEntity, IDownloadableEntity, IUpdateable<TvShow>, INamedEntity
  {
    public virtual List<TvShowSeason> Seasons { get; set; }
    public string Name { get; set; }
    public InfoDownloadStatus InfoDownloadStatus { get; set; }
    public string CsfdUrl { get; set; }
    public string PosterPath { get; set; }

    public void Update(TvShow other)
    {
      Name = other.Name;
      InfoDownloadStatus = other.InfoDownloadStatus;
      CsfdUrl = other.CsfdUrl;
      PosterPath = other.PosterPath;

      if (other.Seasons != null && Seasons != null)
      {
        foreach (var showSeason in other.Seasons)
        {
          var mineEpisode = Seasons.SingleOrDefault(x => x.Id == showSeason.Id);

          if (mineEpisode != null)
            mineEpisode.Update(showSeason);
        }

        var notIn = other.Seasons.Where(x => x.Id == 0);

        Seasons.AddRange(notIn);
      }
    }
  }
}
