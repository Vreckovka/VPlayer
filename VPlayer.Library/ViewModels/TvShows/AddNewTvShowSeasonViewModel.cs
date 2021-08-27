using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Logger;
using Microsoft.WindowsAPICodePack.Dialogs;
using VCore;
using VCore.Standard.NewFolder;
using VCore.ViewModels;
using VPlayer.AudioStorage.DataLoader;
using VPlayer.AudioStorage.DomainClasses.Video;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core;
using VPlayer.Library.ViewModels.TvShows;

namespace VPlayer.Home.ViewModels.TvShows
{
  public class AddNewTvShowSeasonViewModel : BaseWindowViewModel
  {
    private readonly DataLoader dataLoader;
    private readonly TvShow tvShow;
    private readonly IStorageManager storageManager;
    private readonly ITvShowScrapper tvShowScrapper;
    private readonly ISettingsProvider settingsProvider;
    private readonly ILogger logger;

    public AddNewTvShowSeasonViewModel(
      DataLoader dataLoader,
      TvShow tvShow,
      IStorageManager storageManager,
      ITvShowScrapper tvShowScrapper,
      ISettingsProvider settingsProvider,
      ILogger logger)
    {
      this.dataLoader = dataLoader ?? throw new ArgumentNullException(nameof(dataLoader));
      this.tvShow = tvShow ?? throw new ArgumentNullException(nameof(tvShow));
      this.storageManager = storageManager ?? throw new ArgumentNullException(nameof(storageManager));
      this.tvShowScrapper = tvShowScrapper ?? throw new ArgumentNullException(nameof(tvShowScrapper));
      this.settingsProvider = settingsProvider ?? throw new ArgumentNullException(nameof(settingsProvider));
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


      string initialDirectory = null;

      if (tvShow.Seasons != null && tvShow.Seasons.Count > 0)
      {
        var episode = tvShow.Seasons[0].Episodes?.FirstOrDefault();

        initialDirectory = new DirectoryInfo(episode.Source).Parent?.Parent?.FullName;
      }


      if (string.IsNullOrEmpty(initialDirectory))
        initialDirectory = settingsProvider.GetSetting(GlobalSettings.TvShowInitialDirectory)?.Value;

      dialog.AllowNonFileSystemItems = true;
      dialog.IsFolderPicker = true;
      dialog.InitialDirectory = initialDirectory;
      dialog.Title = "Select folder with tv show season";

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
          load = new ActionCommand(OnLoad, () => !string.IsNullOrEmpty(TvShowPath) && !string.IsNullOrEmpty(TvShowCsfdUrl));
        }

        return load;
      }
    }

    public async void OnLoad()
    {
      IsLoading = true;

      await Task.Run(async () =>
      {
        try
        {
          dataLoader.AddtvTvShowSeasons(TvShowPath, tvShow);

          var single = tvShow.Seasons.SingleOrDefault(x => x.Created == null);

          if (single == null)
            single = tvShow.Seasons.OrderByDescending(x => x.Created).FirstOrDefault();

          var id = await storageManager.DeepUpdateTvShow(tvShow);

          await tvShowScrapper.UpdateTvShowSeasonFromCsfd(single.Id, TvShowCsfdUrl);

        }
        catch (Exception ex)
        {
          logger.Log(ex);
        }
      });

      IsLoading = false;
      Window?.Close();
    }

    #endregion

    #endregion

  }
}