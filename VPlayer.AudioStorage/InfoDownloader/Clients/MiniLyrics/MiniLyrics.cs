using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VPlayer.AudioStorage.DomainClasses;

namespace VPlayer.AudioStorage.InfoDownloader.Clients.MiniLyrics
{
  public class MiniLyricsClient
  {
    private string baseLrcFolder = "C:\\Lyrics";

    public Song UpdateSongWithLrc(string songName, string artistName, Song song, out LRCFile lrcFile)
    {
      if (!string.IsNullOrEmpty(songName) && Directory.Exists(Path.Combine(baseLrcFolder, artistName)))
      {
        DirectoryInfo lrcDirectory = new DirectoryInfo(baseLrcFolder);

        if (!string.IsNullOrEmpty(artistName))
        {
          lrcDirectory = new DirectoryInfo(Path.Combine(baseLrcFolder, artistName));
        }

        var lrcFilePath = FindLrcFile(songName, lrcDirectory);

        if (lrcFilePath != null)
          return UpdateSong(lrcFilePath, song, out lrcFile);
      }

      lrcFile = null;
      return null;
    }

    private string FindLrcFile(string lrcFileName, DirectoryInfo LrcDirectoryInfo)
    {
      if (!string.IsNullOrEmpty(lrcFileName) && LrcDirectoryInfo != null)
      {
        var filesInDir = LrcDirectoryInfo.GetFiles($"*{lrcFileName}*.*").ToList();

        if (filesInDir.Count == 1)
          return filesInDir?.FirstOrDefault()?.FullName;
        else if (filesInDir.Count == 0)
          return null;
        else
          throw new NotImplementedException();
      }

      return null;
    }

    public Song UpdateSong(string lrcFilePath, Song song, out LRCFile lrcFile)
    {
      var parser = new LRCParser();

      lrcFile = parser.Parse(lrcFilePath);

      if (lrcFile != null)
      {
        song.LRCLyrics = lrcFile.GetString();
        return song;
      }

      if (File.Exists(lrcFilePath) && lrcFile == null)
      {
        throw new Exception("Could not parse " + lrcFilePath);
      }

      return null;
    }
  }

  public class LRCLyricLine
  {
    public TimeSpan? Timestamp { get; set; }
    public string Text { get; set; }
    public string OriginalLine { get; set; }

    public override string ToString()
    {
      return $"[{Timestamp}]{Text}";
    }
  }

  public class LRCFile
  {
    public LRCFile(List<LRCLyricLine> lines)
    {
      Lines = lines;
    }

    public List<LRCLyricLine> Lines { get; private set; }

    public string Id { get; set; }
    public string Artist { get; set; }
    public string Title { get; set; }
    public string Album { get; set; }
    public string By { get; set; }
    public TimeSpan? Length { get; set; }

    public string GetString()
    {
      var toString = $"[id:{Id}]";
      toString += $"\n[ar:{Artist}]";
      toString += $"\n[ti:{Title}]";
      toString += $"\n[al:{Album}]";
      toString += $"\n[by:{By}]";
      toString += $"\n[length:{Length}]";

      toString += "\n" + String.Join("\n", Lines.Select(x => x.ToString()).ToArray());

      return toString;
    }
  }

  public class LRCParser
  {
    private char parameterSeparator = ':';

    private bool IsTimestamp(string item)
    {
      if (item[0] == '[' && item[item.Length - 1] == ']' && ParseTime(item.Substring(1, item.Length - 2)) != null)
      {
        return true;
      }

      return false;
    }

