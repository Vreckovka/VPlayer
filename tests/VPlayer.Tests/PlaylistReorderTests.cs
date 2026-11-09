using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Logger;
using Moq;
using Prism.Events;
using VCore.WPF;
using VPlayer.Core.Managers.Status;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.ViewModels;
using VPlayer.Core.ViewModels.SoundItems;
using VPlayer.WindowsPlayer.Behaviors;
using VPlayer.WindowsPlayer.ViewModels;
using Xunit;

namespace VPlayer.Tests
{
  [Collection("UI synchronization")]
  public class PlaylistReorderTests
  {
    [Theory]
    [InlineData(new[] { 1, 3 }, 0, new[] { 1, 3, 0, 2, 4, 5 })]
    [InlineData(new[] { 1, 3 }, 6, new[] { 0, 2, 4, 5, 1, 3 })]
    [InlineData(new[] { 1, 3 }, 3, new[] { 0, 2, 1, 3, 4, 5 })]
    [InlineData(new[] { 2, 3 }, 1, new[] { 0, 2, 3, 1, 4, 5 })]
    [InlineData(new[] { 1, 2 }, 5, new[] { 0, 3, 4, 1, 2, 5 })]
    public void MovingSelectionPreservesRelativeOrder(int[] selected, int insertion, int[] expected)
      => Assert.Equal(expected, PlaylistReorder.CreateOrder(6, selected, insertion));

    [Theory]
    [InlineData(new[] { 1, 2 }, 1)]
    [InlineData(new[] { 1, 2 }, 2)]
    [InlineData(new[] { 1, 2 }, 3)]
    [InlineData(new[] { 0, 1, 2, 3, 4, 5 }, 4)]
    [InlineData(new int[] { }, 0)]
    [InlineData(new[] { 1 }, -1)]
    [InlineData(new[] { 1 }, 7)]
    [InlineData(new[] { -1 }, 2)]
    [InlineData(new[] { 6 }, 2)]
    [InlineData(new[] { 2, 1 }, 2)]
    public void UnchangedOrInvalidMovesAreRejected(int[] selected, int insertion)
      => Assert.Null(PlaylistReorder.CreateOrder(6, selected, insertion));

    private sealed class EqualItem
    {
      public override bool Equals(object other) => other is EqualItem;
      public override int GetHashCode() => 0;
    }

    [Fact]
    public void SelectionUsesOccurrenceIdentityAndPlaylistOrder()
    {
      var items = Enumerable.Range(0, 6).Select(_ => new EqualItem()).ToArray();
      Assert.Equal(new[] { 1, 4 }, PlaylistReorder.SelectedIndices(items, new[] { items[4], items[1], items[4] }));
      Assert.Empty(PlaylistReorder.SelectedIndices(items, new object[] { items[1], new EqualItem() }));
      Assert.Empty(PlaylistReorder.SelectedIndices(items, new object[] { null }));
    }

    [Fact]
    public void EveryBoundaryOfSmallQueuesMatchesRemoveThenInsert()
    {
      for (int mask = 1; mask < 256; mask++)
      {
        var selected = Enumerable.Range(0, 8).Where(i => (mask & (1 << i)) != 0).ToArray();
        for (int boundary = 0; boundary <= 8; boundary++)
        {
          var expected = Enumerable.Range(0, 8).Except(selected).ToList();
          expected.InsertRange(boundary - selected.Count(i => i < boundary), selected);
          var actual = PlaylistReorder.CreateOrder(8, selected, boundary) ?? Enumerable.Range(0, 8).ToArray();
          Assert.Equal(expected, actual);
        }
      }
    }

    private delegate bool Save(SoundItemFilePlaylist input, out SoundItemFilePlaylist output);
    private sealed class TestPlayer : MusicPlayerViewModel
    {
      public TestPlayer() : base(null, null, null, null, null, null, null, null, null, null, null, null, null, null, null) { }
      protected override void OnActualItemChanged() { }
      public override void SetItemAndPlay(int? itemIndex = null, bool forcePlay = false, bool onlyItemSet = false)
      {
        actualItemIndex = itemIndex.Value;
        ActualItem = PlayList[actualItemIndex];
      }
    }

