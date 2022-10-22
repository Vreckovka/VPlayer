using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ChromeDriverScrapper;
using HtmlAgilityPack;
using Logger;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Remote;
using OpenQA.Selenium.Support.UI;
using VCore;
using VCore.WPF.Controls.StatusMessage;
using VCore.WPF.Interfaces.Managers;
using VPlayer.AudioStorage.DataLoader;
using VPlayer.AudioStorage.InfoDownloader;
using VPlayer.AudioStorage.Scrappers.CSFD.Domain;
using VPlayer.Core.Managers.Status;

namespace VPlayer.AudioStorage.Scrappers.CSFD
{
  public class CSFDWebsiteScrapper : ICSFDWebsiteScrapper
  {
    private readonly ILogger logger;
    private readonly IStatusManager statusManager;
    public readonly IChromeDriverProvider chromeDriverProvider;
    private readonly IWindowManager windowManager;
    private string baseUrl = "https://www.csfd.sk/";

    public CSFDWebsiteScrapper(ILogger logger, IStatusManager statusManager, IChromeDriverProvider chromeDriverProvider, IWindowManager windowManager)
    {
      this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
      this.statusManager = statusManager ?? throw new ArgumentNullException(nameof(statusManager));
      this.chromeDriverProvider = chromeDriverProvider ?? throw new ArgumentNullException(nameof(chromeDriverProvider));
      this.windowManager = windowManager ?? throw new ArgumentNullException(nameof(windowManager));
    }


    #region LoadTvShow

    public CSFDTVShow LoadTvShow(
      string url,
      CancellationToken cancellationToken,
      int? seasonNumber = null,
      int? episodeNumber = null,
      StatusMessageViewModel parentMessage = null,
      string fileName = null)
    {
      var nameF = url
        .Replace("https://www.csfd.cz/film/", null)
        .Replace("https://csfd.cz/film/", null)
        .Replace("/recenze", null)
        .Replace("/prehled", null)
        .Replace("/", null);

      var statusMessage = new StatusMessageViewModel(3)
      {
        Status = StatusType.Processing,
        Message = $"Downloading {nameF}"
      };

      statusMessage.CopyParentState(parentMessage);

      try
      {
        if (!chromeDriverProvider.Initialize())
        {
          return null;
        }

        var tvShow = new CSFDTVShow()
        {
          Url = url
        };

        statusManager.UpdateMessage(statusMessage);

        var document = GetItemMainPage(url, cancellationToken);

        var name = GetItemName(document, out var posterUrl);

        if (name != null)
        {
          tvShow.Name = name;

          statusManager.UpdateMessageAndIncreaseProcessCount(statusMessage);


          var poster = DownloadPoster(posterUrl, cancellationToken);

          if (poster != null)
          {
            tvShow.ImagePath = SaveImage(tvShow.Name, poster, cancellationToken);
          }

          statusManager.UpdateMessageAndIncreaseProcessCount(statusMessage);

          tvShow.Seasons = LoadSeasons(document, statusMessage, seasonNumber, episodeNumber, cancellationToken, fileName: fileName);

          if (tvShow.Seasons == null)
          {
            return tvShow;
          }

          foreach (var item in tvShow.Seasons.Where(x => x.SeasonEpisodes != null).SelectMany(x => x.SeasonEpisodes))
          {
            item.ImagePath = tvShow.ImagePath;
          }

          tvShow.Rating = GetCsfdRating(tvShow);

          statusManager.UpdateMessageAndIncreaseProcessCount(statusMessage);

          return tvShow;
        }

        statusMessage.Status = StatusType.Failed;
        statusManager.UpdateMessage(statusMessage);

        return null;
      }
      catch (Exception ex)
      {
        if (statusMessage.MessageState != MessageStatusState.Closed)
          statusManager.ShowErrorMessage(ex);

        return null;
      }
    }

    #endregion

    #region LoadTvShowSeason

    public CSFDTVShowSeason LoadTvShowSeason(string url, CancellationToken cancellationToken)
    {
      if (!chromeDriverProvider.Initialize())
      {
        return null;
      };

      var statusMessage = new StatusMessageViewModel(2)
      {
        Status = StatusType.Processing,
        Message = "Downloading tv show season"
      };

      statusManager.UpdateMessage(statusMessage);

      var document = new HtmlDocument();

      document.LoadHtml(chromeDriverProvider.ChromeDriver.PageSource);

      statusMessage = new StatusMessageViewModel(1)
      {
        Status = StatusType.Processing,
        Message = "Downloading tv show seasons"
      };

      statusManager.UpdateMessage(statusMessage);


      var newSeason = new CSFDTVShowSeason();

      newSeason.Name = document.DocumentNode.SelectSingleNode("").InnerText;
      newSeason.Url = url;
      newSeason.SeasonNumber = 0;

      logger.Log(MessageType.Success, $"Tv show season: {newSeason.Name}");

      newSeason.SeasonEpisodes = LoadSeasonEpisodes(newSeason.SeasonNumber, newSeason.Url, statusMessage);

      statusManager.UpdateMessageAndIncreaseProcessCount(statusMessage);

      return newSeason;
    }

