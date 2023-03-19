using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Logger;
using Microsoft.EntityFrameworkCore;
using VCore.WPF.Controls.StatusMessage;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.DomainClasses.Video;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.AudioStorage.Scrappers.CSFD;
using VPlayer.Core.Managers.Status;
using VPlayer.Library.ViewModels.TvShows;

namespace VPlayer.AudioStorage.Scrappers
{
  public class TVShowScrapper : ITvShowScrapper
  {
    private readonly IStorageManager storageManager;
    private readonly ICSFDWebsiteScrapper cSfdWebsiteScrapper;
    private readonly ILogger logger;
    private readonly IStatusManager statusManager;

    public TVShowScrapper(
       IStorageManager storageManager,
       ICSFDWebsiteScrapper cSfdWebsiteScrapper,
       ILogger logger,
       IStatusManager statusManager)
    {
      this.storageManager = storageManager ?? throw new ArgumentNullException(nameof(storageManager));
      this.cSfdWebsiteScrapper = cSfdWebsiteScrapper ?? throw new ArgumentNullException(nameof(cSfdWebsiteScrapper));
      this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
      this.statusManager = statusManager ?? throw new ArgumentNullException(nameof(statusManager));
    }

    #region UpdateTvShowFromCsfd

    private CancellationTokenSource ctsUpdateTvShowFromCsfd;
    public Task UpdateTvShowFromCsfd(int tvShowId, string csfUrl)
    {
      return Task.Run(async () =>
      {
        try
        {
          var statusMessage = new StatusMessageViewModel(1)
          {
            Status = StatusType.Starting,
            Message = "Starting to scrape tv show"
          };

          statusManager.UpdateMessage(statusMessage);

          var dbTvShow = storageManager.GetTempRepository<TvShow>().Include(x => x.Seasons).ThenInclude(x => x.Episodes).ThenInclude(x => x.VideoItem).Single(x => x.Id == tvShowId);

          dbTvShow.InfoDownloadStatus = InfoDownloadStatus.Downloading;

          Application.Current.Dispatcher.Invoke(() =>
          {
            storageManager.PublishItemChanged(dbTvShow);
          });

          ctsUpdateTvShowFromCsfd?.Cancel();
          ctsUpdateTvShowFromCsfd = new CancellationTokenSource();

          var csfdTvShow = cSfdWebsiteScrapper.LoadTvShow(csfUrl, ctsUpdateTvShowFromCsfd.Token);

          if (csfdTvShow == null)
          {
            statusMessage = new StatusMessageViewModel(1)
            {
              Status = StatusType.Failed,
              Message = "Unable to scrape Tv show"
            };

            statusManager.UpdateMessage(statusMessage);

            dbTvShow.InfoDownloadStatus = InfoDownloadStatus.Failed;

           

            return;
          }

          dbTvShow.PosterPath = csfdTvShow.ImagePath;

          dbTvShow.Name = csfdTvShow.Name;

          foreach (var season in dbTvShow.Seasons)
          {
            logger.Log(MessageType.Inform, "Updating: Season " + season.SeasonNumber);

            if (csfdTvShow.Seasons.Count >= season.SeasonNumber)
            {
              foreach (var episode in season.Episodes)
              {
                if (season.SeasonNumber > 0 && csfdTvShow.Seasons[season.SeasonNumber - 1].SeasonEpisodes.Count >= episode.EpisodeNumber)
                {
                  var csfdEpisode = csfdTvShow.Seasons[season.SeasonNumber - 1].SeasonEpisodes[episode.EpisodeNumber - 1];

                  episode.VideoItem.Name = csfdEpisode.Name;

                  episode.InfoDownloadStatus = InfoDownloadStatus.Downloaded;
                }
                else
                {
                  episode.InfoDownloadStatus = InfoDownloadStatus.Failed;
                }
              }

              season.InfoDownloadStatus = InfoDownloadStatus.Downloaded;
            }
            else
            {
              season.InfoDownloadStatus = InfoDownloadStatus.Failed;
            }
          }

          dbTvShow.InfoDownloadStatus = InfoDownloadStatus.Downloaded;

          await storageManager.DeepUpdateTvShow(dbTvShow);

          statusMessage = new StatusMessageViewModel(1)
          {
            Status = StatusType.Processing,
            Message = "Updating tv show in database"
          };

          statusManager.UpdateMessage(statusMessage);

          statusMessage.ProcessedCount++;

          statusMessage.Status = StatusType.Done;

          statusManager.UpdateMessage(statusMessage);
        }
        catch (Exception ex)
        {
          var statusMessage = new StatusMessageViewModel(1)
          {
            Status = StatusType.Error,
            Message = "Updating tv show in database"
          };

          statusManager.UpdateMessage(statusMessage);

          statusMessage.ProcessedCount++;

          logger.Log(ex);
        }
      });
    }