    private sealed class Fixture : IDisposable
    {
      internal readonly TestPlayer Player = (TestPlayer)FormatterServices.GetUninitializedObject(typeof(TestPlayer));
      internal readonly SoundItemInPlaylistViewModel[] Items;
      internal readonly SoundItemFilePlaylist Saved;
      internal SoundItemFilePlaylist Written;
      internal int Saves;
      private readonly ReplaySubject<int> actual = new ReplaySubject<int>(1);
      private readonly SynchronizationContext previousUi;
      internal Fixture(int count, bool saveSucceeds, bool unavailable = true)
      {
        var storage = new Mock<IStorageManager>();
        SoundItemFilePlaylist ignored;
        storage.Setup(x => x.UpdatePlaylist<SoundItemFilePlaylist, PlaylistSoundItem, SoundItem>(It.IsAny<SoundItemFilePlaylist>(), out ignored))
          .Returns(new Save((SoundItemFilePlaylist input, out SoundItemFilePlaylist output) =>
          { Saves++; Written = input; output = input; return saveSucceeds; }));
        var events = new EventAggregator();
        Items = Enumerable.Range(0, count).Select(i => new SoundItemInPlaylistViewModel(
          new SoundItem { Id = i % 7 + 1, FileInfoEntity = new FileInfoEntity { Name = "Track " + i } }, events, storage.Object)).ToArray();
        var rows = Items.Select((item, i) => new PlaylistSoundItem
          { Id = i + 1, OrderInPlaylist = i + 1, ReferencedItem = item.Model, IdReferencedItem = item.Model.Id }).ToList();
        if (unavailable) rows.Insert(1, new PlaylistSoundItem { Id = count + 1, OrderInPlaylist = 1, IdReferencedItem = 999 });
        for (int i = 0; i < rows.Count; i++) rows[i].OrderInPlaylist = i + 1;
        Saved = new SoundItemFilePlaylist { Id = 42, Name = "Test", PlaylistItems = rows, ActualItem = rows.Last(), ActualItemId = count, LastItemIndex = count, HashCode = 123 };
        Set("playList", new PlaylistCollection<SoundItemInPlaylistViewModel>(Items));
        Set("actualSavedPlaylist", Saved);
        Set("actualItem", Items.Last());
        Set("actualItemIndex", count - 1);
        Set("actualItemSubject", actual);
        Set("playlistEditSemaphore", new SemaphoreSlim(1, 1));
        Set("playlistSemaphore", new SemaphoreSlim(1, 1));
        Set("storageManager", storage.Object);
        Set("logger", new Mock<ILogger>().Object);
        Set("statusManager", new Mock<IStatusManager>().Object);
        previousUi = VSynchronizationContext.UISynchronizationContext;
        VSynchronizationContext.UISynchronizationContext = new IgnorePosts();
      }
      private sealed class IgnorePosts : SynchronizationContext
      { public override void Post(SendOrPostCallback callback, object state) { } }
      private void Set(string name, object value)
      {
        bool found = false;
        for (var type = Player.GetType(); type != null; type = type.BaseType)
        {
          var field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
          if (field != null) { field.SetValue(Player, value); found = true; }
        }
        if (!found) throw new InvalidOperationException(name);
      }
      public void Dispose()
      {
        VSynchronizationContext.UISynchronizationContext = previousUi;
        Player.PlayList.Dispose();
        foreach (var item in Items) item.Dispose();
        actual.Dispose();
      }
    }

