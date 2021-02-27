using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Logger;
using Microsoft.EntityFrameworkCore;
using Microsoft.WindowsAPICodePack.Dialogs;
using VCore;
using VCore.Annotations;
using VCore.ViewModels;
using VPlayer.AudioStorage.DataLoader;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.DomainClasses.Video;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.AudioStorage.Parsers;

namespace VPlayer.Library.ViewModels.TvShows
{
  public class UpdateTvShowViewModel : BaseWindowViewModel
  {
    private readonly DataLoader dataLoader;
    private readonly IStorageManager storageManager;
    private readonly ICSFDWebsiteScrapper cSfdWebsiteScrapper;
    private readonly ILogger logger;
    private readonly TvShow tvShow;

    public UpdateTvShowViewModel(
      [NotNull] DataLoader dataLoader,
      [NotNull] IStorageManager storageManager,
      [NotNull] ICSFDWebsiteScrapper cSfdWebsiteScrapper,
      [NotNull] ILogger logger,
      [NotNull] TvShow tvShow)
    {
      this.dataLoader = dataLoader ?? throw new ArgumentNullException(nameof(dataLoader));
      this.storageManager = storageManager ?? throw new ArgumentNullException(nameof(storageManager));
      this.cSfdWebsiteScrapper = cSfdWebsiteScrapper ?? throw new ArgumentNullException(nameof(cSfdWebsiteScrapper));
      this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
      this.tvShow = tvShow ?? throw new ArgumentNullException(nameof(tvShow));
    }


    #region Name

    public string Name
    {
      get { return tvShow.Name; }
      set
      {
        if (value != tvShow.Name)
        {
          tvShow.Name = value;
          save.RaiseCanExecuteChanged();
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region TvShowCsfdUrl

    private string tvShowCsfdUrl;

    public string TvShowCsfdUrl
    {
      get { return tvShowCsfdUrl; }
      set
      {
        if (value != tvShowCsfdUrl)
        {
          tvShowCsfdUrl = value;
          save.RaiseCanExecuteChanged();
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region IsLoading

    private bool isLoading;

    public bool IsLoading
    {
      get { return isLoading; }
      set
      {
        if (value != isLoading)
        {
          isLoading = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region Commands
    
    #region Load

    private ActionCommand save;
    public ICommand Save
    {
      get
      {
        if (save == null)
        {
          save = new ActionCommand(OnLoad, () => !string.IsNullOrEmpty(TvShowCsfdUrl) && !string.IsNullOrEmpty(Name));
        }

        return save;
      }
    }

    public void OnLoad()
    {
      Task.Run(async () =>
      {
        try
        {
          IsLoading = true;

          ScrappeTvShow(tvShow.Id);

          IsLoading = false;
        }
        catch (Exception ex)
        {

          Console.WriteLine(ex);
        }
      });

    }

    #endregion

    #endregion

    #region ScrappeTvShow

    private void ScrappeTvShow(int tvShowId)
    {
      Task.Run(async () =>
      {
        try
        {
          var dbTvShow = storageManager.GetRepository<TvShow>().Include(x => x.Episodes).Single(x => x.Id == tvShowId);

          dbTvShow.InfoDownloadStatus = InfoDownloadStatus.Downloading;

          Application.Current.Dispatcher.Invoke(() =>
          {
            storageManager.ItemChanged.OnNext(new VCore.Modularity.Events.ItemChanged()
            {
              Changed = VCore.Modularity.Events.Changed.Updated,
              Item = dbTvShow
            });
          });
      
          var csfdTvShow = cSfdWebsiteScrapper.LoadTvShow(TvShowCsfdUrl);

          dbTvShow.Name = tvShow.Name;

          foreach (var episode in dbTvShow.Episodes)
          {
            Console.WriteLine(episode.SeasonNumber + "x" + episode.EpisodeNumber);

            if (csfdTvShow.Seasons.Count > episode.SeasonNumber)
            {
              if (csfdTvShow.Seasons[episode.SeasonNumber - 1].SeasonEpisodes.Count > episode.EpisodeNumber)
              {
                var csfdEpisode = csfdTvShow.Seasons[episode.SeasonNumber - 1].SeasonEpisodes[episode.EpisodeNumber - 1];

                episode.Name = csfdEpisode.Name;
                episode.InfoDownloadStatus = InfoDownloadStatus.Downloaded;
              }
              else
              {
                episode.InfoDownloadStatus = InfoDownloadStatus.Failed;
              }
            }
            else
            {
              episode.InfoDownloadStatus = InfoDownloadStatus.Failed;
            }


          }

          dbTvShow.InfoDownloadStatus = InfoDownloadStatus.Downloaded;

          await storageManager.UpdateWholeTvShow(dbTvShow);
        }
        catch (Exception ex)
        {
          logger.Log(ex);
        }
      });
    }

    #endregion

  }
}

