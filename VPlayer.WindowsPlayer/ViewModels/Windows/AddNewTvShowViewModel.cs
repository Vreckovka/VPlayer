using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Logger;
using Microsoft.EntityFrameworkCore;
using Microsoft.WindowsAPICodePack.Dialogs;
using VCore;
using VCore.Standard;
using VCore.Standard.Providers;
using VCore.ViewModels;
using VPlayer.AudioStorage.DataLoader;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core;
using VPlayer.Library.ViewModels.TvShows;

namespace VPlayer.WindowsPlayer.ViewModels.Windows
{
  public class AddNewTvShowViewModel : BaseWindowViewModel
  {
    private readonly DataLoader dataLoader;
    private readonly IStorageManager storageManager;
    private readonly ITvShowScrapper tvShowScrapper;
    private readonly ISettingsProvider settingsProvider;
    private readonly ILogger logger;

    public AddNewTvShowViewModel(
       DataLoader dataLoader,
       IStorageManager storageManager,
       ITvShowScrapper tvShowScrapper,
       ISettingsProvider settingsProvider,
       ILogger logger)
    {
      this.dataLoader = dataLoader ?? throw new ArgumentNullException(nameof(dataLoader));
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

    private string tvShowCsfdUrl = "https://www.csfd.cz/";

    public string TvShowCsfdUrl
    {
      get { return tvShowCsfdUrl; }
      set
      {
        if (value != tvShowCsfdUrl)
        {
          tvShowCsfdUrl = value;

          if (string.IsNullOrEmpty(TemporaryName))
          {
            TemporaryName = value;
          }

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
      dialog.InitialDirectory = settingsProvider.GetSetting(GlobalSettings.TvShowInitialDirectory)?.Value;
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

          tvShow.CsfdUrl = TvShowCsfdUrl;

          var id = await storageManager.StoreTvShow(tvShow);

          await tvShowScrapper.UpdateTvShowFromCsfd(id, TvShowCsfdUrl);

          IsLoading = false;
        }
        catch (Exception ex)
        {
          logger.Log(ex);
        }
      });

      Window?.Close();

    }

    #endregion

    #endregion

  }
}