    #endregion

    #region GetItemName

    private string GetItemName(HtmlDocument document, out string posterUrl)
    {
      var node = TrySelectNodes(document.DocumentNode, "/div/div[1]/div/div[1]/div/div/header/div/h1")?.FirstOrDefault()?.InnerText;

      if (node != null)
      {
        var name = node.Replace("\t", string.Empty).Replace(" (TV seriál)", string.Empty).Replace("\r", null).Replace("\n", null);

        logger.Log(MessageType.Success, $"Tv show name: {name}");

        var posterNode = TrySelectNodes(document.DocumentNode, "/div/div[1]/div/div[1]/div/div/div[1]/div[1]/a/img")?.FirstOrDefault();

        posterUrl = posterNode?.Attributes.SingleOrDefault(x => x.Name == "src")?.Value.Replace("//image.pmgstatic.com", "https://image.pmgstatic.com");

        return name;
      }

      posterUrl = null;
      return null;
    }

    #endregion

    #region DownloadPoster

    private byte[] DownloadPoster(string coverUrl, CancellationToken cancellationToken)
    {
      if (string.IsNullOrEmpty(coverUrl))
        return null;

      using (var client = new WebClient())
      {
        cancellationToken.ThrowIfCancellationRequested();

        var downloadedCover = client.DownloadData(coverUrl);

        return downloadedCover;
      }
    }

    #endregion

    #region SaveImage

    private string SaveImage(string tvShowName, byte[] image, CancellationToken cancellationToken)
    {
      MemoryStream ms = new MemoryStream(image);
      Image i = Image.FromStream(ms);

      var directory = Path.Combine(AudioInfoDownloader.GetDefaultPicturesPath(), $"TvShows\\{AudioInfoDownloader.GetPathValidName(tvShowName)}");
      var finalPath = Path.Combine(directory, "poster.jpg");

      try
      {
        cancellationToken.ThrowIfCancellationRequested();

        finalPath.EnsureDirectoryExists();

        if (File.Exists(finalPath))
        {
          cancellationToken.ThrowIfCancellationRequested();

          File.Delete(finalPath);
        }

        cancellationToken.ThrowIfCancellationRequested();

        i.Save(finalPath, ImageFormat.Jpeg);
        i.Dispose();
      }
      catch (Exception ex)
      {
      }

      return finalPath;
    }

    #endregion

    #region LoadSeasons

