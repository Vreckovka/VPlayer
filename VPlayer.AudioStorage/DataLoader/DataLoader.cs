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
      var statusMessage = new StatusMessage(1)
      {
        Message = "Getting file info"
      };

      statusManager.UpdateMessage(statusMessage);

      statusMessage.ProcessedCount++;

      statusMessage.MessageStatusState = MessageStatusState.Done;

      statusManager.UpdateMessage(statusMessage);

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


    }

    #endregion

    #region GetTvShowSeriesNumber

    public KeyValuePair<int, int> GetTvShowSeriesNumber(string name)
    {
      string sample = name;

      List<string> regexExpressions = new List<string>()
      {
        //S01E01
        @"S(?<season>\d{1,2})E(?<episode>\d{1,2})",
        //S01E01
        @"(?<season>\d{1,2})x(?<episode>\d{1,2})"
      };

      foreach (var regexExpression in regexExpressions)
      {
        Regex regex = new Regex(regexExpression);

        Match match = regex.Match(sample);

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
