using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Data;
using System.Windows.Media;
using VPlayer.Home.Views;
using Xunit;

namespace VPlayer.Tests
{
  public class GroupedPlaylistSelectionTests
  {
    private sealed class Fixture : IDisposable
    {
      public readonly object[] Items = Enumerable.Range(0, 10000).Select(index => (object)new Item(index)).ToArray();
      public readonly ListView Owner;
      public readonly GroupedPlaylistListView First;
      public readonly GroupedPlaylistListView Second;
      public Fixture()
      {
        Owner = new ListView {ItemsSource = Items};
        First = new GroupedPlaylistListView {ItemsSource = Items.Take(5000).ToArray(), GroupOwner = Owner};
        Second = new GroupedPlaylistListView {ItemsSource = Items.Skip(5000).ToArray(), GroupOwner = Owner};
      }
      public void Dispose()
      {
        First.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent));
        Second.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent));
      }
    }
    private sealed class TestPresentationSource : PresentationSource
    {
      public override Visual RootVisual {get;set;}
      public override bool IsDisposed => false;
      protected override CompositionTarget GetCompositionTargetCore() => null;
    }
    public sealed class KeyboardList : GroupedPlaylistListView
    {
      public bool Press(Key key)
      {
        var args = new KeyEventArgs(Keyboard.PrimaryDevice, new TestPresentationSource(), 0, key) {RoutedEvent = Keyboard.PreviewKeyDownEvent};
        OnPreviewKeyDown(args);
        return args.Handled;
      }
    }
    private static T Find<T>(DependencyObject root, int ordinal = 0) where T : DependencyObject
    {
      int seen = 0;
      return Visit(root);
      T Visit(DependencyObject node)
      {
        if (node is T item && seen++ == ordinal) return item;
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++)
        {
          var found = Visit(VisualTreeHelper.GetChild(node, i));
          if (found != null) return found;
        }
        return null;
      }
    }

    [Fact]
    public void KeyboardCrossesGroupBoundariesAndHomeEndUseTheWholeCollection() => Sta.Run(() =>
    {
      using var fixture = new Fixture();
      var panel = new FrameworkElementFactory(typeof(StackPanel));
      foreach (var items in new[] {fixture.Items.Take(5000).ToArray(), fixture.Items.Skip(5000).ToArray()})
      {
        var group = new FrameworkElementFactory(typeof(KeyboardList));
        group.SetValue(ItemsControl.ItemsSourceProperty, items);
        group.SetBinding(GroupedPlaylistListView.GroupOwnerProperty, new Binding {RelativeSource = RelativeSource.TemplatedParent});
        group.SetValue(FrameworkElement.HeightProperty, 180d);
        panel.AppendChild(group);
      }
      fixture.Owner.Template = new ControlTemplate(typeof(ListView)) {VisualTree = panel};
      fixture.Owner.Measure(new Size(900, 360));
      fixture.Owner.Arrange(new Rect(0, 0, 900, 360));
      fixture.Owner.UpdateLayout();
      var first = Find<KeyboardList>(fixture.Owner);
      var second = Find<KeyboardList>(fixture.Owner, 1);
      Assert.NotNull(first);
      Assert.NotNull(second);
      first.SelectedItem = fixture.Items[4999];
      Assert.True(first.Press(Key.Down));
      Assert.Same(fixture.Items[5000], fixture.Owner.SelectedItem);
      Assert.Null(first.SelectedItem);
      Assert.Same(fixture.Items[5000], second.SelectedItem);
      Assert.True(second.Press(Key.Up));
      Assert.Same(fixture.Items[4999], fixture.Owner.SelectedItem);
      Assert.True(first.Press(Key.Home));
      Assert.Same(fixture.Items[0], fixture.Owner.SelectedItem);
      Assert.True(first.Press(Key.End));
      Assert.Same(fixture.Items[9999], fixture.Owner.SelectedItem);
      Assert.Null(first.SelectedItem);
      Assert.Same(fixture.Items[9999], second.SelectedItem);
      first.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent));
      second.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent));
    });

    private sealed class Item
    {
      public Item(int id) => Id = id;
      public int Id {get;}
    }

    [Fact]
    public void SelectingAnotherGroupKeepsOneSelectionAcrossTenThousandItems() => Sta.Run(() =>
    {
      using var fixture = new Fixture();
      fixture.First.SelectedItem = fixture.Items[4999];
      Assert.Same(fixture.Items[4999], fixture.Owner.SelectedItem);
      fixture.Second.SelectedItem = fixture.Items[9999];
      Assert.Same(fixture.Items[9999], fixture.Owner.SelectedItem);
      Assert.Null(fixture.First.SelectedItem);
      Assert.Same(fixture.Items[9999], fixture.Second.SelectedItem);
    });

    [Fact]
    public void ExternalOwnerSelectionAndClearUpdateBothGroups() => Sta.Run(() =>
    {
      using var fixture = new Fixture();
      fixture.Owner.SelectedItem = fixture.Items[9999];
      Assert.Same(fixture.Items[9999], fixture.Second.SelectedItem);
      Assert.Null(fixture.First.SelectedItem);
      fixture.Owner.SelectedItem = fixture.Items[4999];
      Assert.Same(fixture.Items[4999], fixture.First.SelectedItem);
      Assert.Null(fixture.Second.SelectedItem);
      fixture.Owner.SelectedItem = null;
      Assert.Null(fixture.First.SelectedItem);
      Assert.Null(fixture.Second.SelectedItem);
    });

    [Fact]
    public void ClearingTheSelectedGroupAlsoClearsTheOuterSelection() => Sta.Run(() =>
    {
      using var fixture = new Fixture();
      fixture.Second.SelectedItem = fixture.Items[9999];
      fixture.Second.SelectedItem = null;
      Assert.Null(fixture.Owner.SelectedItem);
      Assert.Null(fixture.First.SelectedItem);
    });

    [Fact]
    public void UnloadedGroupStopsReceivingAndPublishingSelectionAndReloadResynchronizes() => Sta.Run(() =>
    {
      using var fixture = new Fixture();
      fixture.First.SelectedItem = fixture.Items[4999];
      fixture.First.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent));
      fixture.Owner.SelectedItem = fixture.Items[9999];
      Assert.Same(fixture.Items[4999], fixture.First.SelectedItem);
      fixture.First.SelectedItem = fixture.Items[4998];
      Assert.Same(fixture.Items[9999], fixture.Owner.SelectedItem);
      fixture.First.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
      Assert.Null(fixture.First.SelectedItem);
      fixture.First.SelectedItem = fixture.Items[4997];
      Assert.Same(fixture.Items[4997], fixture.Owner.SelectedItem);
      Assert.Null(fixture.Second.SelectedItem);
    });

    [Fact]
    public void ChangingOwnerDetachesTheOldList() => Sta.Run(() =>
    {
      using var fixture = new Fixture();
      var replacement = new ListView {ItemsSource = fixture.Items};
      replacement.SelectedItem = fixture.Items[1];
      fixture.First.GroupOwner = replacement;
      Assert.Same(fixture.Items[1], fixture.First.SelectedItem);
      fixture.Owner.SelectedItem = fixture.Items[2];
      Assert.Same(fixture.Items[1], fixture.First.SelectedItem);
      fixture.First.SelectedItem = fixture.Items[3];
      Assert.Same(fixture.Items[3], replacement.SelectedItem);
      Assert.Same(fixture.Items[2], fixture.Owner.SelectedItem);
    });
  }
}