    private List<CSFDTVShowSeason> LoadSeasons(
      HtmlDocument document,
      StatusMessageViewModel pStatusMessageViewModel,
      int? seasonNumber,
      int? episodeNumber,
      CancellationToken cancellationToken,
      int? pageNumber = null,
      string fileName = null)
    {
      var seasons = new List<CSFDTVShowSeason>();

      cancellationToken.ThrowIfCancellationRequested();
      
      var nodes = TrySelectNodes(document.DocumentNode, "/div/div[1]/div/section[1]/div[2]/div/ul/li/h3/a");

      if (nodes == null)
        return null;

      var statusMessageViewModel = new StatusMessageViewModel(nodes.Count)
      {
        Status = StatusType.Processing,
        Message = "Downloading tv show seasons",
      };

      statusMessageViewModel.CopyParentState(pStatusMessageViewModel);

      statusManager.UpdateMessage(statusMessageViewModel);

      foreach (var node in nodes)
      {
        var newSeason = new CSFDTVShowSeason();

        newSeason.Name = node.InnerText;
        newSeason.Url = baseUrl + node.Attributes.FirstOrDefault(x => x.Name == "href")?.Value;

        seasons.Add(newSeason);

        cancellationToken.ThrowIfCancellationRequested();
        logger.Log(MessageType.Success, $"Tv show season: {newSeason.Name}");
      }

      for (int i = 0; seasons.Count > i; i++)
      {
        seasons[i].SeasonNumber = i + 1;
      }

      cancellationToken.ThrowIfCancellationRequested();

      bool isSingleSeasoned = false;

      nodes = TrySelectNodes(document.DocumentNode, "/div/div[1]/div/section[1]/div[1]/h3");

      if (nodes.Any())
      {
        isSingleSeasoned = !nodes.FirstOrDefault()?.InnerText.Contains("Série") ?? false;
      }

      if (isSingleSeasoned == true)
      {
        var episodes = new List<CSFDTVShowSeasonEpisode>();

        var newSingleSeason = new CSFDTVShowSeason()
        {
          SeasonEpisodes = episodes,
          SeasonNumber = 1
        };

        statusMessageViewModel.Message = "Single seasoned: Downlading episodes";
        statusManager.UpdateMessage(statusMessageViewModel);

        var episodeIndex = 0;

        if (seasonNumber == null && episodeNumber == null && !string.IsNullOrEmpty(fileName))
        {
          if (int.TryParse(Regex.Match(fileName, @"\d+").Value, out var parsedNumber))
          {
            episodeNumber = parsedNumber;
          }
        }

        if (episodeNumber > (seasons.Count) * (pageNumber ?? 1))
        {
          if (pageNumber == null)
          {
            pageNumber = 2;
          }
          else
          {
            pageNumber++;
          }

          var url = chromeDriverProvider.ChromeDriver.Url;

          if (url.Contains("seriePage"))
          {
            url = url.Split("?seriePage")[0] + $"?seriePage={pageNumber}";
          }
          else
          {
            url += $"?seriePage={pageNumber}";
          }

          var newDocument = GetItemMainPage(url, cancellationToken);

          return LoadSeasons(newDocument, pStatusMessageViewModel, seasonNumber, episodeNumber, cancellationToken, pageNumber);
        }

        if (episodeNumber == null || episodeNumber <= 0)
        {
          foreach (var season in seasons)
          {
            var newEpisode = new CSFDTVShowSeasonEpisode();

            newEpisode.Name = season.Name;
            newEpisode.Url = season.Url;

            cancellationToken.ThrowIfCancellationRequested();
            LoadCsfdEpisode(newEpisode);

            if (newEpisode.EpisodeNumber == null)
            {
              newEpisode.EpisodeNumber = episodeIndex + 1;
            }

            episodes.Add(newEpisode);

            statusManager.UpdateMessageAndIncreaseProcessCount(statusMessageViewModel);
            episodeIndex++;
          }
        }
        else
        {
          var index = episodeNumber.Value - (((pageNumber ?? 1) - 1) * 50) - 1;

          var season = seasons[index];

          var newEpisode = new CSFDTVShowSeasonEpisode();

          newEpisode.Name = season.Name;
          newEpisode.Url = season.Url;

          cancellationToken.ThrowIfCancellationRequested();

          LoadCsfdEpisode(newEpisode);

          newEpisode.EpisodeNumber = episodeNumber;

          episodes.Add(newEpisode);

          statusManager.UpdateMessageAndIncreaseProcessCount(statusMessageViewModel);
        }


        return new List<CSFDTVShowSeason>() { newSingleSeason };
      }


      if (seasonNumber == null)
      {
        foreach (var season in seasons)
        {
          cancellationToken.ThrowIfCancellationRequested();

          season.SeasonEpisodes = LoadSeasonEpisodes(season.SeasonNumber, season.Url, statusMessageViewModel);

          statusManager.UpdateMessageAndIncreaseProcessCount(statusMessageViewModel);
        }
      }
      else
      {
        statusMessageViewModel.ProcessedCount = 0;
        statusMessageViewModel.NumberOfProcesses = 1;

        var season = seasons[seasonNumber.Value - 1];

        if (season != null)
        {
          cancellationToken.ThrowIfCancellationRequested();
          season.SeasonEpisodes = LoadSeasonEpisodes(seasonNumber.Value, season.Url, statusMessageViewModel, episodeNumber);
        }
        else
        {
          return null;
        }

        cancellationToken.ThrowIfCancellationRequested();
        statusManager.UpdateMessageAndIncreaseProcessCount(statusMessageViewModel);
      }



      return seasons;
    }

    #endregion

    #region LoadSeasonEpisodes

    private List<CSFDTVShowSeasonEpisode> LoadSeasonEpisodes(int seasonNumber, string url, StatusMessageViewModel pStatusMessageViewModel, int? episodeNumber = null)
    {
      try
      {
        List<CSFDTVShowSeasonEpisode> episodes = new List<CSFDTVShowSeasonEpisode>();
        HtmlDocument document = new HtmlDocument();

        var html = chromeDriverProvider.SafeNavigate(url);

        document.LoadHtml(html);

        var nodes = TrySelectNodes(document.DocumentNode,"/div/div[1]/div[3]/div/ul/li/a");

        if (nodes == null)
        {
          nodes = TrySelectNodes(document.DocumentNode, "/div/div[1]/div/section[1]/div[2]/div/ul/li/h3/a");
        }

        if (nodes == null)
        {
          return episodes;
        }

        var statusMessageViewModel = new StatusMessageViewModel(nodes.Count)
        {
          Status = StatusType.Processing,
          Message = $"Downloading season ({seasonNumber}) episodes",
        };

        statusMessageViewModel.CopyParentState(pStatusMessageViewModel);
        statusManager.UpdateMessage(statusMessageViewModel);


        if (episodeNumber == null)
        {
          foreach (var node in nodes)
          {
            var newEpisode = new CSFDTVShowSeasonEpisode();

            newEpisode.Name = node.InnerText;
            newEpisode.Url = baseUrl + node.Attributes.FirstOrDefault(x => x.Name == "href")?.Value;

            LoadCsfdEpisode(newEpisode);
            episodes.Add(newEpisode);

            logger.Log(MessageType.Success, $"Tv show episode: {newEpisode.Name}");
            statusManager.UpdateMessageAndIncreaseProcessCount(statusMessageViewModel);
          }
        }
        else
        {
          var node = nodes[episodeNumber.Value - 1];
          statusMessageViewModel.ProcessedCount = 0;
          statusMessageViewModel.NumberOfProcesses = 1;

          var newEpisode = new CSFDTVShowSeasonEpisode();

          newEpisode.Name = node.InnerText;
          newEpisode.Url = baseUrl + node.Attributes.FirstOrDefault(x => x.Name == "href")?.Value;

          LoadCsfdEpisode(newEpisode);
          episodes.Add(newEpisode);

          logger.Log(MessageType.Success, $"Tv show episode: {newEpisode.Name}");
          statusManager.UpdateMessageAndIncreaseProcessCount(statusMessageViewModel);
        }


        return episodes;
      }
      catch (Exception ex)
      {
        logger.Log(ex);
        throw;
      }
    }

