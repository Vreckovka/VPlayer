using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Logger;
using Microsoft.EntityFrameworkCore;
using Microsoft.WindowsAPICodePack.Dialogs;
using VCore;
using VCore.Annotations;
using VCore.Standard;
using VCore.ViewModels;
using VPlayer.AudioStorage.DataLoader;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.DomainClasses.Video;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.AudioStorage.Parsers;

namespace VPlayer.WindowsPlayer.ViewModels.Windows
{
  public class AddNewTvShowViewModel : BaseWindowViewModel
  {
    private readonly DataLoader dataLoader;
    private readonly IStorageManager storageManager;
    private readonly ICSFDWebsiteScrapper cSfdWebsiteScrapper;
    private readonly ILogger logger;

    public AddNewTvShowViewModel(
      [NotNull] DataLoader dataLoader,
      [NotNull] IStorageManager storageManager,
      [NotNull] ICSFDWebsiteScrapper cSfdWebsiteScrapper,
      [NotNull] ILogger logger)
    {
      this.dataLoader = dataLoader ?? throw new ArgumentNullException(nameof(dataLoader));
      this.storageManager = storageManager ?? throw new ArgumentNullException(nameof(storageManager));
      this.cSfdWebsiteScrapper = cSfdWebsiteScrapper ?? throw new ArgumentNullException(nameof(cSfdWebsiteScrapper));
      this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }


    #region TvShowPath

    private string tvShowPath;

    public string TvShowPath
    {
      get { return tvShowPath; }
      set
      {
        if (value != tvShowPath)
        {
          tvShowPath = value;
          load.RaiseCanExecuteChanged();
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region TemporaryName

    private string temporaryName;

    public string TemporaryName
    {
      get { return temporaryName; }
      set
      {
        if (value != temporaryName)
        {
          temporaryName = value;
          load.RaiseCanExecuteChanged();
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
          load.RaiseCanExecuteChanged();
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

    #region ChoosePath

    private ActionCommand choosePath;

    public ICommand ChoosePath
    {
      get
      {
        if (choosePath == null)
        {
          choosePath = new ActionCommand(OnLoadLoadTvShow);
        }

        return choosePath;
      }
    }

    public void OnLoadLoadTvShow()
    {
      CommonOpenFileDialog dialog = new CommonOpenFileDialog();

      dialog.AllowNonFileSystemItems = true;
      dialog.IsFolderPicker = true;
      dialog.Title = "Select folders with tv show";

      if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
      {
        TvShowPath = dialog.FileName;
      }

      TopMost = true;
      TopMost = false;
    }

    #endregion

    #region Load

    private ActionCommand load;
    public ICommand Load
    {
      get
      {
        if (load == null)
        {
          load = new ActionCommand(OnLoad, () => !string.IsNullOrEmpty(TvShowPath) && !string.IsNullOrEmpty(TvShowCsfdUrl) && !string.IsNullOrEmpty(TemporaryName));
        }

        return load;
      }
    }

    public void OnLoad()
    {
      Task.Run(async () =>
      {
        try
        {
          IsLoading = true;

          var tvShow = dataLoader.LoadTvShow(TemporaryName, TvShowPath);

          var id = await storageManager.StoreTvShow(tvShow);

          ScrappeTvShow(id);

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

          dbTvShow.Name = csfdTvShow.Name;

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

