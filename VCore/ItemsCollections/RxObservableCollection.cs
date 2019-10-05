using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace VCore.ItemsCollections
{
  public class RxObservableCollection<TItem> : ObservableCollection<TItem>, IDisposable where TItem : class, INotifyPropertyChanged
  {
    #region Fields

    private List<IDisposable> itemsDisposables = new List<IDisposable>();

    #endregion Fields

    #region Constructors

    public RxObservableCollection()
    {
      CollectionChanged += ObservableItems_CollectionChanged;

      ItemAdded = new Subject<EventPattern<TItem>>();
      ItemUpdated = new Subject<EventPattern<PropertyChangedEventArgs>>();
      ItemRemoved = new Subject<EventPattern<TItem>>();
    }

    #endregion Constructors

    #region Properties

    public Subject<EventPattern<TItem>> ItemAdded { get; }
    public Subject<EventPattern<TItem>> ItemRemoved { get; }
    public Subject<EventPattern<PropertyChangedEventArgs>> ItemUpdated { get; }

    #endregion Properties

    #region Methods

    public void Dispose()
    {
      ItemUpdated?.Dispose();
      ItemAdded?.Dispose();
      ItemRemoved?.Dispose();

      foreach (var disposable in itemsDisposables)
      {
        disposable.Dispose();
      }
    }

    protected override void ClearItems()
    {
      var list = this.ToList();
      base.ClearItems();

      try
      {
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, list));
      }
      catch (NotSupportedException ex)
      {
        Debug.WriteLine(ex);
      }
    }

    private void ItemPropertyChanged(EventPattern<PropertyChangedEventArgs> eventPattern)
    {
      ItemUpdated.OnNext(eventPattern);
    }

    private void ObservableItems_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
      if (e.NewItems != null)
      {
        foreach (var newItem in e.NewItems.OfType<TItem>())
        {
          itemsDisposables.Add(Observable.FromEventPattern<PropertyChangedEventHandler, PropertyChangedEventArgs>(
            x => newItem.PropertyChanged += x,
            x => newItem.PropertyChanged -= x).Subscribe(ItemPropertyChanged));

          ItemAdded.OnNext(new EventPattern<TItem>(this, newItem));
        }
      }

      if (e.OldItems != null)
      {
        foreach (var oldItem in e.OldItems.OfType<TItem>())
        {
          ItemRemoved.OnNext(new EventPattern<TItem>(this, oldItem));
        }
      }
    }

    #endregion Methods
  }
}