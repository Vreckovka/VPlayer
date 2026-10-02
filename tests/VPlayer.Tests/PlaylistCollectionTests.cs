using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Data;
using VPlayer.Core.ViewModels;
using Xunit;

namespace VPlayer.Tests
{
  public class PlaylistCollectionTests
  {
    private sealed class Item : INotifyPropertyChanged
    {
      internal int Id;
      public event PropertyChangedEventHandler PropertyChanged;
      internal void Change() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Title"));
    }

    [Fact]
    public void HugeAppendKeepsOccurrencesAndRxEventsButPublishesOneWpfReset()
    {
      using var items = new PlaylistCollection<Item>();
      var view = new ListCollectionView(items);
      var incoming = Enumerable.Range(0, 100000).Select(i => new Item { Id = i % 97 }).ToArray();
      var added = new List<Item>();
      var properties = new List<string>();
      int changes = 0, clears = 0, updates = 0, removes = 0;
      using var addSubscription = items.ItemAdded.Subscribe(x => added.Add(x.EventArgs));
      using var clearSubscription = items.Cleared.Subscribe(_ => clears++);
      using var updateSubscription = items.ItemUpdated.Subscribe(_ => updates++);
      using var removeSubscription = items.ItemRemoved.Subscribe(_ => removes++);
      items.CollectionChanged += (_, args) => { Assert.Equal(NotifyCollectionChangedAction.Reset, args.Action); changes++; };
      ((INotifyPropertyChanged)items).PropertyChanged += (_, args) => properties.Add(args.PropertyName);

      items.AddPlaylistRange(incoming);
      Assert.Equal(1, changes);
      Assert.Equal(0, clears);
      Assert.Equal(1, properties.Count(x => x == "Count"));
      Assert.Equal(1, properties.Count(x => x == "Item[]"));
      Assert.Equal(incoming.Length, view.Count);
      Assert.Equal(incoming.Length, items.View.Count);
      Assert.Equal(incoming.Length, added.Count);
      for (int i = 0; i < incoming.Length; i++)
      {
        Assert.Same(incoming[i], items[i]);
        Assert.Same(incoming[i], added[i]);
        Assert.Same(incoming[i], view.GetItemAt(i));
      }
      incoming[99999].Change();
      Assert.Equal(1, updates);
      Assert.Equal(0, removes);
    }

    [Fact]
    public void OrdinaryEditsAfterBatchStillTrackItemsAndEmptyBatchDoesNotClear()
    {
      using var items = new PlaylistCollection<Item>();
      var initial = Enumerable.Range(0, 100000).Select(i => new Item { Id = i }).ToArray();
      items.AddPlaylistRange(initial);
      int added = 0, removed = 0, cleared = 0, changed = 0;
      using var a = items.ItemAdded.Subscribe(_ => added++);
      added = 0; // ItemAdded replays the last item.
      using var r = items.ItemRemoved.Subscribe(_ => removed++);
      using var c = items.Cleared.Subscribe(_ => cleared++);
      items.CollectionChanged += (_, __) => changed++;
      items.AddPlaylistRange(Array.Empty<Item>());
      Assert.Equal(0, changed);
      var extra = new Item { Id = -1 };
      items.Add(extra);
      items.Remove(extra);
      Assert.Equal(1, added);
      Assert.Equal(1, removed);
      Assert.Equal(100000, items.View.Count);
      items.Clear();
      Assert.Equal(1, cleared);
      Assert.Empty(items);
      Assert.Empty(items.View);
      items.AddPlaylistRange(initial);
      Assert.Equal(100000, items.View.Count);
    }

    [Fact]
    public void ObserverFailureLeavesFullBatchAndRestoresSingleEditNotifications()
    {
      using var items = new PlaylistCollection<Item>();
      var incoming = Enumerable.Range(0, 100000).Select(i => new Item { Id = i }).ToArray();
      NotifyCollectionChangedEventHandler fail = (_, __) => throw new InvalidOperationException("observer failure");
      items.CollectionChanged += fail;
      Assert.Throws<InvalidOperationException>(() => items.AddPlaylistRange(incoming));
      items.CollectionChanged -= fail;
      Assert.Equal(incoming.Length, items.Count);
      int removed = 0;
      using var r = items.ItemRemoved.Subscribe(_ => removed++);
      items.Remove(incoming[99999]);
      Assert.Equal(1, removed);
      Assert.Equal(99999, items.View.Count);
    }

    [Fact]
    public void EnumerationFailureDoesNotMutateAndSelfAppendKeepsRepeatedReferences()
    {
      using var items = new PlaylistCollection<Item>();
      var incoming = Enumerable.Range(0, 100000).Select(i => new Item { Id = i }).ToArray();
      items.AddPlaylistRange(incoming);
      Assert.Throws<InvalidOperationException>(() => items.AddPlaylistRange(Fail(incoming[0])));
      Assert.Equal(100000, items.Count);
      items.AddPlaylistRange(items);
      Assert.Equal(200000, items.Count);
      for (int i = 0; i < incoming.Length; i++) Assert.Same(items[i], items[i + incoming.Length]);
    }

    private static IEnumerable<Item> Fail(Item first)
    {
      yield return first;
      throw new InvalidOperationException("enumeration failure");
    }
  }
}