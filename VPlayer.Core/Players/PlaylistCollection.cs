using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using VCore.ItemsCollections;

namespace VPlayer.Core.ViewModels
{
  // The Rx collection still tracks and publishes each item. Only the collection
  // notifications consumed by WPF are deferred while a playlist is appended.
  public sealed class PlaylistCollection<T> : RxObservableCollection<T> where T : class, INotifyPropertyChanged
  {
    private bool publishingBatch;
    private bool collectionChanged;
    private bool countChanged;
    private bool indexerChanged;

    public PlaylistCollection() { }
    public PlaylistCollection(IEnumerable<T> items) : base(items) { }

    public void AddPlaylistRange(IEnumerable<T> items)
    {
      if (publishingBatch) throw new InvalidOperationException("A playlist batch is already being published.");
      // Enumerate before mutation, including when appending this collection itself.
      var snapshot = items.ToArray();
      if (snapshot.Length == 0) return;
      publishingBatch = true;
      try
      {
        base.AddRange(snapshot);
      }
      finally
      {
        publishingBatch = false;
        // AddRange disables the Rx handler while manually tracking its items, and
        // leaves it disabled. A final WPF reset must not emit Rx.Cleared.
        DisableNotification();
        try
        {
          if (countChanged) base.OnPropertyChanged(new PropertyChangedEventArgs("Count"));
          if (indexerChanged) base.OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
          if (collectionChanged) base.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
        finally
        {
          collectionChanged = countChanged = indexerChanged = false;
          EnableNotification();
        }
      }
    }

    protected override void ClearItems()
    {
      // RxObservableCollection does not remove its secondary view on Reset.
      // Keep that view consistent when the player clears/replaces a playlist.
      View.Clear();
      base.ClearItems();
    }

    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs args)
    {
      if (publishingBatch) collectionChanged = true;
      else base.OnCollectionChanged(args);
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs args)
    {
      if (publishingBatch && args.PropertyName == "Count") countChanged = true;
      else if (publishingBatch && args.PropertyName == "Item[]") indexerChanged = true;
      else base.OnPropertyChanged(args);
    }
  }
}