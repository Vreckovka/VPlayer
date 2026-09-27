using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using VCore.Standard.Modularity.Interfaces;
using VPlayer.Core.ViewModels;
using VPlayer.WindowsPlayer.Behaviors;

namespace VPlayer.Player.Views.WindowsPlayer
{
  public partial class WindowsPlayerView : UserControl, IView
  {
    private const string TrackFormat = "VPlayer.PlaylistTrack";
    private Point dragStart;
    private object dragItem;
    private object dragOwner;
    private PlaylistDragPreview dragPreview;

    public WindowsPlayerView()
    {
      InitializeComponent();
      Unloaded += (sender, args) => ClearDragPreview();
      DataContextChanged += (sender, args) => ClearDragPreview();
    }

    private static bool IsInteractive(DependencyObject source)
    {
      while (source != null && !(source is ListViewItem))
      {
        if (source is ButtonBase || source is TextBoxBase || source is Slider) return true;
        source = source is Visual ? VisualTreeHelper.GetParent(source) : LogicalTreeHelper.GetParent(source);
      }
      return false;
    }

    private ListViewItem RowAt(DependencyObject source) =>
      source == null ? null : ItemsControl.ContainerFromElement(ListView_Tracks, source) as ListViewItem;

    private void TrackMouseDown(object sender, MouseButtonEventArgs e)
    {
      dragItem = null;
      if (e.ClickCount != 1 || IsInteractive(e.OriginalSource as DependencyObject)) return;
      dragStart = e.GetPosition(ListView_Tracks);
      dragItem = RowAt(e.OriginalSource as DependencyObject)?.DataContext;
      dragOwner = DataContext;
    }

    private void TrackMouseMove(object sender, MouseEventArgs e)
    {
      if (e.LeftButton != MouseButtonState.Pressed) { dragItem = null; return; }
      if (dragItem == null || !ReferenceEquals(dragOwner, DataContext) ||
          !(DataContext is IReorderablePlaylist playlist) || !playlist.CanReorderPlaylist) return;
      var point = e.GetPosition(ListView_Tracks);
      if (Math.Abs(point.X - dragStart.X) < SystemParameters.MinimumHorizontalDragDistance &&
          Math.Abs(point.Y - dragStart.Y) < SystemParameters.MinimumVerticalDragDistance) return;
      int index = playlist.PlaylistItemIndex(dragItem);
      var row = ListView_Tracks.ItemContainerGenerator.ContainerFromIndex(index) as ListViewItem;
      if (row == null) return;
      var item = dragItem;
      try
      {
        dragPreview = new PlaylistDragPreview(ListView_Tracks, row, index, dragStart);
        dragPreview.Update(point);
        DragDrop.DoDragDrop(ListView_Tracks, new DataObject(TrackFormat, item), DragDropEffects.Move);
      }
      finally
      {
        ClearDragPreview();
        dragItem = null;
      }
      e.Handled = true;
    }

    private bool AcceptsDrag(DragEventArgs e) => dragPreview != null &&
      ReferenceEquals(dragOwner, DataContext) && DataContext is IReorderablePlaylist playlist &&
      playlist.CanReorderPlaylist && ReferenceEquals(e.Data.GetData(TrackFormat), dragItem) &&
      playlist.PlaylistItemIndex(dragItem) == dragPreview.SourceIndex;

    private void TrackDragOver(object sender, DragEventArgs e)
    {
      if (!e.Data.GetDataPresent(TrackFormat)) return;
      var accepted = AcceptsDrag(e);
      e.Effects = accepted ? DragDropEffects.Move : DragDropEffects.None;
      if (accepted) dragPreview.Update(e.GetPosition(ListView_Tracks));
      else dragPreview?.Hide();
      e.Handled = true;
    }

    private void TrackDragLeave(object sender, DragEventArgs e)
    {
      if (!e.Data.GetDataPresent(TrackFormat)) return;
      var point = e.GetPosition(ListView_Tracks);
      if (!new Rect(ListView_Tracks.RenderSize).Contains(point)) dragPreview?.Hide();
    }

    private void TrackGiveFeedback(object sender, GiveFeedbackEventArgs e)
    {
      if (dragPreview?.IsVisible != true) return;
      e.UseDefaultCursors = false;
      Mouse.SetCursor(Cursors.Arrow);
      e.Handled = true;
    }

    private async void TrackDrop(object sender, DragEventArgs e)
    {
      if (!e.Data.GetDataPresent(TrackFormat)) return;
      e.Handled = true;
      if (!AcceptsDrag(e)) return;
      var playlist = (IReorderablePlaylist)DataContext;
      var item = dragItem;
      dragPreview.Update(e.GetPosition(ListView_Tracks));
      int target = dragPreview.TargetIndex;
      ClearDragPreview();
      if (await playlist.MovePlaylistItemAsync(item, target) && ReferenceEquals(DataContext, playlist))
      {
        ListView_Tracks.SelectedItem = item;
        ListView_Tracks.ScrollIntoView(item);
      }
    }

    private void ClearDragPreview()
    {
      dragPreview?.Dispose();
      dragPreview = null;
    }
  }
}
