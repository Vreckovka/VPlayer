using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VPlayer.AudioStorage.DomainClasses.Video
{
  public class TvShow : DomainEntity, DownloadableEntity, IUpdateable<TvShow>
  {
    public virtual List<TvShowEpisode> Episodes { get; set; }
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

      if (other.Episodes != null && Episodes != null)
      {
        foreach (var episode in other.Episodes)
        {
          var mineEpisode = Episodes.Single(x => x.Id == episode.Id);

          mineEpisode.Update(episode);
        }
      }
    }
  }
}