    #endregion

    #region LoadCsfdEpisode

    private void LoadCsfdEpisode(CSFDTVShowSeasonEpisode cSFDTVShowSeasonEpisode)
    {
      try
      {
        var html = chromeDriverProvider.SafeNavigate(cSFDTVShowSeasonEpisode.Url);

        var document = new HtmlDocument();

        document.LoadHtml(html);
        
        var ratingNode = GetClearText(TrySelectNodes(document.DocumentNode,"/div/div[1]/aside/div/div[2]/div")?.FirstOrDefault()?.InnerText)?.Trim();

        if (ratingNode != "?")
        {
          int.TryParse(ratingNode, out var rating);

          cSFDTVShowSeasonEpisode.Rating = rating;

          if (rating >= 70)
          {
            cSFDTVShowSeasonEpisode.RatingColor = RatingColor.Red;
          }
          else if (rating >= 30)
          {
            cSFDTVShowSeasonEpisode.RatingColor = RatingColor.Blue;
          }
          else if (rating > 0)
          {
            cSFDTVShowSeasonEpisode.RatingColor = RatingColor.Gray;
          }
          else
          {
            cSFDTVShowSeasonEpisode.RatingColor = RatingColor.LightGray;
          }
        }
        else if(ratingNode == "?")
        {
          cSFDTVShowSeasonEpisode.RatingColor = RatingColor.LightGray;
        }

        var originalName = GetClearText(TrySelectNodes(document.DocumentNode, "/div/div[1]/div/div[1]/div[2]/div/header/div/ul/li")?.FirstOrDefault()?.InnerText);

        var seasonNode = TrySelectNodes(document.DocumentNode, "/div/div[1]/div/div[1]/div[2]/div/header/h2")?.FirstOrDefault();

        string tvShowUrl = null;
        if (seasonNode?.ChildNodes.Count > 1)
        {
          var url = seasonNode.ChildNodes[1].Attributes.FirstOrDefault()?.Value;

          if (!string.IsNullOrEmpty(url))
          {
            tvShowUrl = baseUrl + url;
          }
        }

        var nameNode = TrySelectNodes(document.DocumentNode, "/div/div[1]/div/div[1]/div[2]/div/header/div")?.FirstOrDefault();

        List<string> parameters = new List<string>();

        TvShowEpisodeNumbers number = null;

        if (nameNode != null && nameNode.ChildNodes.Count > 3)
        {
          var fullname = GetClearText(nameNode.ChildNodes[1].InnerText);

          number = DataLoader.DataLoader.GetTvShowSeriesNumber(fullname);

          var properties = GetClearText(nameNode.ChildNodes[3].InnerText);

          var regex1 = new Regex(@"\((.*?)\)");

          var ads = regex1.Matches(properties);

          for (int i = 0; i < ads.Count; i++)
          {
            if (ads[i].Groups.Count > 1 && ads[i].Groups[1].Captures.Count > 0)
              parameters.Add(ads[i].Groups[1].Captures[0].Value);
          }
        }


        var infoNode = TrySelectNodes(document.DocumentNode, "/div/div[1]/div/div[1]/div[2]/div/div[2]")?.FirstOrDefault();

        string[] actors = null;
        string[] directors = null;
        string[] generes = null;
        int? year = null;

        if (infoNode != null && infoNode.ChildNodes.Count > 5)
        {
          generes = infoNode.ChildNodes[3].InnerText.Replace("\t", null).Replace("\n", null).Replace("\r", null).Split("/");

          if (infoNode.ChildNodes[5].ChildNodes.Count > 1 && int.TryParse(infoNode.ChildNodes[5].ChildNodes[1].InnerText.Replace(",",null).Trim(), out var year1))
          {
            year = year1;
          }

          var creatorsNode = infoNode.ChildNodes[7];

          if (creatorsNode.ChildNodes.Count > 3)
          {
            var textDirectors = creatorsNode.ChildNodes[3].InnerText;

            if (textDirectors.Contains("Réžia:"))
            {
              directors = textDirectors.Replace("\t", null).Replace("\n", null).Replace("\r", null).Split("Réžia:")[1].Split(",");
            }


            if (creatorsNode.ChildNodes.Count > 11)
            {
              var textActors = creatorsNode.ChildNodes[11].InnerText;

              if (textActors.Contains("Hrajú:"))
              {
                actors = textActors.Replace("\t", null).Replace("\n", null).Replace("\r", null).Split("Hrajú:")[1].Split(",");
              }
            }
          }
        }


        cSFDTVShowSeasonEpisode.TvShowUrl = tvShowUrl;
        cSFDTVShowSeasonEpisode.Year = year;
        cSFDTVShowSeasonEpisode.Actors = actors;
        cSFDTVShowSeasonEpisode.Directors = directors;
        cSFDTVShowSeasonEpisode.OriginalName = originalName?.Replace("(viac" +
          "" +
          "<)", null);
        cSFDTVShowSeasonEpisode.Generes = generes;
        cSFDTVShowSeasonEpisode.SeasonNumber = number?.SeasonNumber;
        cSFDTVShowSeasonEpisode.EpisodeNumber = number?.EpisodeNumber;
        cSFDTVShowSeasonEpisode.Parameters = parameters.ToArray();
      }
      catch (Exception ex)
      {
        logger.Log(ex);
      }
    }

