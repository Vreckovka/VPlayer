﻿using System;
using System.Collections.Generic;
using System.Linq;
using VCore.Standard.Modularity.Interfaces;

namespace VPlayer.AudioStorage.DomainClasses.Video
{
  [Serializable]
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
          var mineEpisodes = Episodes.Where(x => x.Id == episode.Id).ToList();

          TvShowEpisode mineEpisode = null;

          if (mineEpisodes.Count > 1)
          {
            var mineEpisodes1 = mineEpisodes.Where(x => x.EpisodeNumber == episode.EpisodeNumber).ToList();

            if (mineEpisodes1.Count == 1)
            {
              mineEpisode = mineEpisodes1[0];
            }
          }
          else if(mineEpisodes.Count == 1)
          {
            mineEpisode = mineEpisodes[0];
          }

          if (mineEpisode != null)
            mineEpisode.Update(episode);
        }

        var notIn = other.Episodes.Where(x => x.Id == 0).ToList();

        Episodes.AddRange(notIn);

      }
    }
  }
}