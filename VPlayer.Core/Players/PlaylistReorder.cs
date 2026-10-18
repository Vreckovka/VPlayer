using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace VPlayer.Core.ViewModels
{
  public static class PlaylistReorder
  {
    // Compare queue occurrences, not models: two rows can contain the same song.
    public static int[] SelectedIndices<T>(IEnumerable<T> playlist, IEnumerable<object> selected) where T : class
    {
      if (selected == null) return Array.Empty<int>();
      var remaining = new HashSet<object>(selected, ReferenceComparer.Instance);
      if (remaining.Count == 0 || remaining.Contains(null)) return Array.Empty<int>();
      var indices = new List<int>();
      int index = 0;
      foreach (var item in playlist)
      {
        if (remaining.Remove(item)) indices.Add(index);
        index++;
      }
      return remaining.Count == 0 ? indices.ToArray() : Array.Empty<int>();
    }

    // insertionIndex is a boundary in the ORIGINAL queue, including Count (append).
    public static int TargetIndex(IReadOnlyList<int> selectedIndices, int insertionIndex)
    {
      int low = 0, high = selectedIndices.Count;
      while (low < high)
      {
        int middle = low + (high - low) / 2;
        if (selectedIndices[middle] < insertionIndex) low = middle + 1;
        else high = middle;
      }
      return insertionIndex - low;
    }

    public static int[] CreateOrder(int count, IReadOnlyList<int> selectedIndices, int insertionIndex)
    {
      if (insertionIndex < 0 || insertionIndex > count || selectedIndices.Count == 0) return null;
      int previous = -1;
      foreach (var index in selectedIndices)
      {
        if (index <= previous || index >= count) return null;
        previous = index;
      }
      int target = TargetIndex(selectedIndices, insertionIndex);
      var order = new int[count];
      int selected = 0, output = 0, survivors = 0;
      for (int index = 0; index <= count; index++)
      {
        if (survivors == target)
        {
          foreach (var moved in selectedIndices) order[output++] = moved;
          target = -1;
        }
        if (index == count) break;
        if (selected < selectedIndices.Count && selectedIndices[selected] == index) selected++;
        else { order[output++] = index; survivors++; }
      }
      for (int index = 0; index < count; index++)
        if (order[index] != index) return order;
      return null;
    }

    private sealed class ReferenceComparer : IEqualityComparer<object>
    {
      internal static readonly ReferenceComparer Instance = new ReferenceComparer();
      public new bool Equals(object x, object y) => ReferenceEquals(x, y);
      public int GetHashCode(object value) => value == null ? 0 : RuntimeHelpers.GetHashCode(value);
    }
  }
}