    #endregion

    #region GetItemMainPage

    public HtmlDocument GetItemMainPage(string url, CancellationToken cancellationToken)
    {
      url = url
        .Replace("https://csfd.cz", baseUrl)
        .Replace("https://www.csfd.cz", baseUrl)
        .Replace("https://new.csfd.cz", baseUrl)
        .Replace("https://new.csfd.sk", baseUrl)
        .Replace("https://csfd.sk", baseUrl)
        .Replace("https://www.csfd.sk", baseUrl);


      var html = chromeDriverProvider.SafeNavigate(url);

      var document = new HtmlDocument();

      cancellationToken.ThrowIfCancellationRequested();

      document.LoadHtml(html);

      return document;
    }

    #endregion

    #region GetClearText

    private string GetClearText(string input)
    {
      return input?.Replace("\t", null).Replace("\r", null).Replace("\n", null).Replace("%", null);
    }

    #endregion

    #region FindItems

    private bool chromeDriverInitError;
    public Task<CSFDQueryResult> FindItems(string name, CancellationToken cancellationToken)
    {
      var query = $"{baseUrl}hledat/?q={name}";

      cancellationToken.ThrowIfCancellationRequested();

      var initEx = chromeDriverProvider.InitializeWithExceptionReturn();

      if (initEx != null)
      {
        if (!chromeDriverInitError)
        {
          chromeDriverInitError = true;

          Application.Current?.Dispatcher?.Invoke(() =>
          {
            windowManager.ShowErrorPrompt(initEx.Message);
          });
        }

        return Task.FromResult<CSFDQueryResult>(null);
      }

      return GetItems(query, cancellationToken);
    }

    #endregion

    #region GetBestFind

    private CSFDItem bestItem;
    private string lastParsedName;

