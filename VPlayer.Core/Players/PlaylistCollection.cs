using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Threading;

namespace VPlayer.Core.ViewModels
{
  // Playlist occurrences keep their order while sharing one property handler per object.
  public sealed class PlaylistCollection<T> : ObservableCollection<T>, IDisposable where T : class, INotifyPropertyChanged
  {
    private sealed class ReferenceComparer : IEqualityComparer<T>
    {
      public bool Equals(T first,T second)=>ReferenceEquals(first,second);
      public int GetHashCode(T value)=>RuntimeHelpers.GetHashCode(value);
    }
    private static readonly PropertyChangedEventArgs countChanged=new PropertyChangedEventArgs("Count");
    private static readonly PropertyChangedEventArgs indexerChanged=new PropertyChangedEventArgs("Item[]");
    private readonly Dictionary<T,int> occurrences=new Dictionary<T,int>(new ReferenceComparer());
    private readonly object trackingGate=new object();
    private readonly PropertyChangedEventHandler itemChanged;
    private readonly ReplaySubject<EventPattern<T>> added=new ReplaySubject<EventPattern<T>>(1);
    private readonly ReplaySubject<EventPattern<T>> removed=new ReplaySubject<EventPattern<T>>(1);
    private readonly ReplaySubject<EventPattern<PropertyChangedEventArgs>> updated=new ReplaySubject<EventPattern<PropertyChangedEventArgs>>(1);
    private readonly Subject<NotifyCollectionChangedEventArgs> cleared=new Subject<NotifyCollectionChangedEventArgs>();
    private int publicationDepth;
    private bool notificationsEnabled=true;
    private int disposeState;

    public PlaylistCollection()=>itemChanged=ItemChanged;
    public PlaylistCollection(IEnumerable<T> items):this()=>AddPlaylistRange(items);
    public ObservableCollection<T> View {get;}=new ObservableCollection<T>();
    public IObservable<EventPattern<T>> ItemAdded=>added.AsObservable();
    public IObservable<EventPattern<T>> ItemRemoved=>removed.AsObservable();
    public IObservable<EventPattern<PropertyChangedEventArgs>> ItemUpdated=>updated.AsObservable();
    public IObservable<NotifyCollectionChangedEventArgs> Cleared=>cleared.AsObservable();

