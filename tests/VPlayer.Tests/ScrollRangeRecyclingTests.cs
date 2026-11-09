using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VCore.WPF.ItemsCollections.VirtualList.VirtualLists;
using VPlayer.Core.ViewModels;
using VPlayer.WindowsPlayer.Behaviors;
using Xunit;

namespace VPlayer.Tests
{
  public class ScrollRangeRecyclingTests
  {
    private sealed class Track
    {
      private readonly int index;
      internal Track(int index) => this.index = index;
      public override string ToString() => "Track " + (index + 1);

    }

    [Fact]
    public void ClickScrollDownShiftClickScrollUpAndDragKeepsTheWholeRange() => Sta.Run(() =>
    {
      const int count = 100000;
      const int end = 50000;
      var items = Enumerable.Range(0, count).Select(index => new Track(index)).ToArray();
      var list = new PlaylistTrackListView
      {
        ItemsSource = new PlaylistSelectionView(new VirtualList<Track>(items)),
        SelectionMode = SelectionMode.Extended
      };
      var style = new Style(typeof(ListViewItem));
      style.Setters.Add(new Setter(FrameworkElement.HeightProperty, 24d));
      list.ItemContainerStyle = style;
      var scroll = new FrameworkElementFactory(typeof(ScrollViewer));
      scroll.SetValue(ScrollViewer.CanContentScrollProperty, true);
      scroll.AppendChild(new FrameworkElementFactory(typeof(ItemsPresenter)));
      list.Template = new ControlTemplate(typeof(PlaylistTrackListView)) { VisualTree = scroll };
      list.ItemsPanel = new ItemsPanelTemplate(new FrameworkElementFactory(typeof(VirtualizingStackPanel)));
      VirtualizingPanel.SetIsVirtualizing(list, true);
      VirtualizingPanel.SetVirtualizationMode(list, VirtualizationMode.Recycling);
      VirtualizingPanel.SetScrollUnit(list, ScrollUnit.Pixel);
      VirtualizingPanel.SetCacheLength(list, new VirtualizationCacheLength(0));
      var root = new AdornerDecorator { Child = list };
      root.Measure(new Size(640, 480));
      root.Arrange(new Rect(0, 0, 640, 480));
      root.UpdateLayout();

      var first = Row(1);
      var drag = new PlaylistDragSelection(list);
      Assert.True(drag.TryBegin(items[1], ModifierKeys.None, 1, out bool firstHandled));
      Assert.False(firstHandled);
      NativeClick("MakeSingleSelection", first);
      drag.CompleteClick();
      Assert.Same(items[1], Assert.Single(list.SelectedItems.Cast<object>()));
      list.ScrollIntoView(items[end]);
      root.UpdateLayout();
      Assert.Null(list.ItemContainerGenerator.ContainerFromIndex(1));
      Assert.Null(list.ItemContainerGenerator.ContainerFromIndex(2));
      Assert.Same(items[1], Assert.Single(list.SelectedItems.Cast<object>()));
      NativeClick("MakeAnchorSelection", Row(end), true);
      Assert.Equal(end, list.SelectedItems.Count);
      list.ScrollIntoView(items[0]);
      root.UpdateLayout();
      Assert.Null(list.ItemContainerGenerator.ContainerFromIndex(end));
      Assert.True(Row(1).IsSelected);

      Assert.True(drag.TryBegin(items[1], ModifierKeys.None, 1, out bool handled));
      Assert.True(handled);
      var selected = drag.ItemsForDrag(items[1]);
      var indices = PlaylistReorder.SelectedIndices(items, selected);
      Assert.Equal(Enumerable.Range(1, end), indices);
      drag.CommitDrag(selected);
      using var preview = new PlaylistDragPreview(list, Row(1), 1, Row(1).TranslatePoint(new Point(10, 12), list), indices);
      preview.Update(Row(0).TranslatePoint(new Point(10, 4), list));
      Assert.True(preview.IsVisible);
      Assert.Equal(0, preview.InsertionIndex);
      var order = PlaylistReorder.CreateOrder(count, indices, preview.InsertionIndex);
      Assert.Equal(Enumerable.Range(1, end), order.Take(end));
      Assert.Equal(0, order[end]);
      var output = Environment.GetEnvironmentVariable("VPLAYER_TEST_ARTIFACTS");
      if (!string.IsNullOrEmpty(output))
      {
        root.UpdateLayout();
        var bitmap = new RenderTargetBitmap(640, 480, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(root);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var image = File.Create(Path.Combine(output, "scroll-range-drag.png"));
        encoder.Save(image);
      }

      ListViewItem Row(int index) => Assert.IsType<ListViewItem>(list.ItemContainerGenerator.ContainerFromIndex(index));
      void NativeClick(string method, ListViewItem row, params object[] extra) =>
        typeof(ListBox).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)
          .Invoke(list, new object[] { row }.Concat(extra).ToArray());
    }, 60);
  }
}
