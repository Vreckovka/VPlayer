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
using HtmlAgilityPack;
using Logger;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using VCore;
using VPlayer.AudioStorage.InfoDownloader;
using VPlayer.AudioStorage.Scrappers.CSFD.Domain;
using VPlayer.Core.Managers.Status;

namespace VPlayer.AudioStorage.Scrappers.CSFD
{

  public class CSFDWebsiteScrapper : ICSFDWebsiteScrapper
  {
    private readonly ILogger logger;
    private readonly IStatusManager statusManager;
    private ChromeDriver chromeDriver;
    private string baseUrl = "https://csfd.cz";
    private bool wasInitilized;

    public CSFDWebsiteScrapper(ILogger logger, IStatusManager statusManager)
    {
      this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
      this.statusManager = statusManager ?? throw new ArgumentNullException(nameof(statusManager));
    }

    #region Initialize

    public bool Initialize()
    {
      try
      {
        if (!wasInitilized)
        {
          var chromeOptions = new ChromeOptions();

          chromeOptions.AddArguments(new List<string>() { "headless", "disable-infobars", "--log-level=3" });

          var dir = Directory.GetCurrentDirectory();
          var chromeDriverService = ChromeDriverService.CreateDefaultService(dir, "chromedriver.exe");

          chromeDriverService.HideCommandPromptWindow = true;

          chromeDriver = new ChromeDriver(chromeDriverService, chromeOptions);

          wasInitilized = true;
        }

        return wasInitilized;
      }
      catch (Exception ex)
      {
        logger.Log(ex);

        return false;
      }
    }

    #endregion

    #region LoadTvShow

    public CSFDTVShow LoadTvShow(string url, CancellationToken cancellationToken, int? seasonNumber = null, int? episodeNumber = null)
    {
      if (!Initialize())
      {
        return null;
      }

      var tvShow = new CSFDTVShow()
      {
        Url = url
      };

      var statusMessage = new StatusMessage(3)
      {
        MessageStatusState = MessageStatusState.Processing,
        Message = $"Downloading {url.Replace("https://www.csfd.cz/", null).Replace("/prehled", null)}"
      };

      statusManager.UpdateMessage(statusMessage);

      var name = GetTvShowName(url, out var posterUrl, cancellationToken);

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

        tvShow.Seasons = LoadSeasons(statusMessage, seasonNumber, episodeNumber, cancellationToken);

        if (tvShow.Seasons == null)
        {
          return tvShow;
        }

        foreach (var item in tvShow.Seasons.Where(x => x.SeasonEpisodes != null).SelectMany(x => x.SeasonEpisodes))
        {
          item.ImagePath = tvShow.ImagePath;
        }


        statusManager.UpdateMessageAndIncreaseProcessCount(statusMessage);

        return tvShow;
      }

      statusMessage.MessageStatusState = MessageStatusState.Failed;

      return null;
    }

    #endregion

    #region LoadTvShowSeason

