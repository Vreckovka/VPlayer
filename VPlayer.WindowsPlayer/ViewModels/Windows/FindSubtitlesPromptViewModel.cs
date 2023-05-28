using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using MovieCollection.OpenSubtitles;
using MovieCollection.OpenSubtitles.Models;
using VCore.WPF.Misc;
using VCore.WPF.Prompts;
using VPlayer.AudioStorage.DataLoader;
using VPlayer.Core.ViewModels.TvShows;

namespace VPlayer.WindowsPlayer.ViewModels.Windows
{
  public class FindSubtitlesPromptViewModel : BasePromptViewModel<VideoItemInPlaylistViewModel>
  {
    private static readonly HttpClient httpClient = new HttpClient();
    private OpenSubtitlesService openSubtitlesService;
    

    public FindSubtitlesPromptViewModel(VideoItemInPlaylistViewModel model) : base(model)
    {
      CancelVisibility = System.Windows.Visibility.Visible;


      var openSubtitlesOptions = new OpenSubtitlesOptions
      {
        ApiKey = "eTQWO74msW2zmCQwoYPQcd4BlGdVy39J",
        ProductInformation = new ProductHeaderValue("VPlayer", "0.1.0"),
      };

      openSubtitlesService = new OpenSubtitlesService(httpClient, openSubtitlesOptions);
      SearchSubtitles(model.Name);

      SearchText = model.Name;

    }

    public Download Download { get; set; }



    #region SearchText

    private string searchText;

    public string SearchText
    {
      get { return searchText; }
      set
      {
        if (value != searchText)
        {
          searchText = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region FindSubtitles

    private ActionCommand findSubtitles;

    public ICommand FindSubtitles
    {
      get
      {
        if (findSubtitles == null)
        {
          findSubtitles = new ActionCommand(OnFindSubtitles);
        }

        return findSubtitles;
      }
    }

    public async void OnFindSubtitles()
    {
      var acutalEpisode = DataLoader.GetTvShowSeriesNumber(SearchText);

      if(acutalEpisode?.EpisodeNumber != null)
      {
        await SearchSubtitles(acutalEpisode.ParsedName, acutalEpisode.SeasonNumber, acutalEpisode.EpisodeNumber);
      }
      else
      {
        await SearchSubtitles(SearchText);
      }
    }

    #endregion

    #region DownloadSubtitles

    private ActionCommand<Subtitle> downloadSubtitles;

    public ICommand DownloadSubtitles
    {
      get
      {
        if (downloadSubtitles == null)
        {
          downloadSubtitles = new ActionCommand<Subtitle>(OnDownloadSubtitles);
        }

        return downloadSubtitles;
      }
    }

    public async void OnDownloadSubtitles(Subtitle subtitle)
    {
      await GetSubtitles(subtitle.Files?.FirstOrDefault()?.FileId);
    }

    #endregion

    public ObservableCollection<Subtitle> Subtitles { get; } = new ObservableCollection<Subtitle>();

    #region SearchSubtitles

    private async Task SearchSubtitles(string name, int? seasonNumber = null, int? epizodeNnumber = null)
    {
      Subtitles.Clear();

      var search = new NewSubtitleSearch
      {
        Query = name,
        SeasonNumber = seasonNumber,
        EpisodeNumber = epizodeNnumber,
        Languages = new List<string>()
        {
          "en",
          "sk",
          "cs"
        }
      };

      var result = (await openSubtitlesService.SearchSubtitlesAsync(search))?.Data?.Select(x => x.Attributes)?.ToList();

      if (result != null)
      {
        Subtitles.AddRange(result);
      }
    }

    #endregion

    private async Task GetSubtitles(int? fileId)
    {
      if(fileId != null)
      {
        var newDownload = new NewDownload()
        {
          FileId = fileId.Value
        };

        Download = await openSubtitlesService.GetSubtitleForDownloadAsync(newDownload);
        OnClose(Window);
      }
    }
  }
}
