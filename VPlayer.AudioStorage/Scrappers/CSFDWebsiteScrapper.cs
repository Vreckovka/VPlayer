using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using HtmlAgilityPack;
using Logger;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using VCore;
using VCore.Standard;
using VPlayer.AudioStorage.InfoDownloader;
using VPlayer.AudioStorage.InfoDownloader.Models;
using VPlayer.Core.Managers.Status;
using WebsiteParser;

namespace VPlayer.AudioStorage.Parsers
{

  public class CSFDWebsiteScrapper : ICSFDWebsiteScrapper
  {
    private readonly ILogger logger;
    private readonly IStatusManager statusManager;
    private ChromeDriver chromeDriver;
    private string baseUrl = "https://csfd.sk";

    public CSFDWebsiteScrapper(ILogger logger, IStatusManager statusManager)
    {
      this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
      this.statusManager = statusManager ?? throw new ArgumentNullException(nameof(statusManager));
    }

    public bool Initialize()
    {
      try
      {
        var chromeOptions = new ChromeOptions();

        chromeOptions.AddArguments(new List<string>() { "headless", "disable-infobars", "--log-level=3" });

        var chromeDriverService = ChromeDriverService.CreateDefaultService(Directory.GetCurrentDirectory());

        chromeDriverService.HideCommandPromptWindow = true;


        chromeDriver = new ChromeDriver(chromeDriverService, chromeOptions);

        return true;
      }
      catch (Exception ex)
      {
        logger.Log(ex);
        return false;
      }
    }


    public CSFDTVShow LoadTvShow(string url)
    {
      if (!Initialize())
      {
        return null;
      }

      var tvShow = new CSFDTVShow()
      {
        TvShowUrl = url
      };

      var statusMessage = new StatusMessage(2)
      {
        MessageStatusState = MessageStatusState.Processing,
        Message = "Downloading tv show basic info"
      };

      statusManager.UpdateMessage(statusMessage);

      tvShow.Name = GetTvShowName(url, out var posterUrl);

      statusManager.UpdateMessageAndIncreaseProcessCount(statusMessage);

      var poster = DownloadPoster(posterUrl);

      if (poster != null)
      {
        tvShow.PosterPath = SaveImage(tvShow.Name, poster);
      }

      statusManager.UpdateMessageAndIncreaseProcessCount(statusMessage);

      tvShow.Seasons = LoadSeasons(statusMessage);

      return tvShow;
    }

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


    #region GetTvShowName

    private string GetTvShowName(string url, out string posterUrl)
    {
      chromeDriver.Url = url;
      chromeDriver.Navigate();

      var document = new HtmlDocument();

      document.LoadHtml(chromeDriver.PageSource);

      var node = document.DocumentNode.SelectNodes("/html/body/div[2]/div/div[1]/div/div[1]/div/div/header/div/h1").FirstOrDefault()?.InnerText;

      var name = node.Replace("\t", string.Empty).Replace(" (TV seriál)", string.Empty).Replace("\r", null).Replace("\n", null);

      logger.Log(MessageType.Success, $"Tv show name: {name}");

      var posterNode = chromeDriver.FindElement(By.XPath("/html/body/div[2]/div/div[1]/div/div[1]/div/div/div[1]/div[1]/a/img"));

      posterUrl = posterNode.GetAttribute("src");

      return name;
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

      var nodes = document.DocumentNode.SelectNodes("/html/body/div[2]/div/div[1]/div/section[1]/div[2]/div/ul/li/h3/a");

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


        var nodes = document.DocumentNode.SelectNodes("/html/body/div[2]/div/div[1]/div[3]/div/ul/li/a");

        if (nodes == null)
        {
          nodes = document.DocumentNode.SelectNodes("/html/body/div[2]/div/div[1]/div/section[1]/div[2]/div/ul/li/h3/a");
        }

        if (nodes == null)
        {
          nodes = document.DocumentNode.SelectNodes("/html/body/div[2]/div/div[1]/div/section/div[2]/div/ul/li/h3/a");
        }

        if (nodes == null)
        {
          return episodes;
        }

        foreach (var node in nodes)
        {
          var newEpisode = new CSFDTVShowSeasonEpisode();

          newEpisode.Name = node.InnerText;
          newEpisode.EpisodeUrl = baseUrl + node.Attributes.FirstOrDefault(x => x.Name == "href")?.Value;

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

    #region TryFind

    private bool TryFind(string xPath, out IWebElement webElement)
    {
      try
      {
        webElement = chromeDriver.FindElement(By.XPath(xPath));
        return true;
      }
      catch (Exception ex)
      {
        webElement = null;
        return false;
      }
    }

    #endregion

    #region LoadTestHtml

    private void LoadTestHtml(string filePath)
    {
      var html = File.ReadAllText(filePath);

      var document = new HtmlDocument();

      document.LoadHtml(html);

      //#children > div.content.columns
      var seasonsXPath = "body/div[2]/div[3]/div[1]/div[3]/div[2]/ul/li/a";
      var nodes = document.DocumentNode.SelectNodes(seasonsXPath);


    }

    #endregion
  }

  public class CSFDTVShow
  {
    public string Name { get; set; }
    public string TvShowUrl { get; set; }
    public string PosterPath { get; set; }
    public List<CSFDTVShowSeason> Seasons { get; set; }
  }

  public class CSFDTVShowSeason
  {
    public string Name { get; set; }
    public string SeasonUrl { get; set; }

    public List<CSFDTVShowSeasonEpisode> SeasonEpisodes { get; set; }
  }

  public class CSFDTVShowSeasonEpisode
  {
    public string Name { get; set; }
    public string EpisodeUrl { get; set; }
  }
}
