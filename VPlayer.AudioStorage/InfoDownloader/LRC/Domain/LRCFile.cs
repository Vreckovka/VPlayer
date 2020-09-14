using System;
using System.Collections.Generic;
using System.Linq;

namespace VPlayer.AudioStorage.InfoDownloader.LRC.Domain
{
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
}