using System.Windows.Controls;
using System.Windows.Input;

namespace VPlayer.WindowsPlayer.Behaviors
{
  // WPF normally collapses an extended selection on mouse-down, before a drag can start.
  internal sealed class PlaylistDragSelection
  {
    private readonly PlaylistTrackListView list;
    private object pendingClick;
    public PlaylistDragSelection(PlaylistTrackListView list) => this.list = list;

    public bool Begin(object item, ModifierKeys modifiers)
    {
      pendingClick = null;
      if (modifiers != ModifierKeys.None || item == null || list.SelectedItems.Count < 2 ||
          !list.SelectedItems.Contains(item)) return false;
      pendingClick = item;
      list.Focus();
      return true;
    }

    public void Cancel() => pendingClick = null;

    public void CompleteClick()
    {
      var item = pendingClick;
      pendingClick = null;
      if (item == null || !list.Items.Contains(item)) return;
      list.CompletePlainClick(item);
    }
  }
}
