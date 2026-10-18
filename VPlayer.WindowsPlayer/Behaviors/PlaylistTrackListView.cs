using System.Collections;
using System.Windows.Controls;

namespace VPlayer.WindowsPlayer.Behaviors
{
  public sealed class PlaylistTrackListView : ListView
  {
    // One selection update, including queues with thousands of selected occurrences.
    public void RestoreSelection(IEnumerable items) => SetSelectedItems(items);
  }
}
