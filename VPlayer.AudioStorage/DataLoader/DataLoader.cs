using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;
using VCore.Helpers;
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

    private List<TvShowSeason> clearedSeasons = new List<TvShowSeason>();
    public void AddtvTvShowSeasons(string path, TvShow tvShow)
    {
      var statusMessage = new StatusMessage(1)
      {
        Message = "Getting file info"
      };

      statusManager.UpdateMessage(statusMessage);

      var files = LoadData(DataType.Video, path, true);

      TvShowSeason tvShowSeason;

      foreach (var file in files)
      {
        var seriesNumber = GetTvShowSeriesNumber(file.Name);

        tvShowSeason = tvShow.Seasons.SingleOrDefault(x => x.SeasonNumber == seriesNumber.Key);

        if (tvShowSeason == null)
        {
          tvShowSeason = new TvShowSeason()
          {
            SeasonNumber = seriesNumber.Key,
            Episodes = new List<TvShowEpisode>(),
            TvShow = tvShow
          };

          tvShow.Seasons.Add(tvShowSeason);
        }
        else if(!clearedSeasons.Contains(tvShowSeason) && tvShowSeason.Id != 0)
        {
          tvShowSeason.Episodes.Clear();
          clearedSeasons.Add(tvShowSeason);
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

        if (seriesNumber.Key != -1)
        {
          tvEpisode.EpisodeNumber = seriesNumber.Value;
        }

        tvShowSeason.Episodes.Add(tvEpisode);
      }

      statusManager.UpdateMessageAndIncreaseProcessCount(statusMessage);
    }

    #endregion

    #region GetTvShowSeriesNumber

    public static KeyValuePair<int, int> GetTvShowSeriesNumber(string name)
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
        @"\[(?<season>\d{1,2}).(?<episode>\d{1,2})-(?<concatEpisode>\d{1,2})\]"
      };

      foreach (var regexExpression in regexExpressions)
      {
        Regex regex = new Regex(regexExpression);

        Match match = regex.Match(name.ToLower());

        if (match.Success)
        {
          string season = match.Groups["season"].Value;
          string episode = match.Groups["episode"].Value;

          return new KeyValuePair<int, int>(int.Parse(season), int.Parse(episode));
        }
      }

      return new KeyValuePair<int, int>(-1, -1);
    }

    #endregion

    #region IsTvShow

    public static bool IsTvShow(KeyValuePair<int, int> episodeNumber)
    {
      if (episodeNumber.Key != -1 && episodeNumber.Value != -1)
      {
        return true;
      }

      return false;
    }


    public static bool IsTvShow(string name )
    {
      var episodeNumber = GetTvShowSeriesNumber(name);

      if (episodeNumber.Key != -1 && episodeNumber.Value != -1)
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
