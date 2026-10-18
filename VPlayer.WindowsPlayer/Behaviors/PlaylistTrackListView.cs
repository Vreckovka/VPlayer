using System.Collections;
using System.Windows.Controls;

namespace VPlayer.WindowsPlayer.Behaviors
{
  public sealed class PlaylistTrackListView : ListView
  {
    // One selection update, including queues with thousands of selected occurrences.
    public void RestoreSelection(IEnumerable items, object anchorItem = null)
    {
      GetBindingExpression(ItemsSourceProperty)?.UpdateTarget();
      SetSelectedItems(items);
      SetRangeAnchor(anchorItem, false);
    }

    internal void CompletePlainClick(object item)
    {
      SetSelectedItems(new[] { item });
      SetRangeAnchor(item, true);
    }

    private void SetRangeAnchor(object item, bool focus)
    {
      if (item == null || !Items.Contains(item)) return;
      ScrollIntoView(item);
      UpdateLayout();
      if (ItemContainerGenerator.ContainerFromItem(item) is ListBoxItem row)
      {
        if (focus) row.Focus();
        // SelectedItem alone does not update WPF's pivot for Shift-click/keyboard ranges.
        AnchorItem = item;
      }
    }
  }
}
