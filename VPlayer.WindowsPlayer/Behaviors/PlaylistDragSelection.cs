using System;
using System.Linq;
using System.Windows.Input;

namespace VPlayer.WindowsPlayer.Behaviors
{
  // WPF normally collapses an extended selection on mouse-down, before a drag can start.
  internal sealed class PlaylistDragSelection
  {
    private readonly PlaylistTrackListView list;
    private object pressedItem;
    private bool preserveOnRelease;
    private object[] pendingItems;
    public PlaylistDragSelection(PlaylistTrackListView list) => this.list = list;

    public bool TryBegin(object item, ModifierKeys modifiers, int clickCount, out bool handled)
    {
      Cancel();
      handled = false;
      if (item == null) return false;
      if ((modifiers == ModifierKeys.None || modifiers == ModifierKeys.Shift) &&
          list.SelectedItems.Count > 1 && list.SelectedItems.Contains(item))
      {
        pressedItem = item;
        pendingItems = list.SelectedItems.Cast<object>().ToArray();
        preserveOnRelease = modifiers == ModifierKeys.None;
        // Take precedence over double-click playback when a selected group can be dragged.
        handled = preserveOnRelease;
        if (handled) list.Focus();
        return true;
      }
      // Let native Shift-click establish a new range before the mouse starts moving.
      return clickCount == 1 || (modifiers & ModifierKeys.Shift) != 0;
    }

    public object[] ItemsForDrag(object item) =>
      ReferenceEquals(item, pressedItem) && pendingItems != null ? pendingItems :
      list.SelectedItems.Contains(item) ? list.SelectedItems.Cast<object>().ToArray() : Array.Empty<object>();

    public void Cancel()
    {
      pressedItem = null;
      pendingItems = null;
      preserveOnRelease = false;
    }

    public void CommitDrag(object[] items)
    {
      // A native Shift press can shorten the visible range before crossing the drag threshold.
      if (!list.SelectedItems.Cast<object>().SequenceEqual(items)) list.KeepDragSelection(items);
      Cancel();
    }

    public void CompleteClick()
    {
      var item = preserveOnRelease ? pressedItem : null;
      Cancel();
      if (item != null && list.SelectedItems.Contains(item)) list.SetRangeAnchor(item);
    }
  }
}