    public LRCFile Parse(List<string> lines)
    {
      var id = ParseLrcParameterLine(lines.SingleOrDefault(x => x.Contains($"id{parameterSeparator}")));
      var artist = ParseLrcParameterLine(lines.SingleOrDefault(x => x.Contains($"ar{parameterSeparator}")));
      var album = ParseLrcParameterLine(lines.SingleOrDefault(x => x.Contains($"ti{parameterSeparator}")));
      var title = ParseLrcParameterLine(lines.SingleOrDefault(x => x.Contains($"al{parameterSeparator}")));
      var by = ParseLrcParameterLine(lines.SingleOrDefault(x => x.Contains($"by{parameterSeparator}")));
      var length = ParseTime(ParseLrcParameterLine(lines.SingleOrDefault(x => x.Contains($"length{parameterSeparator}"))));

      var bla = 0;

      var lyricsLines = lines.Where(x => x.Length > 2 && char.IsDigit(x[1])).Select(ParseLrcLyricLine).Where(x => x != null).SelectMany(x => x).OrderBy(x => x.Timestamp).ToList();
      var nullLines = lyricsLines.Where(x => string.IsNullOrEmpty(x.Text)).ToList();

      if (nullLines.Count > 0)
      {
        foreach (var line in nullLines)
        {
          var originalLineIndex = lines.IndexOf(line.OriginalLine);

          if (lines.Count - 1 > originalLineIndex && !IsTimestamp(lines[originalLineIndex + 1].Split(']')[0] + "]"))
          {
            line.Text = lines[originalLineIndex + 1];
          }
        }

      }

      return new LRCFile(lyricsLines)
      {
        Id = id,
        Album = album,
        Artist = artist,
        By = by,
        Length = length,
        Title = title,
      };
    }

    public LRCFile Parse(string path)
    {
      var lines = File.ReadAllLines(path);

      return Parse(lines.ToList());
    }

    private TimeSpan? ParseTime(string item)
    {
      if (!string.IsNullOrEmpty(item))
      {
        var time = item.Split(':');

        if (time.Length == 2)
        {
          if (double.TryParse(time[0], out var minutes))
          {
            if (double.TryParse(time[1].Replace('.', ','), out var seconds))
            {
              return TimeSpan.FromMinutes(minutes + (seconds / 60));
            }
          }
        }
        else
        {
          if (TimeSpan.TryParse(item, out var formatProvider))
            return formatProvider;
        }
      }

      return null;
    }

    private string ParseLrcParameterLine(string line)
    {
      if (!string.IsNullOrEmpty(line))
      {
        var split = line.Split(parameterSeparator);

        if (split.Length <= 2)
        {
          return split.Last()?.TrimEnd(']');
        }
        else if (line.Contains("length"))
        {
          string time = "";

          for (int i = 1; i < split.Length; i++)
          {
            var minsplit = split[i];

            if (minsplit.Last() == ']')
            {
              minsplit = minsplit.TrimEnd(']');
            }

            time += minsplit;

            if (i != split.Length - 1)
            {
              time += ":";
            }
          }

          return time;
        }
        else
        {
          throw new NotImplementedException();
        }
      }

      return null;
    }

    private IEnumerable<LRCLyricLine> ParseLrcLyricLine(string line)
    {
      var list = new List<LRCLyricLine>();
      if (!string.IsNullOrEmpty(line))
      {
        var split = line.Split(']').ToList();

        if (split.Count == 2)
        {
          var timeStamp = split[0].Substring(1, split[0].Length - 1);
          var text = split[1];

          list.Add(new LRCLyricLine()
          {
            Text = text,
            Timestamp = ParseTime(timeStamp),
            OriginalLine = line
          });
        }
        else
        {
          var timeStamps = split.Where(x => !string.IsNullOrEmpty(x) && x[0] == '[' && IsTimestamp(x + "]")).Select(x => x).ToList();

          string text = null;

          if (timeStamps.Count > 0)
          {
            var textIndex = split.IndexOf(timeStamps.Last()) + 1;
            text = split[textIndex];

            if (textIndex + 1 == split.Count - 1 && string.IsNullOrEmpty(split[textIndex + 1]))
            {
              text += "]";
            }
          }
          else
          {
            text = split.FirstOrDefault();
          }

          foreach (var timeStamp in timeStamps)
          {
            list.Add(new LRCLyricLine()
            {
              Text = text,
              Timestamp = ParseTime(timeStamp.Substring(1, timeStamp.Length - 1)),
              OriginalLine = line
            });
          }
        }

        return list;
      }

      return null;
    }
  }
}
