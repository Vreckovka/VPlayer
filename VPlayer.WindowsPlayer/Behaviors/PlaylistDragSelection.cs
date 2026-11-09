using System;
using System.Linq;
using System.Windows.Input;

namespace VPlayer.WindowsPlayer.Behaviors
{
  // WPF normally collapses an extended selection on mouse-down, before a drag can start.
  internal sealed class PlaylistDragSelection
  {
    private readonly PlaylistTrackListView list;
    private object pendingClick;
    private object[] pendingItems;
    public PlaylistDragSelection(PlaylistTrackListView list) => this.list = list;

    public bool Begin(object item, ModifierKeys modifiers)
    {
      Cancel();
      if (modifiers != ModifierKeys.None || item == null || list.SelectedItems.Count < 2 ||
          !list.SelectedItems.Contains(item)) return false;
      pendingClick = item;
      pendingItems = list.SelectedItems.Cast<object>().ToArray();
      list.Focus();
      return true;
    }

    public object[] ItemsForDrag(object item) =>
      ReferenceEquals(item, pendingClick) && pendingItems != null ? pendingItems :
      list.SelectedItems.Contains(item) ? list.SelectedItems.Cast<object>().ToArray() : Array.Empty<object>();

    public void Cancel()
    {
      pendingClick = null;
      pendingItems = null;
    }

    public void CompleteClick()
    {
      var item = pendingClick;
      Cancel();
      if (item == null || !list.Items.Contains(item)) return;
      list.CompletePlainClick(item);
    }
  }
}