    public void DisableNotification()=>notificationsEnabled=false;
    public void EnableNotification()=>notificationsEnabled=true;
    public void ForEach(Action<T> action)
    {
      if(action==null)throw new ArgumentNullException(nameof(action));
      foreach(var item in this)action(item);
    }
    public void AddRange(IEnumerable<T> items)=>AddPlaylistRange(items);
    public void AddPlaylistRange(IEnumerable<T> items)
    {
      EnsureAvailable();
      if(items==null)throw new ArgumentNullException(nameof(items));
      var snapshot=items.ToArray();
      foreach(var item in snapshot)ValidateItem(item);
      if(snapshot.Length==0)return;
      CheckReentrancy();
      publicationDepth++;
      try
      {
        foreach(var item in snapshot)
        {
          Items.Add(item);
          Track(item);
          View.Add(item);
          if(notificationsEnabled)added.OnNext(new EventPattern<T>(this,item));
        }
      }
      finally
      {
        publicationDepth--;
        if(publicationDepth==0)
        {
          OnPropertyChanged(countChanged);
          OnPropertyChanged(indexerChanged);
          OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
      }
    }

    protected override void InsertItem(int index,T item)
    {
      EnsureAvailable();
      ValidateItem(item);
      CheckReentrancy();
      if(index<0 || index>Count)throw new ArgumentOutOfRangeException(nameof(index));
      Items.Insert(index,item);
      Track(item);
      View.Insert(index,item);
      OnPropertyChanged(countChanged);
      OnPropertyChanged(indexerChanged);
      if(notificationsEnabled)added.OnNext(new EventPattern<T>(this,item));
      OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add,item,index));
    }
    protected override void RemoveItem(int index)
    {
      EnsureAvailable();
      CheckReentrancy();
      var item=Items[index];
      Items.RemoveAt(index);
      Untrack(item);
      View.RemoveAt(index);
      OnPropertyChanged(countChanged);
      OnPropertyChanged(indexerChanged);
      if(notificationsEnabled)removed.OnNext(new EventPattern<T>(this,item));
      OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove,item,index));
    }
    protected override void SetItem(int index,T item)
    {
      EnsureAvailable();
      ValidateItem(item);
      CheckReentrancy();
      var previous=Items[index];
      Items[index]=item;
      Untrack(previous);
      Track(item);
      View[index]=item;
      OnPropertyChanged(indexerChanged);
      if(notificationsEnabled)
      {
        removed.OnNext(new EventPattern<T>(this,previous));
        added.OnNext(new EventPattern<T>(this,item));
      }
      OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace,item,previous,index));
    }
    protected override void MoveItem(int oldIndex,int newIndex)
    {
      EnsureAvailable();
      CheckReentrancy();
      var item=Items[oldIndex];
      if(newIndex<0 || newIndex>=Count)throw new ArgumentOutOfRangeException(nameof(newIndex));
      Items.RemoveAt(oldIndex);
      Items.Insert(newIndex,item);
      View.Move(oldIndex,newIndex);
      OnPropertyChanged(indexerChanged);
      OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move,item,newIndex,oldIndex));
    }
    protected override void ClearItems()
    {
      EnsureAvailable();
      CheckReentrancy();
      DetachAll();
      Items.Clear();
      View.Clear();
      OnPropertyChanged(countChanged);
      OnPropertyChanged(indexerChanged);
      var change=new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
      if(notificationsEnabled)cleared.OnNext(change);
      OnCollectionChanged(change);
    }
    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs args)
    {
      if(publicationDepth==0)base.OnCollectionChanged(args);
    }
    protected override void OnPropertyChanged(PropertyChangedEventArgs args)
    {
      if(publicationDepth==0)base.OnPropertyChanged(args);
    }

    private void Track(T item)
    {
      lock(trackingGate)
      {
        if(occurrences.TryGetValue(item,out var count))occurrences[item]=count+1;
        else
        {
          occurrences.Add(item,1);
          item.PropertyChanged+=itemChanged;
        }
      }
    }
    private void Untrack(T item)
    {
      lock(trackingGate)
      {
        if(!occurrences.TryGetValue(item,out var count))return;
        if(count>1)occurrences[item]=count-1;
        else
        {
          occurrences.Remove(item);
          item.PropertyChanged-=itemChanged;
        }
      }
    }
    private void ItemChanged(object sender,PropertyChangedEventArgs args)
    {
      int count;
      lock(trackingGate)
      {
        if(Volatile.Read(ref disposeState)!=0 || !(sender is T item) || !occurrences.TryGetValue(item,out count))return;
      }
      // Do not hold the tracking lock while invoking observers; they may dispatch to UI.
      var change=new EventPattern<PropertyChangedEventArgs>(sender,args);
      try
      {
        for(int i=0;i<count;i++)
        {
          if(Volatile.Read(ref disposeState)!=0)return;
          updated.OnNext(change);
        }
      }
      catch(ObjectDisposedException) when(Volatile.Read(ref disposeState)!=0) { }
    }
    private void DetachAll()
    {
      lock(trackingGate)
      {
        foreach(var item in occurrences.Keys)item.PropertyChanged-=itemChanged;
        occurrences.Clear();
      }
    }
    private static void ValidateItem(T item)
    {
      if(item==null)throw new ArgumentNullException(nameof(item));
    }
    private void EnsureAvailable()
    {
      if(Volatile.Read(ref disposeState)!=0)throw new ObjectDisposedException(nameof(PlaylistCollection<T>));
    }
    public void Dispose()
    {
      if(Interlocked.Exchange(ref disposeState,1)!=0)return;
      DetachAll();
      added.Dispose();
      removed.Dispose();
      updated.Dispose();
      cleared.Dispose();
    }
  }
}