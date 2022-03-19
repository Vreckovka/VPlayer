using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;
using VCore.WPF.Helpers;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.DomainClasses.Video;
using VPlayer.Core.Managers.Status;
using FileAttributes = System.IO.FileAttributes;

namespace VPlayer.AudioStorage.DataLoader
{
  public enum DataType
  {
    Audio,
    Video
  }

  public class TvShowEpisodeNumbers
  {
    public int? SeasonNumber { get; set; }
    public int? EpisodeNumber { get; set; }
    public string RegexExpression { get; set; }
    public string ParsedName { get; set; }
  }

  public class DataLoader
  {
    private readonly IStatusManager statusManager;
    private Dictionary<DataType, List<string>> supportedItems = new Dictionary<DataType, List<string>>();

    public DataLoader(IStatusManager statusManager)
    {
      this.statusManager = statusManager ?? throw new ArgumentNullException(nameof(statusManager));

      supportedItems.Add(DataType.Video, new List<string>()
      {
        "*.avi", "*.mkv", "*.mp4"
      });
    }

    public List<FileInfo> LoadData(DataType dataType, string path, bool getSubDirectories)
    {
      // get the file attributes for file or directory
      FileAttributes attr = File.GetAttributes(path);

      //detect whether its a directory or file
      if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
      {
        return LoadData(supportedItems[dataType], path, getSubDirectories);
      }
      else
      {
        return new List<FileInfo>();
      }
    }

    #region LoadData

    private List<FileInfo> LoadData(List<string> fileTypes, string directoryPath, bool getSubDirectories, List<FileInfo> aggregatedFileInfos = null)
    {
      if (aggregatedFileInfos == null)
      {
        aggregatedFileInfos = new List<FileInfo>();
      }

      DirectoryInfo d = new DirectoryInfo(directoryPath);

      FileInfo[] files = fileTypes.SelectMany(ext => d.GetFiles(ext)).ToArray();

      if (getSubDirectories)
      {
        var subDirectories = Directory.GetDirectories(directoryPath);

        foreach (var directory in subDirectories)
        {
          aggregatedFileInfos.AddRange(LoadData(fileTypes, directory, true));
        }
      }

      aggregatedFileInfos.AddRange(files);

      return aggregatedFileInfos;
    }

    #endregion

    #region LoadTvShow

    public TvShow LoadTvShow(string tvShowName, string path)
    {
      var tvShow = new TvShow()
      {
        Seasons = new List<TvShowSeason>(),
        Name = tvShowName
      };

      AddtvTvShowSeasons(path, tvShow);

      return tvShow;
    }

    #endregion

    #region AddtvTvShowSeasons

    public void AddtvTvShowSeasons(string path, TvShow tvShow)
    {
      var statusMessage = new StatusMessageViewModel(1)
      {
        Message = "Getting file info"
      };

      statusManager.UpdateMessage(statusMessage);

      var files = LoadData(DataType.Video, path, true);

      TvShowSeason tvShowSeason = null;

      foreach (var file in files)
      {
        var seriesNumber = GetTvShowSeriesNumber(file.Name);

        if(seriesNumber != null)
        {
          tvShowSeason = tvShow.Seasons.SingleOrDefault(x => x.SeasonNumber == seriesNumber.SeasonNumber);
        }

        if(tvShowSeason == null)
        {
          tvShowSeason = new TvShowSeason()
          {
            Episodes = new List<TvShowEpisode>(),
            TvShow = tvShow
          };

          if (seriesNumber?.SeasonNumber != null)
          {
            tvShowSeason.SeasonNumber = seriesNumber.SeasonNumber.Value;
          }

          tvShow.Seasons.Add(tvShowSeason);
        }
      
        var videoItem = new VideoItem()
        {
          Source = file.FullName,
          Duration = (int)GetFileDuration(file.FullName).TotalSeconds,
          Name = file.Name,
         
        };

        var tvEpisode = new TvShowEpisode()
        {
          TvShow = tvShow,
          TvShowSeason = tvShowSeason,
          VideoItem = videoItem
        };

        if (seriesNumber?.EpisodeNumber != null)
        {
          tvEpisode.EpisodeNumber = seriesNumber.EpisodeNumber.Value;
        }

        tvShowSeason.Episodes.Add(tvEpisode);
      }

      statusManager.UpdateMessageAndIncreaseProcessCount(statusMessage);
    }

    #endregion

    #region GetTvShowSeriesNumber

    public static TvShowEpisodeNumbers GetTvShowSeriesNumber(string name)
    {
      List<string> regexExpressions = new List<string>()
      {
        //S01E01
        @"s(?<season>\d{1,2})e(?<episode>\d{1,2})",
        //S01xE01
        @"s(?<season>\d{1,2})xe(?<episode>\d{1,2})",
        //01x01
        @"(?<season>\d{1,2})x(?<episode>\d{1,2})",
        //[1.01]
        @"\[(?<season>\d{1,2}).(?<episode>\d{1,2})\]",
        //[1.01-02]
        @"\[(?<season>\d{1,2}).(?<episode>\d{1,2})-(?<concatEpisode>\d{1,2})\]",
        //1-1
        @"(?<season>\d{1,2})-(?<episode>\d{1,2})",
      };

      foreach (var regexExpression in regexExpressions)
      {
        Regex regex = new Regex(regexExpression);

        Match match = regex.Match(name.ToLower());

        if (match.Success)
        {
          string season = match.Groups["season"].Value;
          string episode = match.Groups["episode"].Value;

          int? seasonNumber = null;
          int? episodeNumber = null;

          if (int.TryParse(season, out var sNumber))
          {
            seasonNumber = sNumber;
          }

          if (int.TryParse(episode, out var eNumber))
          {
            episodeNumber = eNumber;
          }


          var parsedName = name.ToLower().Split(match.Groups[0].Value)[0];

          CultureInfo cultureInfo = Thread.CurrentThread.CurrentCulture;
          TextInfo textInfo = cultureInfo.TextInfo;

          var numbers = new TvShowEpisodeNumbers()
          {
            EpisodeNumber = episodeNumber,
            SeasonNumber = seasonNumber,
            RegexExpression = regexExpression,
            ParsedName = textInfo.ToTitleCase(parsedName)
          };

          return numbers;
        }
      }

      return null;
    }

    #endregion

    #region IsTvShow

    public static bool IsTvShow(string name)
    {
      var episodeNumber = GetTvShowSeriesNumber(name);

      if (episodeNumber != null)
      {
        return true;
      }

      return false;
    }

    #endregion

    #region GetFileDuration

    private TimeSpan GetFileDuration(string path)
    {
      StorageFile file = AsyncHelpers.RunSync(() => StorageFile.GetFileFromPathAsync(path).AsTask());
      var videoProperties = AsyncHelpers.RunSync(() => file.Properties.GetVideoPropertiesAsync().AsTask());

      return videoProperties.Duration;
    }

    #endregion
  }
}