    [Theory]
    [InlineData(8, true)]
    [InlineData(100000, true)]
    [InlineData(100000, false)]
    public async Task GroupMovePersistsOnceKeepsDuplicatesPlaybackAndUnavailableRowsAndRollsBackFailure(int count, bool succeeds)
    {
      using var fixture = new Fixture(count, succeeds);
      var original = fixture.Player.PlayList;
      var originalRows = fixture.Saved.PlaylistItems.ToArray();
      var selected = fixture.Items.Where((_, i) => i % 2 == 1).Reverse().Cast<object>().ToArray();
      var expected = fixture.Items.Where((_, i) => i % 2 == 1).Concat(fixture.Items.Where((_, i) => i % 2 == 0)).ToArray();
      Assert.Equal(succeeds, await fixture.Player.MovePlaylistItemsAsync(selected, 0));
      Assert.Equal(1, fixture.Saves);
      Assert.Same(fixture.Items.Last(), fixture.Player.ActualItem);
      Assert.Equal(count, fixture.Written.ActualItemId);
      Assert.Equal(succeeds ? count / 2 - 1 : count - 1, fixture.Player.PlayList.IndexOf(fixture.Player.ActualItem));
      Assert.Equal(count / 2, fixture.Written.LastItemIndex);
      Assert.Equal(Enumerable.Range(1, count).Where(id => id % 2 == 0).Concat(Enumerable.Range(1, count).Where(id => id % 2 == 1)),
        fixture.Written.PlaylistItems.Where(row => row.ReferencedItem != null).Select(row => row.Id));
      Assert.Equal(count + 1, fixture.Written.PlaylistItems[1].Id);
      if (succeeds) Assert.Equal(expected, fixture.Player.PlayList);
      else
      {
        Assert.Same(original, fixture.Player.PlayList);
        Assert.Equal(originalRows, fixture.Saved.PlaylistItems);
        Assert.Equal(Enumerable.Range(1, count + 1), originalRows.Select(row => row.OrderInPlaylist));
        Assert.Equal(123, fixture.Saved.HashCode);
      }
      Assert.True(fixture.Player.CanReorderPlaylist);
    }

