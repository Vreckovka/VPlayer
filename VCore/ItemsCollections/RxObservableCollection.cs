using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;

namespace VCore.ItemsCollections
{
  public class RxObservableCollection<TItem> : ObservableCollection<TItem>, IDisposable where TItem : class, INotifyPropertyChanged
  {
    public Subject<EventPattern<PropertyChangedEventArgs>> ItemUpdated { get; set; }
    public Subject<EventPattern<TItem>> ItemAdded { get; set; } = new Subject<EventPattern<TItem>>();

    private List<IDisposable> itemsDisposables = new List<IDisposable>();
    public RxObservableCollection()
    {
      CollectionChanged += ObservableItems_CollectionChanged;
    }

    private void ObservableItems_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
      foreach (var newItem in e.NewItems.OfType<TItem>())
      {
        itemsDisposables.Add(Observable.FromEventPattern<PropertyChangedEventHandler, PropertyChangedEventArgs>(
          x => newItem.PropertyChanged += x,
          x => newItem.PropertyChanged -= x).Subscribe(ItemPropertyChanged));

        ItemAdded.OnNext(new EventPattern<TItem>(this,newItem));
      }
    }

    private void ItemPropertyChanged(EventPattern<PropertyChangedEventArgs> eventPattern)
    {
      ItemUpdated.OnNext(eventPattern);
    }

    public void Dispose()
    {
      ItemUpdated?.Dispose();
    }
  }
}