    public CSFDTVShowSeason LoadTvShowSeason(string url, CancellationToken cancellationToken)
    {
      Initialize();

      var statusMessage = new StatusMessage(2)
      {
        MessageStatusState = MessageStatusState.Processing,
        Message = "Downloading tv show season"
      };

      statusManager.UpdateMessage(statusMessage);

      var document = new HtmlDocument();

      document.LoadHtml(chromeDriver.PageSource);

      statusMessage = new StatusMessage(1)
      {
        MessageStatusState = MessageStatusState.Processing,
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

    #region GetTvShowName

    private string GetTvShowName(string url, out string posterUrl, CancellationToken cancellationToken)
    {
      url = url
        .Replace("https://new.csfd.sk", baseUrl)
        .Replace("https://csfd.sk", baseUrl)
        .Replace("https://www.csfd.sk", baseUrl);

      chromeDriver.Url = url;
      chromeDriver.Navigate();

      var document = new HtmlDocument();

      cancellationToken.ThrowIfCancellationRequested();
      document.LoadHtml(chromeDriver.PageSource);

      string node = null;
      int maxCount = 10;
      int i = 0;

      while (node == null && i < maxCount)
      {
        cancellationToken.ThrowIfCancellationRequested();

        node = document.DocumentNode.SelectNodes("/html/body/div[3]/div/div[1]/div/div[1]/div/div/header/div/h1")?.FirstOrDefault()?.InnerText;

        if (node != null)
        {
          var name = node.Replace("\t", string.Empty).Replace(" (TV seriál)", string.Empty).Replace("\r", null).Replace("\n", null);

          logger.Log(MessageType.Success, $"Tv show name: {name}");

          var posterNode = chromeDriver.FindElement(By.XPath("/html/body/div[3]/div/div[1]/div/div[1]/div/div/div[1]/div[1]/a/img"));

          posterUrl = posterNode.GetAttribute("src");

          return name;
        }

        i++;
      }

      posterUrl = null;
      return null;
    }

    #endregion

    #region DownloadPoster

    private byte[] DownloadPoster(string coverUrl, CancellationToken cancellationToken)
    {
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

      var directory = Path.Combine(AudioInfoDownloader.GetDefaultPicturesPath(), $"TvShows\\{tvShowName}");
      var finalPath = Path.Combine(directory, "poster.jpg");

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

      return finalPath;
    }

    #endregion

    #region LoadSeasons

    private List<CSFDTVShowSeason> LoadSeasons(StatusMessage statusMessage, int? seasonNumber, int? episodeNumber, CancellationToken cancellationToken)
    {
      var seasons = new List<CSFDTVShowSeason>();

      var document = new HtmlDocument();

      cancellationToken.ThrowIfCancellationRequested();
      document.LoadHtml(chromeDriver.PageSource);

      var nodes = document.DocumentNode.SelectNodes("/html/body/div[3]/div/div[1]/div/section[1]/div[2]/div/ul/li/h3/a");

      if (nodes == null)
      {
        return seasons;
      }

      statusMessage = new StatusMessage(nodes.Count)
      {
        MessageStatusState = MessageStatusState.Processing,
        Message = "Downloading tv show seasons",
        IsMinimized = statusMessage.IsMinimized,
        IsClosed = statusMessage.IsClosed,
      };

      statusManager.UpdateMessage(statusMessage);

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
      var isSingleSeasoned = !document.DocumentNode.SelectNodes("/html/body/div[3]/div/div[1]/div/section[1]/div[1]/h3").FirstOrDefault()?.InnerText.Contains("Série");

      if (isSingleSeasoned == true)
      {
        var episodes = new List<CSFDTVShowSeasonEpisode>();

        var newSingleSeason = new CSFDTVShowSeason()
        {
          SeasonEpisodes = episodes,
          SeasonNumber = 1
        };

        statusMessage.Message = "Single seasoned: Downlading episodes";
        statusManager.UpdateMessage(statusMessage);

        episodeNumber = 1;
        foreach (var season in seasons)
        {
          var newEpisode = new CSFDTVShowSeasonEpisode();

          newEpisode.Name = season.Name;
          newEpisode.Url = season.Url;

          cancellationToken.ThrowIfCancellationRequested();
          LoadCsfdEpisode(newEpisode);

          if (newEpisode.EpisodeNumber == null)
            newEpisode.EpisodeNumber = episodeNumber;

          episodes.Add(newEpisode);

          statusManager.UpdateMessageAndIncreaseProcessCount(statusMessage);
          episodeNumber++;
        }

        return new List<CSFDTVShowSeason>() { newSingleSeason };
      }


      if (seasonNumber == null)
      {
        foreach (var season in seasons)
        {
          cancellationToken.ThrowIfCancellationRequested();

          season.SeasonEpisodes = LoadSeasonEpisodes(season.SeasonNumber, season.Url, statusMessage);

          statusManager.UpdateMessageAndIncreaseProcessCount(statusMessage);
        }
      }
      else
      {
        statusMessage.ProcessedCount = 0;
        statusMessage.NumberOfProcesses = 1;

        var season = seasons[seasonNumber.Value - 1];

        if (season != null)
        {
          cancellationToken.ThrowIfCancellationRequested();
          season.SeasonEpisodes = LoadSeasonEpisodes(seasonNumber.Value, season.Url, statusMessage, episodeNumber);
        }
        else
        {
          return null;
        }

        cancellationToken.ThrowIfCancellationRequested();
        statusManager.UpdateMessageAndIncreaseProcessCount(statusMessage);
      }



      return seasons;
    }

    #endregion

    #region LoadSeasonEpisodes

    private List<CSFDTVShowSeasonEpisode> LoadSeasonEpisodes(int seasonNumber, string url, StatusMessage statusMessage, int? episodeNumber = null)
    {
      try
      {
        List<CSFDTVShowSeasonEpisode> episodes = new List<CSFDTVShowSeasonEpisode>();

        chromeDriver.Url = url;
        chromeDriver.Navigate();

        HtmlDocument document = new HtmlDocument();

        var html = chromeDriver.PageSource;

        document.LoadHtml(html);


        var nodes = document.DocumentNode.SelectNodes("/html/body/div[3]/div/div[1]/div[3]/div/ul/li/a");

        if (nodes == null)
        {
          nodes = document.DocumentNode.SelectNodes("/html/body/div[3]/div/div[1]/div/section[1]/div[2]/div/ul/li/h3/a");
        }

        if (nodes == null)
        {
          nodes = document.DocumentNode.SelectNodes("/html/body/div[3]/div/div[1]/div/section/div[2]/div/ul/li/h3/a");
        }

        if (nodes == null)
        {
          return episodes;
        }

        statusMessage = new StatusMessage(nodes.Count)
        {
          MessageStatusState = MessageStatusState.Processing,
          Message = $"Downloading season ({seasonNumber}) episodes",
          IsMinimized = statusMessage.IsMinimized,
          IsClosed = statusMessage.IsClosed
        };

        statusManager.UpdateMessage(statusMessage);



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
            statusManager.UpdateMessageAndIncreaseProcessCount(statusMessage);
          }
        }
        else
        {
          var node = nodes[episodeNumber.Value - 1];
          statusMessage.ProcessedCount = 0;
          statusMessage.NumberOfProcesses = 1;

          var newEpisode = new CSFDTVShowSeasonEpisode();

          newEpisode.Name = node.InnerText;
          newEpisode.Url = baseUrl + node.Attributes.FirstOrDefault(x => x.Name == "href")?.Value;

          LoadCsfdEpisode(newEpisode);
          episodes.Add(newEpisode);

          logger.Log(MessageType.Success, $"Tv show episode: {newEpisode.Name}");
          statusManager.UpdateMessageAndIncreaseProcessCount(statusMessage);
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
      chromeDriver.Url = cSFDTVShowSeasonEpisode.Url;
      chromeDriver.Navigate();

      var document = new HtmlDocument();

      document.LoadHtml(chromeDriver.PageSource);

      var ratingNode = GetClearText(document.DocumentNode.SelectNodes("/html/body/div[3]/div/div[1]/aside/div/div[2]/div")?.FirstOrDefault()?.InnerText);
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


      var originalName = GetClearText(document.DocumentNode.SelectNodes("/html/body/div[3]/div/div[1]/div/div[1]/div[2]/div/header/div/ul/li")?.FirstOrDefault()?.InnerText);

      var nameNode = document.DocumentNode.SelectNodes("/html/body/div[3]/div/div[1]/div/div[1]/div[2]/div/header/div")?.FirstOrDefault();

      List<string> parameters = new List<string>();

      KeyValuePair<int, int> number = new KeyValuePair<int, int>(-1, -1);

      if (nameNode != null)
      {
        var fullname = GetClearText(nameNode.ChildNodes[1].InnerText);

        number = DataLoader.DataLoader.GetTvShowSeriesNumber(fullname);

        var properties = GetClearText(nameNode.ChildNodes[3].InnerText);

        var regex1 = new Regex(@"\((.*?)\)");

        var ads = regex1.Matches(properties);

        for (int i = 0; i < ads.Count; i++)
        {
          parameters.Add(ads[i].Groups[1].Captures[0].Value);
        }
      }


      var infoNode = document.DocumentNode.SelectNodes("/html/body/div[3]/div/div[1]/div/div[1]/div[2]/div/div[2]")?.FirstOrDefault();

      string[] actors = null;
      string[] directors = null;
      string[] generes = null;
      int? year = null;

      if (infoNode != null)
      {
        generes = infoNode.ChildNodes[1].InnerText.Replace("\t", null).Replace("\n", null).Replace("\r", null).Split("/");

        if (int.TryParse(infoNode.ChildNodes[3].ChildNodes[1].InnerText, out var year1))
        {
          year = year1;
        }

        if (infoNode.ChildNodes.Count >= 6)
        {
          var creatorsNode = infoNode.ChildNodes[5];

          var textDirectors = creatorsNode.ChildNodes[3].InnerText;

          if (textDirectors.Contains("Režie:"))
          {
            directors = textDirectors.Replace("\t", null).Replace("\n", null).Replace("\r", null).Split("Režie:")[1].Split(",");
          }


          if (creatorsNode.ChildNodes.Count >= 12)
          {
            var textActors = creatorsNode.ChildNodes[11].InnerText;

            if (textActors.Contains("Hrají:"))
            {
              actors = textActors.Replace("\t", null).Replace("\n", null).Replace("\r", null).Split("Hrají:")[1].Split(",");
            }
          }
        }
      }


      cSFDTVShowSeasonEpisode.Year = year;
      cSFDTVShowSeasonEpisode.Actors = actors;
      cSFDTVShowSeasonEpisode.Directors = directors;
      cSFDTVShowSeasonEpisode.OriginalName = originalName?.Replace("(více)", null);
      cSFDTVShowSeasonEpisode.Generes = generes;
      cSFDTVShowSeasonEpisode.SeasonNumber = number.Key != -1 ? (int?)number.Key : null;
      cSFDTVShowSeasonEpisode.EpisodeNumber = number.Value != -1 ? (int?)number.Value : null;
      cSFDTVShowSeasonEpisode.Parameters = parameters.ToArray();
    }

    #endregion

    private string GetClearText(string input)
    {
      return input?.Replace("\t", null).Replace("\r", null).Replace("\n", null).Replace("%", null);
    }

    #region FindItems

    public Task<CSFDQueryResult> FindItems(string name, CancellationToken cancellationToken)
    {
      var query = $"https://www.csfd.cz/hledat/?q={name}";

      cancellationToken.ThrowIfCancellationRequested();

      if (!Initialize())
      {
        return null;
      }

      return GetItems(query, cancellationToken);
    }

    #endregion

    #region GetBestFind

    private CSFDItem bestItem;
    private string lastParsedName;

    public async Task<CSFDItem> GetBestFind(string name, CancellationToken cancellationToken, int? year = null, bool onlySingleItem = false, string tvShowUrl = null, string tvShowName = null, int? seasonNumber = null, int? episodeNumber = null)
    {
      var statusMessage = new StatusMessage(1)
      {
        MessageStatusState = MessageStatusState.Processing,
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
              if (year > 1887)
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

      if (seasonNumber != null)
      {
        episodeKeys = new KeyValuePair<int, int>(seasonNumber.Value, episodeKeys.Value);
      }

      if (episodeNumber != null)
      {
        episodeKeys = new KeyValuePair<int, int>(episodeKeys.Key, episodeNumber.Value);
      }

      if (DataLoader.DataLoader.IsTvShow(episodeKeys) && !onlySingleItem)
      {
        if (tvShowName == null)
        {
          var match = Regex.Match(parsedName, @"\D*");

          if (match.Success)
          {
            parsedName = match.Value;
          }

          tvShowName = parsedName;
        }

        if (tvShowUrl == null)
        {
          var tvShowFind = await FindSingleCsfdItem(tvShowName, year, episodeKeys, cancellationToken);

          if (tvShowFind == null)
          {
            return null;
          }

          tvShowUrl = tvShowFind.Url;
        }

        var tvSHow = LoadTvShow(tvShowUrl, cancellationToken, episodeKeys.Key, episodeNumber);

        if (tvSHow != null)
        {
          if (tvSHow.Seasons != null)
          {
            if (tvSHow.Seasons.Count >= episodeKeys.Key)
            {
              var season = tvSHow.Seasons[episodeKeys.Key - 1];

              if (season.SeasonEpisodes != null)
              {
                if (season.SeasonEpisodes.Count == 1)
                {
                  return season.SeasonEpisodes[0];
                }
                else if (episodeKeys.Value != -1 && season.SeasonEpisodes.Count >= episodeKeys.Value)
                {
                  return season.SeasonEpisodes[episodeKeys.Value - 1];
                }
              }

              return season;
            }
          }

          return tvSHow;
        }
        else
        {
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


        return await FindSingleCsfdItem(parsedName, year, episodeKeys, cancellationToken, true);
      }
    }

    #endregion

    #region FindSingleCsfdItem

    private async Task<CSFDItem> FindSingleCsfdItem(string parsedName, int? year, KeyValuePair<int, int> episodeNumber, CancellationToken cancellationToken,  bool showStatusMassage = false)
    {
      lastParsedName = parsedName;

      var statusMessage = new StatusMessage(2)
      {
        MessageStatusState = MessageStatusState.Processing,
        Message = $"Finding item"
      };

      if (showStatusMassage)
      {
        statusManager.UpdateMessage(statusMessage);
      }

      var items = await FindItems(parsedName, cancellationToken);

      if (showStatusMassage)
        statusManager.UpdateMessageAndIncreaseProcessCount(statusMessage);

      var allItems = items.AllItems?.ToList();

      if (allItems == null || allItems.Count == 0)
      {
        return null;
      }

      double minSimilarity = 0.55;

      var query = allItems.Where(x => x.OriginalName != null)
        .Where(x => x.OriginalName.Similarity(parsedName) > minSimilarity)
        .OrderByDescending(x => x.OriginalName.Similarity(parsedName)).AsEnumerable();

      query = query.Concat(allItems.Where(x => x.Name != null)
        .Where(x => x.Name.Similarity(parsedName) > minSimilarity)
        .OrderByDescending(x => x.Name.Similarity(parsedName)).AsEnumerable());

      if (year != null)
      {
        query = query.Where(x => x.Year == year);
      }

      var sortedItemsQuery = query.OrderByDescending(x => x.RatingColor).ThenByDescending(x => x.Year).AsQueryable();

      if (episodeNumber.Key != -1 && episodeNumber.Value != -1)
      {
        sortedItemsQuery = sortedItemsQuery.Where(x => x.Parameters.Contains("TV seriál"));
      }

      var sortedItems = sortedItemsQuery.ToList();

      bestItem = sortedItems.FirstOrDefault();

      if (bestItem != null)
      {
        bestItem.Rating = GetCsfdRating(bestItem);
        bestItem.ImagePath = GetCsfdImage(bestItem);
      }

      if (showStatusMassage)
        statusManager.UpdateMessageAndIncreaseProcessCount(statusMessage);

      return bestItem;
    }

    #endregion

    #region GetItems

    private async Task<CSFDQueryResult> GetItems(string url, CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();
      CSFDQueryResult result = new CSFDQueryResult();
      chromeDriver.Url = url;
      chromeDriver.Navigate();

      var document = new HtmlDocument();

      cancellationToken.ThrowIfCancellationRequested();
      document.LoadHtml(chromeDriver.PageSource);

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
        var nodes = document.DocumentNode.SelectNodes("/html/body/div[3]/div/div[2]/div/div[1]/div/div[1]/section[1]/div/div[1]/article");

        return ParseFindNodes(nodes, cancellationToken);
      });
    }

    #endregion

    #region GetCsfdRating

    private int? GetCsfdRating(CSFDItem cSFDItem)
    {
      chromeDriver.Url = cSFDItem.Url;
      chromeDriver.Navigate();

      var document = new HtmlDocument();

      document.LoadHtml(chromeDriver.PageSource);

      var ratingNode = document.DocumentNode.SelectNodes("/html/body/div[3]/div/div[1]/aside/div[1]/div[2]/div")?.FirstOrDefault()?.InnerText.Replace("\t", null).Replace("\r", null).Replace("\n", null).Replace("%", null);

      if (int.TryParse(ratingNode, out var rating))
      {
        return rating;
      }
      else
      {
        ratingNode = document.DocumentNode.SelectNodes("/html/body/div[4]/div/div[1]/aside/div[1]/div[2]/div")?.FirstOrDefault()?.InnerText.Replace("\t", null).Replace("\r", null).Replace("\n", null).Replace("%", null);

        if (int.TryParse(ratingNode, out rating))
        {
          return rating;
        }
      }

      return null;
    }

    #endregion

    #region GetCsfdImage

    private string GetCsfdImage(CSFDItem cSFDItem)
    {
      chromeDriver.Url = cSFDItem.Url;
      chromeDriver.Navigate();

      var document = new HtmlDocument();

      if (chromeDriver.PageSource != null)
      {
        document.LoadHtml(chromeDriver.PageSource);

        var node = document?.DocumentNode?.SelectNodes("/html/body/div[3]/div/div[1]/div/div[1]/div[2]/div/div[1]/div[1]/a/img")?.FirstOrDefault();

        if (node == null)
        {
          node = document?.DocumentNode?.SelectNodes("/html/body/div[3]/div/div[1]/div/div/div[2]/div/div[1]/div[1]/img")?.FirstOrDefault();
        }

        if (node != null)
        {
          if (node.Attributes.Count >= 3)
          {
            var urlValue = node.Attributes[2]?.Value;

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

        var name = posterNode.Attributes[0].Value;

        string posterUrl = null;

        var urlValue = posterNode.ChildNodes[1].Attributes[1].Value;

        if (!urlValue.Contains("data:image"))
        {
          posterUrl = urlValue.Replace("//image.pmgstatic.com", "https://image.pmgstatic.com");
        }

        var url = baseUrl + posterNode.Attributes[1].Value;

        var infoNode = node.ChildNodes[3];
        string originalName = null;

        if (infoNode.ChildNodes[1].ChildNodes.Count > 3)
        {
          originalName = infoNode.ChildNodes[1].ChildNodes[3].InnerText.Replace("(", null).Replace(")", null);
        }


        var tile = infoNode.ChildNodes[1].ChildNodes[1].ChildNodes[3].InnerText;
        var yearStr = tile.Substring(1, 4);

        var regex = new Regex(@"\((.*?)\)");
        var ads = regex.Matches(tile);

        List<string> parameters = new List<string>();

        if (ads.Count > 1)
        {
          for (int i = 1; i < ads.Count; i++)
          {
            parameters.Add(ads[i].Groups[1].Captures[0].Value);
          }
        }

        int year = int.Parse(yearStr);


        var generes = infoNode.ChildNodes[3].InnerText.Replace("\t", null).Replace("\n", null).Replace("\r", null).Split("/");

        string[] actors = null;
        string[] directors = null;

        if (infoNode.ChildNodes.Count >= 6)
        {
          var textDirectors = infoNode.ChildNodes[5].InnerText;

          if (textDirectors.Contains("Režie:"))
          {
            directors = textDirectors.Replace("\t", null).Replace("\n", null).Replace("\r", null).Split("Režie:")[1].Split(",");
          }
        }

        if (infoNode.ChildNodes.Count >= 8)
        {
          var textActors = infoNode.ChildNodes[7].InnerText;

          if (textActors.Contains("Hrají:"))
          {
            actors = textActors.Replace("\t", null).Replace("\n", null).Replace("\r", null).Split("Hrají:")[1].Split(",");
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

        //if (item.ImagePath != null)
        //{
        //  var webClient = new WebClient();

        //  item.Image = webClient.DownloadData("https://image.pmgstatic.com/cache/resized/w60h85/files/images/film/posters/165/598/165598636_4a6e6f.jpg");
        //}

        list.Add(item);
      }

      return list.AsEnumerable();
    }

    #endregion

    #region GetTvShowsFromFind

    private Task<IEnumerable<CSFDItem>> GetTvShowsFromFind(HtmlDocument document, CancellationToken cancellationToken)
    {
      return Task.Run(() =>
      {
        var nodes = document.DocumentNode.SelectNodes("/html/body/div[3]/div/div[2]/div/div[1]/div/div[1]/section[2]/div/div[1]/article");

        return ParseFindNodes(nodes, cancellationToken);
      });
    }

    #endregion
  }
}