    public async Task<CSFDItem> GetBestFind(string name, CancellationToken cancellationToken, int? year = null, bool onlySingleItem = false, string tvShowUrl = null, string tvShowName = null, int? seasonNumber = null, int? episodeNumber = null, bool downloadOneSeason = false)
    {
      var statusMessage = new StatusMessageViewModel(1)
      {
        Status = StatusType.Processing,
        Message = $"Finding {name}"
      };

      statusManager.UpdateMessage(statusMessage);

      var parsedNameMatch = Regex.Match(name, @"(.*?)(\d\d\d\d)");

      string parsedName = "";

      if (parsedNameMatch.Success)
      {
        if (parsedNameMatch.Success)
        {
          if (parsedNameMatch.Groups.Count > 1)
          {
            parsedName = parsedNameMatch.Groups[1].Value;

            if (parsedNameMatch.Groups.Count >= 3 && int.TryParse(parsedNameMatch.Groups[2].Value, out var pYear))
            {
              if (pYear > 1887 && pYear < DateTime.Now.Year)
              {
                year = pYear;
              }
              else
              {
                year = null;
              }
            }
          }
        }
      }
      else
      {
        parsedName = name;
      }

      parsedName = parsedName.Replace(".", " ").Replace("-", null);

      var episodeKeys = DataLoader.DataLoader.GetTvShowSeriesNumber(name);

      if (!onlySingleItem)
      {
        if (tvShowName == null && episodeKeys != null)
        {
          tvShowName = Path.ChangeExtension(episodeKeys.ParsedName, null);
        }

        if (episodeKeys != null && seasonNumber == null)
        {
          seasonNumber = episodeKeys.SeasonNumber;
        }

        if (episodeKeys != null && episodeNumber == null && !downloadOneSeason)
        {
          episodeNumber = episodeKeys.EpisodeNumber;
        }

        CSFDTVShow tvShow = null;

        if (tvShowUrl == null)
        {

          if (string.IsNullOrEmpty(tvShowName))
          {
            if (!string.IsNullOrEmpty(parsedName))
              tvShowName = parsedName;
            else tvShowName = name;

          }

          var tvShowFind = await FindSingleCsfdItem(tvShowName, year, episodeKeys != null ||
                                                                      seasonNumber != null ||
                                                                      episodeNumber != null ||
                                                                      !string.IsNullOrEmpty(tvShowName), cancellationToken, parentMessage: statusMessage);

          if (tvShowFind == null)
          {
            statusMessage.Message = "TV SHOW NOT FOUND, TRYING MOVIE";
            statusMessage.ProcessedCount = statusMessage.NumberOfProcesses;
            statusMessage.Status = StatusType.Done;

            statusManager.UpdateMessage(statusMessage);

            var item = await FindSingleCsfdItem(parsedName, year, false, cancellationToken, true, statusMessage, isMovie: true);

            if (item == null)
            {
              statusMessage.Message = "NOT FOUND";
              statusMessage.ProcessedCount = statusMessage.NumberOfProcesses;
              statusMessage.Status = StatusType.Failed;

              statusManager.UpdateMessage(statusMessage);
            }

            return item;
          }

          tvShowUrl = tvShowFind.Url;

          if (tvShowFind.Parameters.Contains("epizoda"))
          {
            var episode = new CSFDTVShowSeasonEpisode()
            {
              Url = tvShowFind.Url
            };

            await Task.Run(() => LoadCsfdEpisode(episode));

            if (!string.IsNullOrEmpty(episode.TvShowUrl))
              tvShow = LoadTvShow(episode.TvShowUrl, cancellationToken, seasonNumber, episodeNumber, parentMessage: statusMessage);
          }
          else if (episodeKeys != null)
          {
            return LoadTvShow(tvShowFind.Url, cancellationToken, seasonNumber, episodeNumber, parentMessage: statusMessage);
          }
        }

        if (tvShow == null)
        {
          tvShow = LoadTvShow(tvShowUrl, cancellationToken, seasonNumber, episodeNumber, parentMessage: statusMessage, parsedName);
        }


        if (tvShow != null)
        {
          if (tvShow.Seasons != null && episodeKeys?.SeasonNumber != null)
          {
            if (tvShow.Seasons.Count >= episodeKeys.SeasonNumber)
            {
              var season = tvShow.Seasons[episodeKeys.SeasonNumber.Value - 1];

              if (season.SeasonEpisodes != null && !downloadOneSeason)
              {
                if (season.SeasonEpisodes.Count == 1)
                {
                  return season.SeasonEpisodes[0];
                }
                else if (episodeKeys?.EpisodeNumber != null && season.SeasonEpisodes.Count >= episodeKeys.EpisodeNumber)
                {
                  return season.SeasonEpisodes[episodeKeys.EpisodeNumber.Value - 1];
                }
              }

              return tvShow;
            }
          }

          return tvShow;
        }
        else
        {
          statusMessage.Message = "NOT FOUND!";
          statusMessage.Status = StatusType.Failed;

          statusManager.UpdateMessage(statusMessage);

          return null;
        }
      }
      else
      {
        if (bestItem != null && lastParsedName == parsedName)
        {
          statusManager.UpdateMessageAndIncreaseProcessCount(statusMessage);

          return bestItem;
        }


        return await FindSingleCsfdItem(parsedName, year, episodeKeys != null, cancellationToken, true, parentMessage: statusMessage);
      }
    }

    #endregion

    #region FindSingleCsfdItem

