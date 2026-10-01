using System;
using System.Collections.Generic;
using System.Linq;

namespace VPlayer.Core.ViewModels.SoundItems
{
  // Snapshot timestamp order while retaining the original display indices.
  internal sealed class LyricsTimeline
  {
    private readonly (TimeSpan Time, int Index)[] entries;

    public LyricsTimeline(IEnumerable<TimeSpan?> timestamps)
    {
      entries = timestamps.Select((time, index) => (Time: time, Index: index))
        .Where(x => x.Time.HasValue)
        .Select(x => (Time: x.Time.Value, Index: x.Index))
        .OrderBy(x => x.Time).ThenByDescending(x => x.Index).ToArray();
    }

    public int FindIndex(double milliseconds)
    {
      int low = 0, high = entries.Length - 1, selected = -1;
      while (low <= high)
      {
        int middle = low + (high - low) / 2;
        if (entries[middle].Time.TotalMilliseconds <= milliseconds)
        {
          selected = middle;
          low = middle + 1;
        }
        else
          high = middle - 1;
      }
      return selected < 0 ? -1 : entries[selected].Index;
    }
  }
}
