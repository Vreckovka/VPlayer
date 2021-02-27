using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Logger;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using VCore.Annotations;
using VCore.Standard;
using WebsiteParser;

namespace VPlayer.AudioStorage.Parsers
{

  public class CSFDWebsiteScrapper : ICSFDWebsiteScrapper
  {
    private readonly ILogger logger;
    private ChromeDriver chromeDriver;
    private string baseUrl = "https://www.csfd.cz";

    public CSFDWebsiteScrapper([NotNull] ILogger logger)
    {
      this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

    }

    public bool Initialize()
    {
      try
      {
        var chromeOptions = new ChromeOptions();

        chromeOptions.AddArguments(new List<string>() { "no-sandbox", "headless", "disable-gpu" });

        chromeDriver = new ChromeDriver(chromeOptions);

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

      tvShow.Name = GetTvShowName(url);
      tvShow.Seasons = LoadSeasons();

      return tvShow;
    }


    #region GetTvShowName

    private string GetTvShowName(string url)
    {
      chromeDriver.Url = url;
      chromeDriver.Navigate();

      var document = new HtmlDocument();

      document.LoadHtml(chromeDriver.PageSource);



      var node = chromeDriver.FindElement(By.XPath("/html/body/div[2]/div[3]/div[1]/div[1]/div[1]/div[2]/div[1]/h1")).Text;

      var name = node.Replace("\t", string.Empty).Replace(" (TV seriál)", string.Empty);
      logger.Log(MessageType.Success, $"Tv show name: {name}");

      return name;
    }

    #endregion

    #region LoadSeasons

    private List<CSFDTVShowSeason> LoadSeasons()
    {
      var seasons = new List<CSFDTVShowSeason>();

      int elementIndex = 1;
      bool wasFound;
      var document = new HtmlDocument();

      document.LoadHtml(chromeDriver.PageSource);

      var nodes = document.DocumentNode.SelectNodes("/html/body/div[2]/div[3]/div[1]/div[3]/div[2]/ul/li/a");

      foreach (var node in nodes)
      {
        var newSeason = new CSFDTVShowSeason();

        newSeason.Name = node.InnerText;
        newSeason.SeasonUrl = baseUrl + node.Attributes.FirstOrDefault(x => x.Name == "href")?.Value;

        seasons.Add(newSeason);

        logger.Log(MessageType.Success, $"Tv show season: {newSeason.Name}");
        elementIndex++;
      }


      foreach (var season in seasons)
      {
        season.SeasonEpisodes = LoadSeasonEpisodes(season.SeasonUrl);
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


        var nodes = document.DocumentNode.SelectNodes("html/body/div[2]/div[3]/div[1]/div[3]/div[2]/ul/li/a");

        if (nodes == null)
        {
          nodes = document.DocumentNode.SelectNodes("html/body/div[2]/div[3]/div[1]/div[2]/div[2]/ul/li/a");
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
