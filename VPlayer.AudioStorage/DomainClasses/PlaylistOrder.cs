using System;
using System.Collections.Generic;
using System.Linq;

namespace VPlayer.AudioStorage.DomainClasses
{
  public static class PlaylistOrder
  {
    // Keep unavailable entries in their saved slots and match duplicate tracks individually.
    public static List<T> ReorderAvailable<T>(IEnumerable<T> saved, IEnumerable<int> orderedIds,
      Func<T, int> id, Func<T, bool> available)
    {
      var entries = saved.ToList();
      var byId = entries.Where(available).GroupBy(id)
        .ToDictionary(x => x.Key, x => new Queue<T>(x));
      var reordered = new Queue<T>();
      foreach (var itemId in orderedIds)
      {
        if (!byId.TryGetValue(itemId, out var matches) || matches.Count == 0)
          throw new InvalidOperationException("The playlist is still loading. Please try reordering again.");
        reordered.Enqueue(matches.Dequeue());
      }
      if (byId.Values.Any(x => x.Count != 0))
        throw new InvalidOperationException("The playlist is still loading. Please try reordering again.");
      return entries.Select(x => available(x) ? reordered.Dequeue() : x).ToList();
    }

    public static bool HasChanges<T>(IEnumerable<T> saved, IEnumerable<T> incoming,
      Func<T, int> rowId, Func<T, int> itemId, Func<T, int> order)
    {
      return !saved.OrderBy(order).ThenBy(rowId).Select(x => (rowId(x), itemId(x), order(x)))
        .SequenceEqual(incoming.OrderBy(order).ThenBy(rowId).Select(x => (rowId(x), itemId(x), order(x))));
    }
  }
}
