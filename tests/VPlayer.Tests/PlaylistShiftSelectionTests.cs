using System;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using VPlayer.WindowsPlayer.Behaviors;
using Xunit;

namespace VPlayer.Tests
{
  public class PlaylistShiftSelectionTests
  {
    private sealed class Fixture
    {
      internal readonly object[] Items = Enumerable.Range(0, 100000).Select(_ => new object()).ToArray();
      internal readonly PlaylistTrackListView List;
      internal readonly PlaylistDragSelection Drag;
      private readonly AdornerDecorator root;
      internal Fixture()
      {
        List = new PlaylistTrackListView { ItemsSource = new PlaylistSelectionView(Items), SelectionMode = SelectionMode.Extended };
        root = new AdornerDecorator { Child = List };
        root.Measure(new Size(640, 480));
        root.Arrange(new Rect(0, 0, 640, 480));
        root.UpdateLayout();
        Drag = new PlaylistDragSelection(List);
      }
      internal void NativeClick(int index, string method, params object[] extra)
      {
        List.ScrollIntoView(Items[index]);
        root.UpdateLayout();
        var row = (ListBoxItem)List.ItemContainerGenerator.ContainerFromIndex(List.Items.IndexOf(Items[index]));
        Assert.True(row != null, "Missing realized row " + index);
        // Invoke WPF's actual click algorithms with deterministic modifiers. No global input injection.
        var handler = typeof(ListBox).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.True(handler != null, "Missing native WPF handler " + method);
        handler.Invoke(List, new object[] { row }.Concat(extra).ToArray());
      }
      internal void Single(int index) => NativeClick(index, "MakeSingleSelection");
      internal void Toggle(int index) => NativeClick(index, "MakeToggleSelection");
      internal void Shift(int index, bool clear = true) => NativeClick(index, "MakeAnchorSelection", clear);
      internal void Expect(params int[] indices) => Assert.Equal(indices.OrderBy(i => i), List.SelectedItems.Cast<object>().Select(item => Array.IndexOf(Items, item)).OrderBy(i => i));
    }

    [Theory]
    [InlineData(1, 9, 6)]
    [InlineData(9, 1, 4)]
    public void ShiftClickAfterDeferredPlainClickUsesTheClickedRowAsAnchor(int clicked, int oldAnchor, int target) => Sta.Run(() =>
    {
      var fixture = new Fixture();
      fixture.Single(clicked);
      fixture.Toggle(oldAnchor);
      Assert.True(fixture.Drag.TryBegin(fixture.Items[clicked], ModifierKeys.None, 1, out bool handled));
      Assert.True(handled);
      fixture.Drag.CompleteClick();
      fixture.Shift(target);
      fixture.Expect(Enumerable.Range(Math.Min(clicked, target), Math.Abs(clicked - target) + 1).ToArray());
      fixture.Shift(clicked);
      fixture.Expect(clicked);
    });

    [Fact]
    public void ShiftClickAfterReorderingUsesTheDraggedOccurrenceAtItsNewPosition() => Sta.Run(() =>
    {
      var fixture = new Fixture();
      fixture.Single(1);
      fixture.Toggle(9);
      fixture.List.ItemsSource = new PlaylistSelectionView(Enumerable.Reverse(fixture.Items).ToArray());
      fixture.List.RestoreSelection(new[] { fixture.Items[1], fixture.Items[9] }, fixture.Items[9]);
      fixture.Shift(6);
      fixture.Expect(6, 7, 8, 9);
    });

    [Fact]
    public void ClickingAnAlreadySelectedRowKeepsTheShiftRangeAvailableForDrag() => Sta.Run(() =>
    {
      var fixture = new Fixture();
      fixture.Single(1);
      fixture.Shift(9);
      Assert.True(fixture.Drag.TryBegin(fixture.Items[9], ModifierKeys.None, 1, out bool handled));
      Assert.True(handled);
      fixture.Drag.CompleteClick();
      fixture.Expect(1, 2, 3, 4, 5, 6, 7, 8, 9);
      Assert.Equal(9, fixture.Drag.ItemsForDrag(fixture.Items[9]).Length);
    });

    [Theory]
    [InlineData(ModifierKeys.None, 2)]
    [InlineData(ModifierKeys.Shift, 1)]
    public void PressingInsideAShiftRangeDragsTheWholeRange(ModifierKeys modifiers, int clickCount) => Sta.Run(() =>
    {
      var fixture = new Fixture();
      fixture.Single(1);
      fixture.Shift(9);
      var expected = fixture.List.SelectedItems.Cast<object>().ToArray();
      Assert.True(fixture.Drag.TryBegin(fixture.Items[5], modifiers, clickCount, out bool handled));
      Assert.Equal(modifiers == ModifierKeys.None, handled);
      if (!handled) fixture.Shift(5);
      var dragged = fixture.Drag.ItemsForDrag(fixture.Items[5]);
      Assert.Equal(expected, dragged);
      fixture.Drag.CommitDrag(dragged);
      fixture.Drag.CompleteClick();
      fixture.Expect(1, 2, 3, 4, 5, 6, 7, 8, 9);
    });

    [Fact]
    public void ShiftPressCanSelectAndDragFiftyThousandRowsInOneGesture() => Sta.Run(() =>
    {
      var fixture = new Fixture();
      fixture.Single(1);
      Assert.True(fixture.Drag.TryBegin(fixture.Items[50000], ModifierKeys.Shift, 1, out bool handled));
      Assert.False(handled);
      fixture.Shift(50000);
      var dragged = fixture.Drag.ItemsForDrag(fixture.Items[50000]);
      Assert.Equal(fixture.Items.Skip(1).Take(50000), dragged);
      fixture.Drag.CommitDrag(dragged);
      Assert.Equal(50000, fixture.List.SelectedItems.Count);
    }, 60);

    [Fact]
    public void CancellingDragPreservesTheExistingNativeAnchor() => Sta.Run(() =>
    {
      var fixture = new Fixture();
      fixture.Single(1);
      fixture.Toggle(9);
      Assert.True(fixture.Drag.TryBegin(fixture.Items[1], ModifierKeys.None, 1, out bool handled));
      Assert.True(handled);
      fixture.Drag.Cancel();
      fixture.Drag.CompleteClick();
      fixture.Shift(6);
      fixture.Expect(6, 7, 8, 9);
    });

    [Fact]
    public void OrdinaryShiftAndControlShiftStillUseTheNativeAnchor() => Sta.Run(() =>
    {
      var fixture = new Fixture();
      fixture.Single(3);
      fixture.Shift(7);
      fixture.Expect(3, 4, 5, 6, 7);
      fixture.Shift(1);
      fixture.Expect(1, 2, 3);
      fixture.Toggle(10);
      fixture.Shift(13, false);
      fixture.Expect(1, 2, 3, 10, 11, 12, 13);
    });
  }
}
