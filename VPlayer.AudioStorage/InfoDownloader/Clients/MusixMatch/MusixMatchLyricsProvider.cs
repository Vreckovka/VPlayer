using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.XPath;
using ChromeDriverScrapper;
using HtmlAgilityPack;
using Logger;
using OpenQA.Selenium;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.InfoDownloader.LRC.Clients.Google;

namespace VPlayer.AudioStorage.InfoDownloader.Clients.MusixMatch
{
  public class MusixMatchSong
  {
    public string Title { get; set; }
    public string Url { get; set; }
  }
  public class MusixMatchAlbum
  {
    public string Name { get; set; }

    public string Artist { get; set; }

    public IEnumerable<MusixMatchSong> Songs { get; set; }

  }
  public class MusixMatchLyricsProvider
  {
    private readonly IChromeDriverProvider chromeDriverProvider;
    private readonly ILogger logger;
    private List<MusixMatchAlbum> loadedAlbums = new List<MusixMatchAlbum>();

    public MusixMatchLyricsProvider(IChromeDriverProvider chromeDriverProvider, ILogger logger)
    {
      this.chromeDriverProvider = chromeDriverProvider ?? throw new ArgumentNullException(nameof(chromeDriverProvider));
      this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Song UpdateSongLyrics(string artist, string albumName, string songName, Song song)
    {
      try
      {
        HtmlDocument document = new HtmlDocument();
        MusixMatchAlbum storedAlbum = null;

        storedAlbum = loadedAlbums.FirstOrDefault(x => GetNormalizedName(x.Name) == GetNormalizedName(albumName) && GetNormalizedName(x.Artist) == GetNormalizedName(artist));

        if (storedAlbum == null)
        {
          var songs = GetAlbumSongs(artist, albumName);

          if (songs != null)
          {
            storedAlbum = new MusixMatchAlbum()
            {
              Artist = artist,
              Name = albumName,
              Songs = songs.ToList()
            };

            loadedAlbums.Add(storedAlbum);
          }
        }

        var foundSong = storedAlbum?.Songs.FirstOrDefault(x => GetNormalizedName(x.Title) == GetNormalizedName(songName));

        if (foundSong != null)
        {
          var url = $"https://www.musixmatch.com{foundSong.Url}";

          var html = chromeDriverProvider.SafeNavigate(url);

          if (html != null)
          {
            document.LoadHtml(html);

            var lyrics = TrySelectLyrics(document.DocumentNode);

            if (string.IsNullOrEmpty(lyrics))
            {
              lyrics = TryGetLyricsDirectly(artist, songName);
            }

            if (!string.IsNullOrEmpty(lyrics))
            {
              song.Chartlyrics_Lyric = lyrics + "\nFrom MusixMatch";

              return song;
            }
          }
        }

        return null;
      }
      catch (Exception ex)
      {
        logger.Log(ex, false);
      }
    }

    private string GetNormalizedName(string input)
    {
      if (string.IsNullOrEmpty(input))
      {
        return null;
      }

      input = input.ToLower();
      Regex rgx = new Regex("[^a-zA-Z0-9]");

      return rgx.Replace(input.ToLower(), "");
    }

    #region GetAlbumSongs

    private IEnumerable<MusixMatchSong> GetAlbumSongs(string artist, string album)
    {
      HtmlDocument document = new HtmlDocument();
      var url = $"https://www.musixmatch.com/album/{GetAsUrlValue(artist)}/{GetAsUrlValue(album)}";

      var html = chromeDriverProvider.SafeNavigate(url, extraMiliseconds: 500);

      if (html != null)
      {
        document.LoadHtml(html);

        var nodes = document.DocumentNode.SelectNodes("/html/body/div[2]/div/div/div/main/div/div[2]/div/div[2]/div[3]/div/div/ul/li/a");

        var lyrics = nodes?.Select(x => new MusixMatchSong()
        {
          Title = x.SelectNodes("div[2]/h2")?.FirstOrDefault()?.InnerText,
          Url = x.Attributes[0].Value
        });

        return lyrics;
      }

      return null;
    }

    #endregion

    #region GetAsUrlValue

    private string GetAsUrlValue(string value)
    {
      return value.ToLower().Replace(" ", "-").Replace(".", "");
    }

    #endregion

    private string TrySelectLyrics(HtmlNode htmlNode)
    {
      var node = htmlNode?.SelectNodes("//p[contains(@class, 'mxm-lyrics__content')]")?.Select(x => x.InnerText).Aggregate((x, y) => x + "" + y);

      return node;
    }

    private string TryGetLyricsDirectly(string artist, string songName)
    {
      HtmlDocument document = new HtmlDocument();
      var url = $"https://www.musixmatch.com/lyrics/{GetAsUrlValue(artist)}/{GetAsUrlValue(songName)}";

      var html = chromeDriverProvider.SafeNavigate(url);

      if (html != null)
      {
        document.LoadHtml(html);

        return document.DocumentNode.SelectNodes("/html/body/div[2]/div/div/div/main/div/div/div[3]/div[1]/div/div/div/div[2]/div[2]/span/div/p")?.FirstOrDefault()?.InnerText;
      }

      return null;
    }
  }
}