    #endregion

    private CancellationTokenSource ctsUpdateTvShowSeasonFromCsfd;
    public Task UpdateTvShowSeasonFromCsfd(int tvShowSeasonId, string csfUrl)
    {
      return Task.Run(async () =>
      {
        try
        {
          var dbTvShowSeason = storageManager.GetTempRepository<TvShowSeason>().Include(x => x.Episodes).ThenInclude(x => x.VideoItem).Single(x => x.Id == tvShowSeasonId);

          dbTvShowSeason.InfoDownloadStatus = InfoDownloadStatus.Downloading;

          Application.Current.Dispatcher.Invoke(() =>
          {
            storageManager.PublishItemChanged(dbTvShowSeason);
          });

          ctsUpdateTvShowSeasonFromCsfd?.Cancel();
          ctsUpdateTvShowSeasonFromCsfd = new CancellationTokenSource();

          var csfdtvShowSeason = cSfdWebsiteScrapper.LoadTvShowSeason(csfUrl, ctsUpdateTvShowSeasonFromCsfd.Token);

          if (csfdtvShowSeason == null)
          {
            var statusMessage1 = new StatusMessageViewModel(1)
            {
              Status = StatusType.Failed,
              Message = "Unable to scrape Tv show season"
            };

            statusManager.UpdateMessage(statusMessage1);

            dbTvShowSeason.InfoDownloadStatus = InfoDownloadStatus.Failed;
            return;
          }


          dbTvShowSeason.Name = csfdtvShowSeason.Name;

          foreach (var episode in dbTvShowSeason.Episodes)
          {
            if (csfdtvShowSeason.SeasonEpisodes.Count >= episode.EpisodeNumber)
            {
              var csfdEpisode = csfdtvShowSeason.SeasonEpisodes[episode.EpisodeNumber - 1];

              episode.VideoItem.Name = csfdEpisode.Name;

              episode.InfoDownloadStatus = InfoDownloadStatus.Downloaded;
            }
            else
            {
              episode.InfoDownloadStatus = InfoDownloadStatus.Failed;
            }
          }

          dbTvShowSeason.InfoDownloadStatus = InfoDownloadStatus.Downloaded;

          await storageManager.UpdateEntityAsync(dbTvShowSeason);

          var statusMessage = new StatusMessageViewModel(1)
          {
            Status = StatusType.Processing,
            Message = "Updating tv show season in database"
          };

          statusManager.UpdateMessage(statusMessage);

          statusMessage.ProcessedCount++;

          statusMessage.Status = StatusType.Done;

          statusManager.UpdateMessage(statusMessage);
        }
        catch (Exception ex)
        {
          var statusMessage = new StatusMessageViewModel(1)
          {
            Status = StatusType.Error,
            Message = "Updating tv show season in database"
          };

          statusManager.UpdateMessage(statusMessage);

          statusMessage.ProcessedCount++;

          logger.Log(ex);
        }
      });
    }

    #region UpdateTvShowName

    public Task UpdateTvShowName(int tvShowId, string name)
    {
      return Task.Run(() =>
      {
        var dbTvShow = storageManager.GetTempRepository<TvShow>().Include(x => x.Seasons).ThenInclude(x => x.Episodes).Single(x => x.Id == tvShowId);

        dbTvShow.Name = name;

        storageManager.UpdateEntityAsync(dbTvShow);
      });
    }

    #endregion

    #region UpdateTvShowCsfdUrl

    public Task UpdateTvShowCsfdUrl(int tvShowId, string url)
    {
      return Task.Run(async () =>
      {
        var dbTvShow = storageManager.GetTempRepository<TvShow>().Include(x => x.Seasons).ThenInclude(x => x.Episodes).Single(x => x.Id == tvShowId);

        dbTvShow.CsfdUrl = url;

        await storageManager.UpdateEntityAsync(dbTvShow);
      });
    }

    #endregion

  }
}