    private async Task<CSFDItem> FindSingleCsfdItem(
      string parsedName,
      int? year,
      bool isTvSHow,
      CancellationToken cancellationToken,
      bool showStatusMassage = false,
      StatusMessageViewModel parentMessage = null,
      bool parseYearFromName = true,
      bool isMovie = false)
    {
      parsedName = parsedName.RemoveDiacritics().Replace(".", " ").Replace("avi", "").Replace("  ", " ");

      lastParsedName = parsedName;

      if (parseYearFromName)
      {
        var match = Regex.Match(parsedName, @"\D*");

        if (match.Success && !string.IsNullOrEmpty(match.Value) && !Regex.IsMatch(parsedName, @"\d+th"))
        {
          parsedName = match.Value;
        }
      }

      var statusMessage = new StatusMessageViewModel(2)
      {
        Status = StatusType.Processing,
        Message = $"Finding item"
      };

      statusMessage.CopyParentState(parentMessage);

      if (showStatusMassage)
      {
        statusManager.UpdateMessage(statusMessage);
      }

      var items = await FindItems(parsedName, cancellationToken);

      var allItems = items?.AllItems?.ToList();

      if (allItems == null || allItems.Count == 0)
      {
        statusMessage.Status = StatusType.Failed;
        statusMessage.Message = "Not found!";

        statusManager.UpdateMessage(statusMessage);

        if (lastParsedName != parsedName)
        {
          return await FindSingleCsfdItem(parsedName, year, isTvSHow, cancellationToken, showStatusMassage, parentMessage, false);
        }
        else
          return null;
      }

      if (showStatusMassage)
        statusManager.UpdateMessageAndIncreaseProcessCount(statusMessage);


      double minSimilarity = 0.55;


      var query = allItems.Where(x => x.OriginalName != null)
        .Where(x => x.OriginalName.RemoveDiacritics().Similarity(parsedName) > minSimilarity)
        .OrderByDescending(x => x.OriginalName.RemoveDiacritics().Similarity(parsedName)).AsEnumerable();


      query = query.Concat(allItems.Where(x => x.Name != null)
        .Where(x => x.Name.RemoveDiacritics().Similarity(parsedName) > minSimilarity)
        .OrderByDescending(x => x.Name.RemoveDiacritics().Similarity(parsedName)).AsEnumerable());

      if (year != null)
      {
        var list = query.ToList();

        var yearQuery = list.Where(x => x.Year == year).ToList();

        if (yearQuery.Count() != 0)
        {
          query = yearQuery;
        }
      }

      query = query.OrderByDescending(x => x.RatingColor).ThenByDescending(x => x.Year);

      if (isTvSHow)
      {
        query = query.Where(x => x.Parameters.Contains("seriál") || x.Parameters.Contains("epizóda") || x.Parameters.Contains("séria"));
        query = query.OrderBy(x => GetOrderNumber(x.Parameters[0]));
      }
      else if (isMovie)
      {
        query = query.Where(x => !x.Parameters.Contains("seriál") && !x.Parameters.Contains("epizóda"));
      }


      var sortedItems = query.ToList();

      bestItem = sortedItems.FirstOrDefault();

      if (bestItem != null)
      {
        bestItem.Rating = GetCsfdRating(bestItem);
        bestItem.ImagePath = GetCsfdImage(bestItem);

        if (showStatusMassage)
          statusManager.UpdateMessageAndIncreaseProcessCount(statusMessage);

        return bestItem;
      }
      else if (lastParsedName != parsedName)
      {
        return await FindSingleCsfdItem(lastParsedName, year, isTvSHow, cancellationToken, showStatusMassage, parentMessage, false);
      }
      else
        return null;
    }

    #endregion

    private int GetOrderNumber(string value)
    {
      switch (value)
      {
        case "seriál":
          return 0;
        case "séria":
          return 1;
        case "epizóda":
          return 2;
      }

      return int.MaxValue;
    }

    #region GetItems

    private async Task<CSFDQueryResult> GetItems(string url, CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();
      CSFDQueryResult result = new CSFDQueryResult();

      var urlParsed = url.Replace("[", "").Replace("]", "").Replace("(", "").Replace(")", "");

      var html = chromeDriverProvider.SafeNavigate(urlParsed);

      if (html == null)
        return result;

      var document = new HtmlDocument();

      cancellationToken.ThrowIfCancellationRequested();
      document.LoadHtml(html);

      cancellationToken.ThrowIfCancellationRequested();
      var movies = await GetMoviesFromFind(document, cancellationToken);

      cancellationToken.ThrowIfCancellationRequested();
      var tvShows = await GetTvShowsFromFind(document, cancellationToken);

      result.Movies = movies;
      result.TvShows = tvShows;

      return result;
    }

    #endregion

    #region GetMoviesFromFind

    private Task<IEnumerable<CSFDItem>> GetMoviesFromFind(HtmlDocument document, CancellationToken cancellationToken)
    {
      return Task.Run(() =>
      {
        var nodes = TrySelectNodes(document.DocumentNode, "/div/div[2]/div/div[1]/div/div[1]/section[1]/div/div[1]/article");

        return ParseFindNodes(nodes, cancellationToken);
      });
    }

    #endregion

    #region GetCsfdRating

    private int? GetCsfdRating(CSFDItem cSFDItem)
    {
      var html = chromeDriverProvider.SafeNavigate(cSFDItem.Url);

      var document = new HtmlDocument();

      document.LoadHtml(html);

      var ratingNode = TrySelectNodes(document.DocumentNode, "/div/div[1]/aside/div[1]/div[2]/div")?.FirstOrDefault()?.InnerText.Replace("\t", null).Replace("\r", null).Replace("\n", null).Replace("%", null);

      if (int.TryParse(ratingNode, out var rating))
      {
        return rating;
      }

      return null;
    }

    #endregion

    #region GetCsfdImage

    private string GetCsfdImage(CSFDItem cSFDItem)
    {
      var html = chromeDriverProvider.SafeNavigate(cSFDItem.Url);

      var document = new HtmlDocument();

      if (!string.IsNullOrEmpty(html))
      {
        document.LoadHtml(html);

        var node = TrySelectNodes(document.DocumentNode, "/div/div[1]/div/div[1]/div[2]/div/div[1]/div[1]/a/img")?.FirstOrDefault();

        if (node != null)
        {
          if (node.Attributes.Count >= 0)
          {
            var urlValue = node.Attributes.SingleOrDefault(x => x.Name == "src")?.Value;

            if (urlValue != null && !urlValue.Contains("data:image"))
            {
              return urlValue.Replace("//image.pmgstatic.com", "https://image.pmgstatic.com");
            }
          }
        }
      }

      return null;

    }

