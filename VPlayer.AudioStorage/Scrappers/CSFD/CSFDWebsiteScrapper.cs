using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
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
using VCore.Standard.Helpers;
using VCore.WPF;
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
    private int extraMilisecondsForScrape = 0;

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
          Url = GetCorrectBaseUrl(url)
        };

        statusManager.UpdateMessage(statusMessage);

        var document = GetItemMainPage(url, cancellationToken);

        var name = GetItemName(document, out var posterUrl);

        if (name != null)
        {
          tvShow.Name = name;
          GetCsfdInfo(bestItem, document);

          statusManager.UpdateMessageAndIncreaseProcessCount(statusMessage);

          tvShow.ImagePath = GetTvShowPoster(document, cancellationToken);

          statusManager.UpdateMessageAndIncreaseProcessCount(statusMessage);

          tvShow.Seasons = LoadSeasons(document, tvShow.Url, statusMessage, seasonNumber, episodeNumber, cancellationToken, fileName: fileName);

          if (tvShow.Seasons == null)
          {
            statusMessage.Status = StatusType.Failed;
            statusManager.UpdateMessage(statusMessage);

            return tvShow;
          }

          foreach (var item in tvShow.Seasons.Where(x => x.SeasonEpisodes != null).SelectMany(x => x.SeasonEpisodes))
          {
            item.ImagePath = tvShow.ImagePath;
          }



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

    #region GetTvShowPoster

    public string GetTvShowPoster(string url, CancellationToken cancellationToken)
    {
      var document = GetItemMainPage(url, cancellationToken);

      return GetTvShowPoster(document, cancellationToken);
    }

    public string GetTvShowPoster(HtmlDocument document, CancellationToken cancellationToken)
    {
      var name = GetItemName(document, out var posterUrl);

      var poster = DownloadPoster(posterUrl, cancellationToken);

      if (poster != null)
      {
        return SaveImage(name, poster, cancellationToken);
      }

      return null;
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

      newSeason.SeasonEpisodes = LoadSeasonEpisodes(newSeason.SeasonNumber, newSeason.Url, statusMessage, cancellationToken);

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
      string url,
      StatusMessageViewModel pStatusMessageViewModel,
      int? seasonNumber,
      int? episodeNumber,
      CancellationToken cancellationToken,
      int? pageNumber = null,
      string fileName = null,
      int? episodesPerPage = null)
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

      if (pageNumber > 0 && episodeNumber > 0)
      {
        statusMessageViewModel.NumberOfProcesses = pStatusMessageViewModel.NumberOfProcesses;
        statusMessageViewModel.ProcessedCount = pStatusMessageViewModel.ProcessedCount;
        statusMessageViewModel.Message = pStatusMessageViewModel.Message;
      }

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

      if (isSingleSeasoned)
      {
        var episodes = new List<CSFDTVShowSeasonEpisode>();

        var newSingleSeason = new CSFDTVShowSeason()
        {
          SeasonEpisodes = episodes,
          SeasonNumber = 1
        };

        if (episodesPerPage == null)
        {
          episodesPerPage = seasons.Count;
          statusMessageViewModel.Message = $"Downlading episode {episodeNumber}";
        }

        var episodeIndex = 0;

        if (seasonNumber == null && episodeNumber == null && !string.IsNullOrEmpty(fileName))
        {
          if (int.TryParse(Regex.Match(fileName, @"\d+").Value, out var parsedNumber))
          {
            episodeNumber = parsedNumber;
          }
        }

        if (episodeNumber > episodesPerPage.Value * (pageNumber ?? 1))
        {
          pageNumber = (int)Math.Ceiling((double)episodeNumber / episodesPerPage.Value);

          if (url.Contains("seriePage"))
          {
            url = url.Split("?seriePage")[0] + $"?seriePage={pageNumber}";
          }
          else
          {
            url += $"?seriePage={pageNumber}";
          }


          var newDocument = GetItemMainPage(url, cancellationToken);

          statusMessageViewModel.ProcessedCount = 0;
          statusMessageViewModel.NumberOfProcesses = 1;

          return LoadSeasons(newDocument, url, statusMessageViewModel, seasonNumber, episodeNumber, cancellationToken, pageNumber, episodesPerPage: episodesPerPage);
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
          var index = episodeNumber.Value - (((pageNumber ?? 1) - 1) * episodesPerPage.Value) - 1;

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

          season.SeasonEpisodes = LoadSeasonEpisodes(season.SeasonNumber, season.Url, statusMessageViewModel, cancellationToken);

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
          season.SeasonEpisodes = LoadSeasonEpisodes(seasonNumber.Value, season.Url, statusMessageViewModel, cancellationToken, episodeNumber);
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

    private List<CSFDTVShowSeasonEpisode> LoadSeasonEpisodes(int seasonNumber, string url, StatusMessageViewModel pStatusMessageViewModel, CancellationToken cancellationToken, int? episodeNumber = null)
    {
      try
      {
        List<CSFDTVShowSeasonEpisode> episodes = new List<CSFDTVShowSeasonEpisode>();
        HtmlDocument document = new HtmlDocument();

        cancellationToken.ThrowIfCancellationRequested();

        var html = chromeDriverProvider.SafeNavigate(url, out var redirectedUrl, extraMiliseconds: extraMilisecondsForScrape);

        document.LoadHtml(html);

        var nodes = TrySelectNodes(document.DocumentNode, "/div/div[1]/div[3]/div/ul/li/a");

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

            cancellationToken.ThrowIfCancellationRequested();
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

          cancellationToken.ThrowIfCancellationRequested();
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

    #region GetRatingColor

    private RatingColor GetRatingColor(int? rating)
    {
      if (rating != null)
      {
        if (rating >= 70)
        {
          return RatingColor.Red;
        }
        else if (rating >= 30)
        {
          return RatingColor.Blue;
        }
        else if (rating > 0)
        {
          return RatingColor.Gray;
        }
        else
        {
          return RatingColor.LightGray;
        }
      }
      else
      {
        return RatingColor.LightGray;
      }
    }

    #endregion

    #region LoadCsfdEpisode

    private void LoadCsfdEpisode(CSFDTVShowSeasonEpisode cSFDTVShowSeasonEpisode, string pHtml = null)
    {
      try
      {
        string html = pHtml;

        if (pHtml == null)
        {
          html = chromeDriverProvider.SafeNavigate(cSFDTVShowSeasonEpisode.Url, out var redirectedUrl, extraMiliseconds: extraMilisecondsForScrape);
        }

        var document = new HtmlDocument();

        document.LoadHtml(html);

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

        GetCsfdInfo(cSFDTVShowSeasonEpisode, document);

        cSFDTVShowSeasonEpisode.TvShowUrl = tvShowUrl;
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

    private void GetCsfdInfo(CSFDItem item, HtmlDocument document)
    {
      string country = null;
      string originalName = null;

      var names = TrySelectNodes(document.DocumentNode, "/div/div[1]/div/div[1]/div[2]/div/header/div/ul/li");

      originalName = names
        .SingleOrDefault(x => x.ChildNodes.Where(y => y.Name == "img")
        .SingleOrDefault()?.Attributes
        .SingleOrDefault(xy => xy.Name == "alt" && xy.Value == "USA") != null)?.InnerText;

      if (string.IsNullOrEmpty(originalName))
      {
        originalName = names
          .SingleOrDefault(x => x.ChildNodes.Where(y => y.Name == "img")
            .SingleOrDefault()?.Attributes
            .SingleOrDefault(xy => xy.Name == "alt" && xy.Value == "Veľká Británia") != null)?.InnerText;
      }
      else if (string.IsNullOrEmpty(originalName))
      {
        originalName = names.FirstOrDefault()?.InnerText;
      }

      originalName = GetClearText(originalName);

      var infoNode = TrySelectNodes(document.DocumentNode, "/div/div[1]/div/div[1]/div[2]/div/div[2]")?.FirstOrDefault();

      if (infoNode == null)
      {
        infoNode = TrySelectNodes(document.DocumentNode, "/div/div[1]/div/div[1]/div[2]/div/div")?.FirstOrDefault();
      }

      string[] actors = null;
      string[] directors = null;
      string[] generes = null;
      int? year = null;

      string length = null;

      if (infoNode != null && infoNode.ChildNodes.Count > 5)
      {
        generes = infoNode.ChildNodes[3].InnerText.Replace("\t", null).Replace("\n", null).Replace("\r", null).Split("/").Select(x => x.Trim()).ToArray();

        if (infoNode.ChildNodes[5].ChildNodes.Count > 2)
        {
          var basicInfoNode = infoNode.ChildNodes[5];

          var stringYear = basicInfoNode.ChildNodes[0].InnerText.Replace(",", null).Trim();
          var stringYearSecond = basicInfoNode.ChildNodes[1].InnerText.Replace(",", null).Trim();

          length = basicInfoNode.ChildNodes[2].InnerText.Replace(",", null).Trim();

          if (int.TryParse(stringYear, out var parsedYear))
          {
            year = parsedYear;
          }
          else if (int.TryParse(stringYearSecond, out var parsedYear1))
          {
            year = parsedYear1;
            country = stringYear;
          }
        }

        var creatorsNode = infoNode.ChildNodes[7];

        if (creatorsNode.ChildNodes.Count > 3)
        {
          var textDirectors = creatorsNode.ChildNodes.FirstOrDefault(x => x.InnerText.Contains("Réžia:"))?.InnerText;

          if (!string.IsNullOrEmpty(textDirectors))
          {
            directors = textDirectors.Replace("\t", null).Replace("\n", null).Replace("\r", null).Split("Réžia:")[1].Split(",").Select(x => x.Trim()).ToArray();
          }


          var textActors = creatorsNode.ChildNodes.FirstOrDefault(x => x.InnerText.Contains("Hrajú:"))?.InnerText;

          if (!string.IsNullOrEmpty(textActors))
          {
            actors = textActors.Replace("\t", null).Replace("\n", null).Replace("\r", null).Split("Hrajú:")[1].Split(",").Select(x => x.Trim()).ToArray();
          }

        }
      }

      var description = GetClearText(TrySelectNodes(document.DocumentNode, "/div/div[1]/div/section/div[2]/div/div[1]/p")?.FirstOrDefault()?.InnerText)?.Replace(" ",null);
      var premiere = TrySelectNodes(document.DocumentNode, "/div/div[1]/aside/section[1]/div[2]/ul/li/span[2]")?.FirstOrDefault()?.Attributes[0].Value;

      item.Rating = GetCsfdRating(item, document);
      item.RatingColor = GetRatingColor(item.Rating);
      item.Year = year;
      item.Actors = actors;
      item.Directors = directors;
      item.Length = length;
      item.Description = description;
      item.Premiere = premiere;
      item.Generes = generes;
      item.Country = country;
      item.OriginalName = originalName?.Replace("(viac" + "" + "<)", null)?.Replace("(viac)", null);
    }

    #region GetCorrectBaseUrl

    private string GetCorrectBaseUrl(string url)
    {
      return url
        .Replace("https://csfd.cz", baseUrl)
        .Replace("https://www.csfd.cz", baseUrl)
        .Replace("https://new.csfd.cz", baseUrl)
        .Replace("https://new.csfd.sk", baseUrl)
        .Replace("https://csfd.sk", baseUrl)
        .Replace("https://www.csfd.sk", baseUrl);
    }

    #endregion

    #region GetItemMainPage

    public HtmlDocument GetItemMainPage(string url, CancellationToken cancellationToken)
    {
      var html = chromeDriverProvider.SafeNavigate(GetCorrectBaseUrl(url), out var redirectedUrl, extraMiliseconds: extraMilisecondsForScrape);

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

          VSynchronizationContext.PostOnUIThread(() =>
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
            parsedName = parsedNameMatch.Groups[1].Value + " " + Regex.Replace(name, @"(.*?)(\d\d\d\d)", "");

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
      var yearNumbers = DataLoader.DataLoader.GetYear(name);

      if (!onlySingleItem)
      {
        if (tvShowName == null && episodeKeys != null)
        {
          tvShowName = Path.ChangeExtension(episodeKeys.ParsedName, null);
        }

        if (episodeKeys != null)
        {
          if (seasonNumber == null)
          {
            seasonNumber = episodeKeys.SeasonNumber;
          }

          if (episodeNumber == null && downloadOneSeason)
          {
            episodeNumber = episodeKeys.EpisodeNumber;
          }
          else if (episodeNumber == null && episodeKeys.EpisodeNumber != null)
          {
            episodeNumber = episodeKeys.EpisodeNumber;
          }
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

          CSFDItem tvShowFind = null;

          if (episodeKeys == null && yearNumbers != null)
          {
            tvShowName = yearNumbers.ParsedName;
            parsedName = tvShowName;
            year = yearNumbers.YearNumber;
          }



          tvShowFind = await FindSingleCsfdItem(tvShowName, year, episodeKeys != null ||
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
              if (parsedNameMatch.Groups.Count > 1)
                item = await FindSingleCsfdItem(parsedNameMatch.Groups[1].Value, year, false, cancellationToken, true, statusMessage, isMovie: true);

              if (item == null)
              {
                statusMessage.Message = "NOT FOUND";
                statusMessage.ProcessedCount = statusMessage.NumberOfProcesses;
                statusMessage.Status = StatusType.Failed;

                statusManager.UpdateMessage(statusMessage);
              }
            }

            return item;
          }

          if (tvShowFind.Parameters.Contains("epizóda"))
          {
            var episode = new CSFDTVShowSeasonEpisode()
            {
              Url = tvShowFind.Url
            };

            LoadCsfdEpisode(episode);

            if (string.IsNullOrEmpty(episode.ImagePath))
            {
              episode.ImagePath = GetTvShowPoster(episode.TvShowUrl, cancellationToken);
            }

            return episode;
          }
          else if (episodeKeys != null)
          {
            return LoadTvShow(tvShowFind.Url, cancellationToken, seasonNumber, episodeNumber, statusMessage);
          }
          else
          {
            return tvShowFind;
          }
        }

        if (seasonNumber != null && episodeNumber != null)
        {
          tvShow = LoadTvShow(tvShowUrl, cancellationToken, seasonNumber, episodeNumber, statusMessage, parsedName);
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

    private SemaphoreSlim findSemaphore = new SemaphoreSlim(1, 1);
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

      var originalParsedName = parsedName;
      parsedName = parsedName.RemoveDiacritics()
        .Replace("avi", "")
        .Replace("mkv", "")
        .ToLower();

      Regex rgx = new Regex("[^a-zA-Z0-9]");

      parsedName = rgx.Replace(parsedName, " ").Replace("  ", " ").Replace("   ", " ");

      lastParsedName = parsedName.Trim();

      if (parseYearFromName)
      {
        var matches = Regex.Matches(parsedName, @"\D*");

        var anyMatch = matches.Any(match => match.Success && !string.IsNullOrEmpty(match.Value) && !Regex.IsMatch(parsedName, @"\d+th"));

        if (anyMatch)
        {
          parsedName = "";
        }

        foreach (var match in matches.ToList())
        {
          if (match.Success && !string.IsNullOrEmpty(match.Value) && !Regex.IsMatch(parsedName, @"\d+th"))
          {
            parsedName += " " + match.Value;
          }
        }
      }

      parsedName = parsedName.Replace("  ", " ").Replace("   ", " ").Trim();

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
        else if (lastParsedName.Contains("movie"))
        {
          lastParsedName = lastParsedName.Replace("movie", "");

          return await FindSingleCsfdItem(lastParsedName, year, false, cancellationToken, showStatusMassage, parentMessage, false);
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
        IEnumerable<CSFDItem> yearQuery = null;
        List<CSFDItem> list = null;

        if (!query.Any())
        {
          var minSimilarity2 = 0.25;

          yearQuery = allItems.Where(x => x.OriginalName != null)
            .Where(x => x.OriginalName.RemoveDiacritics().Similarity(parsedName) > minSimilarity2)
            .OrderByDescending(x => x.OriginalName.RemoveDiacritics().Similarity(parsedName)).AsEnumerable();

          yearQuery = yearQuery.Concat(allItems.Where(x => x.Name != null)
            .Where(x => x.Name.RemoveDiacritics().Similarity(parsedName) > minSimilarity2)
            .OrderByDescending(x => x.Name.RemoveDiacritics().Similarity(parsedName)).AsEnumerable());

          yearQuery = yearQuery.Concat(allItems.Where(x => x.OriginalName != null)
            .Where(x => x.OriginalName.RemoveDiacritics().Similarity(GetWithoutBrackets(parsedName)) > minSimilarity2)
            .OrderByDescending(x => x.Name.RemoveDiacritics().Similarity(GetWithoutBrackets(parsedName))).AsEnumerable());

          list = yearQuery.ToList();
        }
        else
        {
          list = query.ToList();
        }

        var yearQueryList = list.Where(x => x.Year == year).ToList();

        if (yearQueryList.Count() != 0)
        {
          query = yearQueryList;
        }
      }

      query = query
        .OrderByDescending(x => x.OriginalName.RemoveDiacritics().Similarity(GetWithoutBrackets(parsedName)))
        .ThenByDescending(x => x.RatingColor)
        .ThenByDescending(x => x.Year);

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

      var newBestItem = sortedItems.FirstOrDefault();
      var originalNameWithoutBrackets = GetWithoutBrackets(originalParsedName);

      if (newBestItem != null)
      {
        bestItem = newBestItem;

        var html = chromeDriverProvider.SafeNavigate(bestItem.Url, out var redirectedUrl, extraMiliseconds: extraMilisecondsForScrape);

        var document = new HtmlDocument();

        document.LoadHtml(html);

        GetCsfdInfo(bestItem, document);

        if (showStatusMassage)
          statusManager.UpdateMessageAndIncreaseProcessCount(statusMessage);

        return bestItem;
      }
      else if (originalParsedName != originalNameWithoutBrackets)
      {
        return await FindSingleCsfdItem(originalNameWithoutBrackets, year, isTvSHow, cancellationToken, showStatusMassage, parentMessage, false);
      }
      else if (lastParsedName != parsedName)
      {
        return await FindSingleCsfdItem(lastParsedName, year, isTvSHow, cancellationToken, showStatusMassage, parentMessage, false);
      }
      else if (lastParsedName.Contains("movie"))
      {
        lastParsedName = lastParsedName.Replace("movie", "");

        return await FindSingleCsfdItem(lastParsedName, year, false, cancellationToken, showStatusMassage, parentMessage, false);
      }

      return null;
    }

    #endregion

    #region GetOrderNumber

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

    #endregion

    #region RemoveDiacritics

    public static string GetWithoutBrackets(string text)
    {
      if (string.IsNullOrEmpty(text))
      {
        return text;
      }

      var regex = new Regex(@"\[.*\]");
      return regex.Replace(text, "");
    }

    #endregion

    #region GetItems

    private async Task<CSFDQueryResult> GetItems(string url, CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();
      CSFDQueryResult result = new CSFDQueryResult();

      var urlParsed = url.Replace("[", "").Replace("]", "").Replace("(", "").Replace(")", "");

      var html = chromeDriverProvider.SafeNavigate(urlParsed, out var redirectedUrl, extraMiliseconds: extraMilisecondsForScrape);

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
        var nodes = TrySelectNodes(document.DocumentNode, "/div/div[2]/div/div[1]/section[1]/div/div[1]/article");

        return ParseFindNodes(nodes, cancellationToken);
      });
    }

    #endregion

    #region GetTvShowsFromFind

    private Task<IEnumerable<CSFDItem>> GetTvShowsFromFind(HtmlDocument document, CancellationToken cancellationToken)
    {
      return Task.Run(() =>
      {
        var nodes = TrySelectNodes(document.DocumentNode, "/div/div[2]/div/div[1]/section[2]/div/div[1]/article");

        return ParseFindNodes(nodes, cancellationToken);
      });
    }

    #endregion

    #region GetCsfdRating

    private int? GetCsfdRating(CSFDItem cSFDItem, HtmlDocument document = null)
    {
      if (document == null)
      {
        var html = chromeDriverProvider.SafeNavigate(cSFDItem.Url, out var redirectedUrl, extraMiliseconds: extraMilisecondsForScrape);

        document = new HtmlDocument();

        document.LoadHtml(html);
      }

      var ratingNode = TrySelectNodes(document.DocumentNode, "/div/div[1]/aside/div/div[1]/div[1]")?.FirstOrDefault()?.InnerText.Replace("\t", null).Replace("\r", null).Replace("\n", null).Replace("%", null);

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
      if (cSFDItem == null)
        return null;

      var html = chromeDriverProvider.SafeNavigate(cSFDItem.Url, out var redirectedUrl, extraMiliseconds: extraMilisecondsForScrape);

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

        var infoText = infoNode.ChildNodes[3].InnerText.Replace("\t", null).Replace("\n", null).Replace("\r", null);
        var infoSplit = infoText?.Split(",");

        string[] generes = null;
        string[] origin = null;

        if (infoSplit.Length > 1)
        {
          origin = infoSplit[0].Split("/").Select(x => x.Trim()).ToArray();
          generes = infoSplit[1].Split("/").Select(x => x.Trim()).ToArray();
        }

        string[] actors = null;
        string[] directors = null;

        if (infoNode.ChildNodes.Count >= 6)
        {
          var textDirectors = infoNode.ChildNodes[5].InnerText;

          if (textDirectors.Contains("Réžia:"))
          {
            directors = textDirectors.Replace("\t", null).Replace("\n", null).Replace("\r", null).Split("Réžia:")[1].Split(",").Select(x => x.Trim()).ToArray();
          }
        }

        if (infoNode.ChildNodes.Count >= 8)
        {
          var textActors = infoNode.ChildNodes[7].InnerText;

          if (textActors.Contains("Hrajú:"))
          {
            actors = textActors.Replace("\t", null).Replace("\n", null).Replace("\r", null).Split("Hrajú:")[1].Split(",").Select(x => x.Trim()).ToArray();
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
          Name = name?.Replace("&amp;", "&").Trim(),
          Year = year,
          Url = url,
          Actors = actors,
          Directors = directors,
          Generes = generes,
          Parameters = parameters.ToArray(),
          OriginalName = originalName?.Replace("&amp;", "&").Trim(),
          RatingColor = ratingColor,
          Origin = origin
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
