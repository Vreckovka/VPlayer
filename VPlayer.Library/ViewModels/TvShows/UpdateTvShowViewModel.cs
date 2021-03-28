using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Logger;
using Microsoft.WindowsAPICodePack.Dialogs;
using VCore;
using VCore.ViewModels;
using VPlayer.AudioStorage.DataLoader;
using VPlayer.AudioStorage.DomainClasses.Video;
using VPlayer.AudioStorage.Parsers;

namespace VPlayer.Library.ViewModels.TvShows
{
  public class UpdateTvShowViewModel : BaseWindowViewModel
  {
    private readonly ITvShowScrapper tVShowScrapper;
    private readonly ILogger logger;
    private readonly TvShow tvShow;

    public UpdateTvShowViewModel(
      ITvShowScrapper tVShowScrapper,
       ILogger logger,
       TvShow tvShow)
    {
      this.tVShowScrapper = tVShowScrapper ?? throw new ArgumentNullException(nameof(tVShowScrapper));
      this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
      this.tvShow = tvShow ?? throw new ArgumentNullException(nameof(tvShow));

      save = new ActionCommand(OnSave, () => !string.IsNullOrEmpty(TvShowCsfdUrl) && !string.IsNullOrEmpty(Name));

      Name = tvShow.Name;
      TvShowCsfdUrl = tvShow.CsfdUrl;
    }


    #region Name

    private string name;
    public string Name
    {
      get { return name; }
      set
      {
        if (value != name)
        {
          name = value;
          save.RaiseCanExecuteChanged();
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region TvShowCsfdUrl

    private string csfdUrl;
    public string TvShowCsfdUrl
    {
      get { return csfdUrl; }
      set
      {
        if (value != csfdUrl)
        {
          csfdUrl = value;
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

    #region Save

    private ActionCommand save;
    public ICommand Save => save;

    public void OnSave()
    {
      IsLoading = true;

      Task.Run(async () =>
      {
        try
        {
          if (tvShow.Name != Name)
          {
            await tVShowScrapper.UpdateTvShowName(tvShow.Id, Name);
          }

          if (tvShow.CsfdUrl != TvShowCsfdUrl)
          {
            await tVShowScrapper.UpdateTvShowCsfdUrl(tvShow.Id, TvShowCsfdUrl);
          }

          tVShowScrapper.UpdateTvShowFromCsfd(tvShow.Id, TvShowCsfdUrl);
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

