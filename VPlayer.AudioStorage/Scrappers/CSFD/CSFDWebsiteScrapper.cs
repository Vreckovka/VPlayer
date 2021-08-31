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
    private string baseUrl = "https://csfd.sk";
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

    public CSFDTVShow LoadTvShow(string url)
    {
      if (!Initialize())
      {
        return null;
      }

      var tvShow = new CSFDTVShow()
      {
        Url = url
      };

      var statusMessage = new StatusMessage(2)
      {
        MessageStatusState = MessageStatusState.Processing,
        Message = "Downloading tv show basic info"
      };

      statusManager.UpdateMessage(statusMessage);

      var name = GetTvShowName(url, out var posterUrl);

      if (name != null)
      {
        tvShow.Name = name;

        statusManager.UpdateMessageAndIncreaseProcessCount(statusMessage);

        var poster = DownloadPoster(posterUrl);

        if (poster != null)
        {
          tvShow.ImagePath = SaveImage(tvShow.Name, poster);
        }

        statusManager.UpdateMessageAndIncreaseProcessCount(statusMessage);

        tvShow.Seasons = LoadSeasons(statusMessage);

        return tvShow;
      }

      return null;
    }

    #endregion

    #region LoadTvShowSeason

    public CSFDTVShowSeason LoadTvShowSeason(string url)
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
      newSeason.SeasonUrl = url;

      logger.Log(MessageType.Success, $"Tv show season: {newSeason.Name}");

      newSeason.SeasonEpisodes = LoadSeasonEpisodes(newSeason.SeasonUrl);

      statusManager.UpdateMessageAndIncreaseProcessCount(statusMessage);

      return newSeason;
    }

    #endregion

    #region GetTvShowName

    private string GetTvShowName(string url, out string posterUrl)
    {
      chromeDriver.Url = url;
      chromeDriver.Navigate();

      var document = new HtmlDocument();

      document.LoadHtml(chromeDriver.PageSource);

      string node = null;
      int maxCount = 10;
      int i = 0;

      while (node == null && i < maxCount)
      {
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

    private byte[] DownloadPoster(string coverUrl)
    {
      using (var client = new WebClient())
      {
        var downloadedCover = client.DownloadData(coverUrl);

        return downloadedCover;
      }
    }

    #endregion

    #region SaveImage

    private string SaveImage(string tvShowName, byte[] image)
    {
      MemoryStream ms = new MemoryStream(image);
      Image i = Image.FromStream(ms);

      var directory = Path.Combine(AudioInfoDownloader.GetDefaultPicturesPath(), $"TvShows\\{tvShowName}");
      var finalPath = Path.Combine(directory, "poster.jpg");

      finalPath.EnsureDirectoryExists();

      if (File.Exists(finalPath))
        File.Delete(finalPath);

      i.Save(finalPath, ImageFormat.Jpeg);

      return finalPath;
    }

    #endregion

    #region LoadSeasons

    private List<CSFDTVShowSeason> LoadSeasons(StatusMessage statusMessage)
    {
      var seasons = new List<CSFDTVShowSeason>();

      var document = new HtmlDocument();

      document.LoadHtml(chromeDriver.PageSource);

      var nodes = document.DocumentNode.SelectNodes("/html/body/div[3]/div/div[1]/div/section[1]/div[2]/div/ul/li/h3/a");

      statusMessage = new StatusMessage(nodes.Count)
      {
        MessageStatusState = MessageStatusState.Processing,
        Message = "Downloading tv show seasons"
      };

      statusManager.UpdateMessage(statusMessage);

      foreach (var node in nodes)
      {
        var newSeason = new CSFDTVShowSeason();

        newSeason.Name = node.InnerText;
        newSeason.SeasonUrl = baseUrl + node.Attributes.FirstOrDefault(x => x.Name == "href")?.Value;

        seasons.Add(newSeason);

        logger.Log(MessageType.Success, $"Tv show season: {newSeason.Name}");
      }


      foreach (var season in seasons)
      {
        season.SeasonEpisodes = LoadSeasonEpisodes(season.SeasonUrl);

        statusManager.UpdateMessageAndIncreaseProcessCount(statusMessage);
      }

      return seasons;
    }

    #endregion

    #region LoadSeasonEpisodes

    private List<CSFDTVShowSeasonEpisode> LoadSeasonEpisodes(string url)
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

        foreach (var node in nodes)
        {
          var newEpisode = new CSFDTVShowSeasonEpisode();

          newEpisode.Name = node.InnerText;
          newEpisode.Url = baseUrl + node.Attributes.FirstOrDefault(x => x.Name == "href")?.Value;

          episodes.Add(newEpisode);
          logger.Log(MessageType.Success, $"Tv show episode: {newEpisode.Name}");
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

    #region FindItems

    public Task<CSFDQueryResult> FindItems(string name)
    {
      var query = $"https://www.csfd.cz/hledat/?q={name}";

      if (!Initialize())
      {
        return null;
      }

      return GetItems(query);
    }

    #endregion

    #region GetBestFind

    public async Task<CSFDItem> GetBestFind(string name, int? year = null)
    {
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

      var items = await FindItems(parsedName);

      List<CSFDItem> allItems = null;

      if (items.Movies != null && items.TvShows != null)
      {
        allItems = items.Movies.Concat(items.TvShows).ToList();
      }
      else if (items.Movies != null)
      {
        allItems = items.Movies.ToList();
      }
      else if (items.TvShows != null)
      {
        allItems = items.TvShows.ToList();
      }

      if (allItems == null)
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

      var sortedItems = query.OrderByDescending(x => x.RatingColor).ThenByDescending(x => x.Year).ToList();

      return sortedItems.FirstOrDefault();
    }

    #endregion

    #region GetItems

    private async Task<CSFDQueryResult> GetItems(string url)
    {
      CSFDQueryResult result = new CSFDQueryResult();
      chromeDriver.Url = url;
      chromeDriver.Navigate();

      var document = new HtmlDocument();

      document.LoadHtml(chromeDriver.PageSource);

      var movies = await GetMoviesFromFind(document);
      var tvShows = await GetTvShowsFromFind(document);

      result.Movies = movies;
      result.TvShows = tvShows;

      return result;
    }

    #endregion

    #region GetMoviesFromFind

    private Task<IEnumerable<CSFDItem>> GetMoviesFromFind(HtmlDocument document)
    {
      return Task.Run(() =>
      {
        var nodes = document.DocumentNode.SelectNodes("/html/body/div[3]/div/div[2]/div/div[1]/div/div[1]/section[1]/div/div[1]/article");

        return ParseFindNodes(nodes);
      });
    }

    #endregion

    #region ParseFindNodes

    private IEnumerable<CSFDItem> ParseFindNodes(HtmlNodeCollection nodes)
    {
      if (nodes == null)
      {
        return null;
      }

      var list = new List<CSFDItem>();

      foreach (var node in nodes)
      {
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

    private Task<IEnumerable<CSFDItem>> GetTvShowsFromFind(HtmlDocument document)
    {
      return Task.Run(() =>
      {
        var nodes = document.DocumentNode.SelectNodes("/html/body/div[3]/div/div[2]/div/div[1]/div/div[1]/section[2]/div/div[1]/article");

        return ParseFindNodes(nodes);
      });
    }

    #endregion
  }
}
