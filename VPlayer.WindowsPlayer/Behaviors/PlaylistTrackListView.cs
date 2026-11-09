using System.Collections;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using VPlayer.Core.ViewModels;

namespace VPlayer.WindowsPlayer.Behaviors
{
  public sealed class PlaylistTrackListView : ListView
  {
    public static readonly RoutedUICommand RemovePlaylistSelection = new RoutedUICommand(
      "Remove from playlist", nameof(RemovePlaylistSelection), typeof(PlaylistTrackListView));

    public PlaylistTrackListView()
    {
      CommandBindings.Add(new CommandBinding(RemovePlaylistSelection, RemoveSelection, CanRemoveSelection));
    }

    private void CanRemoveSelection(object sender, CanExecuteRoutedEventArgs e)
    {
      e.CanExecute = DataContext is IReorderablePlaylist playlist && playlist.CanRemovePlaylistItems &&
        (e.Parameter == null ? SelectedItems.Count > 0 : Items.Contains(e.Parameter));
      e.Handled = true;
    }

    private async void RemoveSelection(object sender, ExecutedRoutedEventArgs e)
    {
      e.Handled = true;
      if (DataContext is IReorderablePlaylist playlist && playlist.CanRemovePlaylistItems)
        await playlist.RemovePlaylistItemsAsync(RemovalTargets(e.Parameter));
    }

    internal object[] RemovalTargets(object row) => row != null && !SelectedItems.Contains(row)
      ? new[] { row } : SelectedItems.Cast<object>().ToArray();

    protected override void OnKeyDown(KeyEventArgs e)
    {
      if (!e.Handled && e.Key == Key.Delete && Keyboard.Modifiers == ModifierKeys.None &&
          !(e.OriginalSource is TextBoxBase) && RemovePlaylistSelection.CanExecute(null, this))
      {
        RemovePlaylistSelection.Execute(null, this);
        e.Handled = true;
        return;
      }
      base.OnKeyDown(e);
    }

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