    #endregion

    #region GetTvShowsFromFind

    private Task<IEnumerable<CSFDItem>> GetTvShowsFromFind(HtmlDocument document, CancellationToken cancellationToken)
    {
      return Task.Run(() =>
      {
        var nodes = TrySelectNodes(document.DocumentNode, "/div/div[2]/div/div[1]/div/div[1]/section[2]/div/div[1]/article");

        return ParseFindNodes(nodes, cancellationToken);
      });
    }

    #endregion

    #region ParseFindNodes

    private IEnumerable<CSFDItem> ParseFindNodes(HtmlNodeCollection nodes, CancellationToken cancellationToken)
    {
      if (nodes == null)
      {
        return null;
      }

      var list = new List<CSFDItem>();

      foreach (var node in nodes)
      {
        cancellationToken.ThrowIfCancellationRequested();

        var posterNode = node.ChildNodes[1].ChildNodes[1];

        var name = posterNode.Attributes[1].Value;


        string posterUrl = null;

        var urlValue = posterNode.ChildNodes[1].Attributes[0].Value;

        if (!urlValue.Contains("data:image"))
        {
          posterUrl = urlValue.Replace("//image.pmgstatic.com", "https://image.pmgstatic.com");
        }

        var url = baseUrl + posterNode.Attributes[0].Value;

        var infoNode = node.ChildNodes[3];

        var tile = infoNode.ChildNodes[1].ChildNodes[1].InnerText;
        string originalName = name;

        if (infoNode.ChildNodes[1].ChildNodes.Count > 3)
        {
          originalName = infoNode.ChildNodes[1].ChildNodes[3].InnerText.Replace("(", null).Replace(")", null);
        }


        var regex = new Regex(@"\((.*?)\)");
        var ads = regex.Matches(tile);

        List<string> parameters = new List<string>();
        int year = 0;

        for (int i = 0; i < ads.Count; i++)
        {
          var value = ads[i].Groups[1].Captures[0].Value;
          if (i == 0)
          {
            int.TryParse(value, out year);
          }
          else
            parameters.Add(value);
        }

        var generes = infoNode.ChildNodes[3].InnerText.Replace("\t", null).Replace("\n", null).Replace("\r", null).Split("/");

        string[] actors = null;
        string[] directors = null;

        if (infoNode.ChildNodes.Count >= 6)
        {
          var textDirectors = infoNode.ChildNodes[5].InnerText;

          if (textDirectors.Contains("Réžia:"))
          {
            directors = textDirectors.Replace("\t", null).Replace("\n", null).Replace("\r", null).Split("Réžia:")[1].Split(",");
          }
        }

        if (infoNode.ChildNodes.Count >= 8)
        {
          var textActors = infoNode.ChildNodes[7].InnerText;

          if (textActors.Contains("Hrajú:"))
          {
            actors = textActors.Replace("\t", null).Replace("\n", null).Replace("\r", null).Split("Hrajú:")[1].Split(",");
          }
        }

        var color = infoNode.ChildNodes[1].ChildNodes[1].ChildNodes[0].Attributes[0].Value.Replace("icon icon-rounded-square ", null);

        RatingColor? ratingColor = null;

        switch (color)
        {
          case "lightgrey":
            {
              ratingColor = RatingColor.LightGray;
              break;
            }

          case "grey":
            {
              ratingColor = RatingColor.Gray;
              break;
            }

          case "blue":
            {
              ratingColor = RatingColor.Blue;
              break;
            }

          case "red":
            {
              ratingColor = RatingColor.Red;
              break;
            }
        }

        var item = new CSFDItem()
        {
          ImagePath = posterUrl,
          Name = name,
          Year = year,
          Url = url,
          Actors = actors,
          Directors = directors,
          Generes = generes,
          Parameters = parameters.ToArray(),
          OriginalName = originalName,
          RatingColor = ratingColor
        };

        list.Add(item);
      }

      return list.AsEnumerable();
    }

    #endregion

    #region TrySelectNodes

    private HtmlNodeCollection TrySelectNodes(HtmlNode htmlNode, string xPath, string baseXPath = "/html/body/div[2]/div")
    {
      int[] divIndexes = new int[] { 2, 3, 4, 2 };

      foreach (var divIndex in divIndexes)
      {
        var finalXPath = $"{baseXPath}[{divIndex}]{xPath}";

        var resultHtmlNode = htmlNode.SelectNodes(finalXPath);

        if (resultHtmlNode != null)
        {
          return resultHtmlNode;
        }
      }

      return null;
    }

    #endregion

  }
}