    [Fact]
    public async Task GroupMoveUsesDisplayedOrderEvenWhenTheStoredOrderDiffers()
    {
      using var fixture = new Fixture(7, true);
      using var original = fixture.Player.PlayList;
      using var reversed = new PlaylistCollection<SoundItemInPlaylistViewModel>(Enumerable.Reverse(fixture.Items));
      fixture.Player.PlayList = reversed;
      Assert.True(await fixture.Player.MovePlaylistItemsAsync(new[] { fixture.Items[2], fixture.Items[5] }, 7));
      Assert.Equal(new[] { 7, 5, 4, 2, 1, 6, 3 },
        fixture.Written.PlaylistItems.Where(row => row.ReferencedItem != null).Select(row => row.Id));
      Assert.Equal(1, fixture.Saves);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RemovingHalfOfHugeQueueSavesOnceAndKeepsOtherOccurrences(bool succeeds)
    {
      const int count = 100000;
      using var fixture = new Fixture(count, succeeds);
      var original = fixture.Player.PlayList;
      var selected = fixture.Items.Where((_, index) => index % 2 == 0).ToArray();
      var remaining = fixture.Items.Where((_, index) => index % 2 == 1).ToArray();
      Assert.Equal(succeeds, await fixture.Player.RemovePlaylistItemsAsync(selected));
      Assert.Equal(1, fixture.Saves);
      Assert.Same(fixture.Items.Last(), fixture.Player.ActualItem);
      Assert.Equal(count, fixture.Written.ActualItemId);
      Assert.Equal(Enumerable.Range(1, count).Where(id => id % 2 == 0),
        fixture.Written.PlaylistItems.Where(row => row.ReferencedItem != null).Select(row => row.Id));
      Assert.Contains(fixture.Written.PlaylistItems, row => row.Id == count + 1 && row.ReferencedItem == null);
      if (succeeds) Assert.Equal(remaining, fixture.Player.PlayList);
      else
      {
        Assert.Same(original, fixture.Player.PlayList);
        Assert.Equal(count + 1, fixture.Saved.PlaylistItems.Count);
        Assert.Equal(123, fixture.Saved.HashCode);
      }
    }

    [Fact]
    public async Task DeletingAllSelectedRowsPersistsAnEmptyPlaylistAndClearsTheCurrentTrack()
    {
      using var fixture = new Fixture(100000, true, unavailable: false);
      Assert.True(await fixture.Player.RemovePlaylistItemsAsync(fixture.Items));
      Assert.Empty(fixture.Player.PlayList);
      Assert.Empty(fixture.Written.PlaylistItems);
      Assert.Null(fixture.Player.ActualItem);
      Assert.Null(fixture.Written.ActualItemId);
      Assert.Equal(1, fixture.Saves);
    }

    [Fact]
    public async Task RemovingTheCurrentTrackSelectsTheClosestRemainingTrack()
    {
      using var fixture = new Fixture(8, true);
      Assert.True(await fixture.Player.RemovePlaylistItemsAsync(new[] { fixture.Items[7], fixture.Items[6] }));
      Assert.Same(fixture.Items[5], fixture.Player.ActualItem);
      Assert.Equal(6, fixture.Written.ActualItemId);
    }

    [Fact]
    public async Task UnsavedQueueCanBeMovedAndEditedWithoutWritingStorage()
    {
      using var fixture = new Fixture(8, true);
      fixture.Saved.Id = -1;
      Assert.True(await fixture.Player.MovePlaylistItemsAsync(new[] { fixture.Items[0], fixture.Items[2] }, 8));
      Assert.Equal(new[] { fixture.Items[0], fixture.Items[2] }, fixture.Player.PlayList.Skip(6));
      Assert.True(await fixture.Player.RemovePlaylistItemsAsync(new[] { fixture.Items[0], fixture.Items[2] }));
      Assert.Equal(6, fixture.Player.PlayList.Count);
      Assert.Equal(0, fixture.Saves);
    }

    [Fact]
    public void DeferredDragKeepsItsSnapshotWhenWpfSelectionChanges() => Sta.Run(() =>
    {
      var items = Enumerable.Range(0, 100000).Select(_ => new object()).ToArray();
      var list = new PlaylistTrackListView { ItemsSource = new PlaylistSelectionView(items), SelectionMode = SelectionMode.Extended };
      list.RestoreSelection(new[] { items[1], items[99999] });
      var drag = new PlaylistDragSelection(list);
      Assert.True(drag.Begin(items[99999], ModifierKeys.None));
      list.RestoreSelection(new[] { items[1] });
      Assert.Equal(new[] { items[1], items[99999] }, drag.ItemsForDrag(items[99999]));
      drag.Cancel();
      Assert.Empty(drag.ItemsForDrag(items[99999]));
    });

    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    [InlineData(50000)]
    public void RemoveCommandTargetsTheSelectionOrAnUnselectedContextRow(int contextIndex) => Sta.Run(() =>
    {
      var items = Enumerable.Range(0, 100000).Select(_ => new object()).ToArray();
      var editor = new Mock<IReorderablePlaylist>();
      editor.SetupGet(x => x.CanRemovePlaylistItems).Returns(true);
      object[] removed = null;
      editor.Setup(x => x.RemovePlaylistItemsAsync(It.IsAny<IEnumerable<object>>()))
        .Callback((IEnumerable<object> rows) => removed = rows.ToArray()).ReturnsAsync(true);
      var list = new PlaylistTrackListView { ItemsSource = new PlaylistSelectionView(items), SelectionMode = SelectionMode.Extended, DataContext = editor.Object };
      list.RestoreSelection(new[] { items[1], items[99999] });
      object row = contextIndex < 0 ? null : items[contextIndex];
      Assert.True(PlaylistTrackListView.RemovePlaylistSelection.CanExecute(row, list));
      PlaylistTrackListView.RemovePlaylistSelection.Execute(row, list);
      Assert.Equal(contextIndex == 50000 ? new[] { row } : new[] { items[1], items[99999] }, removed);
    });

    [Fact]
    public async Task NoOpAndStaleSelectionNeverSave()
    {
      using var fixture = new Fixture(100000, true);
      Assert.False(await fixture.Player.MovePlaylistItemsAsync(fixture.Items, 50000));
      Assert.False(await fixture.Player.MovePlaylistItemsAsync(new object[] { fixture.Items[0], new object() }, 0));
      Assert.Equal(0, fixture.Saves);
    }

    [Fact]
    public async Task SingleItemMoveRetainsFinalIndexContract()
    {
      using var fixture = new Fixture(100000, true);
      Assert.True(await fixture.Player.MovePlaylistItemAsync(fixture.Items[0], 99999));
      Assert.Same(fixture.Items[0], fixture.Player.PlayList.Last());
      Assert.Equal(1, fixture.Saves);
    }

    [Fact]
    public void MouseDownKeepsSelectionDragCancelKeepsItAndPlainClickStillSelectsOne() => Sta.Run(() =>
    {
      var items = Enumerable.Range(0, 100000).Select(_ => new object()).ToArray();
      var list = new PlaylistTrackListView { ItemsSource = items, SelectionMode = SelectionMode.Extended };
      list.SelectedItems.Add(items[1]); list.SelectedItems.Add(items[99999]);
      var selection = new PlaylistDragSelection(list);
      Assert.True(selection.Begin(items[99999], ModifierKeys.None));
      Assert.Equal(2, list.SelectedItems.Count);
      selection.Cancel(); selection.CompleteClick();
      Assert.Equal(2, list.SelectedItems.Count);
      Assert.False(selection.Begin(items[99999], ModifierKeys.Control));
      Assert.False(selection.Begin(items[99999], ModifierKeys.Shift));
      Assert.False(selection.Begin(items[2], ModifierKeys.None));
      Assert.True(selection.Begin(items[1], ModifierKeys.None));
      selection.CompleteClick();
      Assert.Same(items[1], Assert.Single(list.SelectedItems.Cast<object>()));
    });

    [Fact]
    public void BulkSelectionRestoresEveryOccurrenceAfterQueueReload() => Sta.Run(() =>
    {
      var items = Enumerable.Range(0, 100000).Select(_ => new object()).ToArray();
      var selected = items.Where((_, index) => index % 2 == 0).ToArray();
      var list = new PlaylistTrackListView { ItemsSource = new PlaylistSelectionView(items), SelectionMode = SelectionMode.Extended };
      list.RestoreSelection(selected);
      list.ItemsSource = new PlaylistSelectionView(Enumerable.Reverse(items).ToArray());
      list.RestoreSelection(selected);
      Assert.Equal(50000, list.SelectedItems.Count);
      Assert.Equal(selected, list.SelectedItems.Cast<object>());
    }, 30);

    [Fact]
    public void PreviewShowsGroupInsertionAndRestoresRecycledContainersOnDispose() => Sta.Run(() =>
    {
      var items = Enumerable.Range(1, 12).Select(index => (object)("Track " + index)).ToArray();
      var list = new ListView { ItemsSource = items, SelectionMode = SelectionMode.Extended };
      foreach (var index in new[] { 1, 3, 10 }) list.SelectedItems.Add(items[index]);
      var root = new AdornerDecorator { Child = list };
      root.Measure(new Size(640, 480)); root.Arrange(new Rect(0, 0, 640, 480)); root.UpdateLayout();
      var source = (ListViewItem)list.ItemContainerGenerator.ContainerFromIndex(1);
      Assert.NotNull(source);
      var last = (ListViewItem)list.ItemContainerGenerator.ContainerFromIndex(11);
      var preview = new PlaylistDragPreview(list, source, 1, new Point(10, 10), new[] { 1, 3, 10 });
      var selectedTarget = (ListViewItem)list.ItemContainerGenerator.ContainerFromIndex(3);
      preview.Update(selectedTarget.TranslatePoint(new Point(10, selectedTarget.ActualHeight / 4), list));
      Assert.Equal(3, preview.InsertionIndex);
      preview.Update(last.TranslatePoint(new Point(10, last.ActualHeight), list));
      Assert.True(preview.IsVisible);
      Assert.Equal(12, preview.InsertionIndex);
      Assert.Equal(0.35, source.Opacity);
      root.UpdateLayout();
      var bitmap = new RenderTargetBitmap(640, 480, 96, 96, PixelFormats.Pbgra32);
      bitmap.Render(root);
      var encoder = new PngBitmapEncoder();
      encoder.Frames.Add(BitmapFrame.Create(bitmap));
      using (var stream = File.Create(Path.Combine(AppContext.BaseDirectory, "multi-drag-preview.png"))) encoder.Save(stream);
      preview.Hide(); Assert.Equal(1, source.Opacity);
      preview.Update(new Point(10, 20));
      preview.Dispose(); preview.Dispose();
      Assert.Equal(1, source.Opacity);
      Assert.False(preview.IsVisible);
      Assert.Empty(AdornerLayer.GetAdornerLayer(list).GetAdorners(list) ?? Array.Empty<Adorner>());
    });
  }
}
