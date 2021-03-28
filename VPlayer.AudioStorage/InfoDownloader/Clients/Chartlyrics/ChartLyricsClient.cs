using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.InfoDownloader.Clients.Chartlyrics.XMLClasses;
using Logger;
using VCore;

namespace VPlayer.AudioStorage.InfoDownloader.Clients.Chartlyrics
{
  public class ChartLyricsClient
  {
    private readonly ILogger logger;

    public ChartLyricsClient(ILogger logger)
    {
      this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Song> UpdateSongLyrics(string artistName, string songName, Song song)
    {
      try
      {
        if (!string.IsNullOrEmpty(artistName) && !string.IsNullOrEmpty(artistName))
        {
          var url = $"http://api.chartlyrics.com/apiv1.asmx/SearchLyric?artist={artistName}&song={songName}";

          var client = new HttpClient();
          var resutl = await client.GetStringAsync(url);

          var xmlNode = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n\n";

          var validResult = resutl.Substring(xmlNode.Length - 1, resutl.Length - xmlNode.Length) + ">";

          XmlDocument doc = new XmlDocument();
          doc.LoadXml(validResult);

          ArrayOfSearchLyricResult obj;

          using (TextReader textReader = new StringReader(doc.OuterXml))
          {
            using (XmlTextReader reader = new XmlTextReader(textReader))
            {
              XmlSerializer serializer = new XmlSerializer(typeof(ArrayOfSearchLyricResult));
              obj = (ArrayOfSearchLyricResult)serializer.Deserialize(reader);
            }
          }


          var searchLyricResult = obj.SearchLyricResult.Where(x => !string.IsNullOrEmpty(x.Artist) && !string.IsNullOrEmpty(x.Song))
            .OrderBy(x => x.Artist.LevenshteinDistance(artistName) + x.Song.LevenshteinDistance(songName)).ToList();

          var bestResult = searchLyricResult.FirstOrDefault(x => x.LyricChecksum != null);

          if (bestResult != null)
          {

            bool isValid = bestResult.Artist.Similarity(artistName, true) > 0.9 && bestResult.Song.Similarity(songName, true) > 0.9;

            if (!isValid)
            {
              isValid = bestResult.Artist.LevenshteinDistance(artistName) == 0 && bestResult.Song.Contains(songName);

              if (!isValid)
              {
                isValid = bestResult.Artist.LevenshteinDistance(artistName) == 0 && bestResult.Song.Similarity(songName, true) > 0.5;
              }
            }

            if (isValid)
            {

              var lyricsUrl = $"http://api.chartlyrics.com/apiv1.asmx/GetLyric?lyricId={bestResult.LyricId}&lyricCheckSum={bestResult.LyricChecksum}";

              var lyrics = await GetAttributes(lyricsUrl);

              if (!string.IsNullOrEmpty(lyrics))
              {
                song.Chartlyrics_Lyric = lyrics;
                song.Chartlyrics_LyricCheckSum = bestResult.LyricChecksum;
                song.Chartlyrics_LyricId = bestResult.LyricId;

                return song;

              }
            }
          }
        }

        return null;
      }
      catch (HttpRequestException ex)
      {
        if (ex.Message.Contains("500"))
        {

        }
        else
        {
          logger.Log(ex);
        }

        return null;
      }
      catch (Exception ex)
      {
        logger.Log(ex);
        return null;
      }
    }

    private async Task<string> GetAttributes(string url)
    {
      try
      {
        var client = new HttpClient();
        var resutl = await client.GetStringAsync(url);

        var xmlNode = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n\n";

        var validResult = resutl.Substring(xmlNode.Length - 1, resutl.Length - xmlNode.Length) + ">";

        XmlDocument doc = new XmlDocument();
        doc.LoadXml(validResult);

        GetLyricResult obj;

        using (TextReader textReader = new StringReader(doc.OuterXml))
        {
          using (XmlTextReader reader = new XmlTextReader(textReader))
          {
            XmlSerializer serializer = new XmlSerializer(typeof(GetLyricResult));
            obj = (GetLyricResult)serializer.Deserialize(reader);
          }
        }

        return obj.Lyric;
      }
      catch (Exception ex)
      {
        logger.Log(ex);
        return null;
      }
    }
  }
}

