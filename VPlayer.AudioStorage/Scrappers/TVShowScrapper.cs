using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Logger;
using Microsoft.EntityFrameworkCore;
using VCore.Annotations;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.DomainClasses.Video;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.AudioStorage.Parsers;
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
      [NotNull] IStorageManager storageManager, 
      [NotNull] ICSFDWebsiteScrapper cSfdWebsiteScrapper, 
      [NotNull] ILogger logger,
      [NotNull] IStatusManager statusManager)
    {
      this.storageManager = storageManager ?? throw new ArgumentNullException(nameof(storageManager));
      this.cSfdWebsiteScrapper = cSfdWebsiteScrapper ?? throw new ArgumentNullException(nameof(cSfdWebsiteScrapper));
      this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
      this.statusManager = statusManager ?? throw new ArgumentNullException(nameof(statusManager));
    }

    #region UpdateTvShowFromCsfd

    public Task UpdateTvShowFromCsfd(int tvShowId, string csfUrl)
    {
      return Task.Run(async () =>
      {
        try
        {
          var dbTvShow = storageManager.GetRepository<TvShow>().Include(x => x.Seasons).ThenInclude(x => x.Episodes).Single(x => x.Id == tvShowId);

          dbTvShow.InfoDownloadStatus = InfoDownloadStatus.Downloading;
         
          Application.Current.Dispatcher.Invoke(() =>
          {
            storageManager.ItemChanged.OnNext(new VCore.Modularity.Events.ItemChanged()
            {
              Changed = VCore.Modularity.Events.Changed.Updated,
              Item = dbTvShow
            });
          });

          var csfdTvShow = cSfdWebsiteScrapper.LoadTvShow(csfUrl);

          dbTvShow.PosterPath = csfdTvShow.PosterPath;

          dbTvShow.Name = csfdTvShow.Name;

          foreach (var season in dbTvShow.Seasons)
          {
            logger.Log(MessageType.Inform, "Updating: Season " + season.SeasonNumber);

            if (csfdTvShow.Seasons.Count > season.SeasonNumber)
            {
              foreach (var episode in season.Episodes)
              {
                if (csfdTvShow.Seasons[season.SeasonNumber - 1].SeasonEpisodes.Count >= episode.EpisodeNumber)
                {
                  var csfdEpisode = csfdTvShow.Seasons[season.SeasonNumber - 1].SeasonEpisodes[episode.EpisodeNumber - 1];

                  episode.Name = csfdEpisode.Name;

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

          await storageManager.UpdateWholeTvShow(dbTvShow);

          var statusMessage = new StatusMessage(1)
          {
            ActualMessageStatusState = MessageStatusState.Processing,
            Message = "Updating tv show in database"
          };

          statusMessage.ProcessedCount++;
          statusMessage.ActualMessageStatusState = MessageStatusState.Done;

          statusManager.UpdateMessage(statusMessage);
        }
        catch (Exception ex)
        {
          logger.Log(ex);
        }
      });
    }

    #endregion

    #region UpdateTvShowName

    public Task UpdateTvShowName(int tvShowId, string name)
    {
      return Task.Run(() =>
      {
        var dbTvShow = storageManager.GetRepository<TvShow>().Include(x => x.Seasons).ThenInclude(x => x.Episodes).Single(x => x.Id == tvShowId);

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
        var dbTvShow = storageManager.GetRepository<TvShow>().Include(x => x.Seasons).ThenInclude(x => x.Episodes).Single(x => x.Id == tvShowId);

        dbTvShow.CsfdUrl = url;

        await storageManager.UpdateEntityAsync(dbTvShow);
      });
    }

    #endregion

  }
}