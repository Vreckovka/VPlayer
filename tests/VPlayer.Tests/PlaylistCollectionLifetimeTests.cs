using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using VPlayer.Core.ViewModels;
using Xunit;

namespace VPlayer.Tests
{
  public class PlaylistCollectionLifetimeTests
  {
    private class Item : INotifyPropertyChanged
    {
      internal int Id;
      internal bool InPlaylist;
      public event PropertyChangedEventHandler PropertyChanged;
      internal void Change()=>PropertyChanged?.Invoke(this,new PropertyChangedEventArgs("Title"));
    }
    private sealed class EqualItem : Item
    {
      public override bool Equals(object value)=>value is EqualItem other && other.Id==Id;
      public override int GetHashCode()=>Id;
    }
    private static Item[] Rows()=>Enumerable.Range(0,100000).Select(i=>new Item {Id=i}).ToArray();
    [Fact]
    public async Task DisposingDuringARepeatedReferencePropertyCallbackStopsPublisherSafely()
    {
      using var collection=new PlaylistCollection<Item>();
      var row=new Item();
      collection.AddPlaylistRange(Enumerable.Repeat(row,100000));
      using var entered=new ManualResetEventSlim(false);
      using var resume=new ManualResetEventSlim(false);
      using var changed=collection.ItemUpdated.Subscribe(_=>
      {
        entered.Set();
        if(!resume.Wait(TimeSpan.FromSeconds(5)))throw new TimeoutException("Callback was not released.");
      });
      var publishing=Task.Run(row.Change);
      try
      {
        Assert.True(entered.Wait(TimeSpan.FromSeconds(5)));
        collection.Dispose();
      }
      finally {resume.Set();}
      Assert.Same(publishing,await Task.WhenAny(publishing,Task.Delay(5000)));
      await publishing;
    }

    [Fact]
    public void LiveObserverExceptionsAreNotHiddenAsDisposalRaces()
    {
      using var collection=new PlaylistCollection<Item>();
      var row=new Item();
      collection.Add(row);
      using var changed=collection.ItemUpdated.Subscribe(_=>throw new ObjectDisposedException("observer"));
      Assert.Throws<ObjectDisposedException>(()=>row.Change());
    }

    [Fact]
    public void RemovedRowsStopReportingUpdatesWhileRemainingRowsStillReport()
    {
      using var collection=new PlaylistCollection<Item>();
      var rows=Rows();
      collection.AddPlaylistRange(rows);
      var senders=new List<object>();
      using var updated=collection.ItemUpdated.Subscribe(e=>senders.Add(e.Sender));
      collection.RemoveAt(99999);
      rows[99999].Change();
      Assert.Empty(senders);
      rows[50000].Change();
      Assert.Single(senders);
      Assert.Same(rows[50000],senders[0]);
    }

    [Fact]
    public void ClearDetachesEveryRowAcrossRepeatedHugeLoads()
    {
      using var collection=new PlaylistCollection<Item>();
      int updates=0;
      using var changed=collection.ItemUpdated.Subscribe(_=>updates++);
      for(int round=0;round<3;round++)
      {
        var rows=Rows();
        collection.AddPlaylistRange(rows);
        collection.Clear();
        foreach(var row in rows)row.Change();
        Assert.Equal(0,updates);
        Assert.Empty(collection.View);
      }
    }

    [Fact]
    public void ReplacementsWhileReactiveNotificationsAreDisabledKeepViewsAndTrackingCurrent()
    {
      using var collection=new PlaylistCollection<Item>();
      var old=Rows();
      var next=Rows();
      collection.AddPlaylistRange(old);
      int added=0,removed=0;
      using var a=collection.ItemAdded.Subscribe(_=>added++);
      using var r=collection.ItemRemoved.Subscribe(_=>removed++);
      added=0; // The observable retains its last added occurrence.
      collection.DisableNotification();
      try {for(int i=0;i<next.Length;i++)collection[i]=next[i];}
      finally {collection.EnableNotification();}
      Assert.Equal(0,added);
      Assert.Equal(0,removed);
      Assert.Equal(next.Length,collection.View.Count);
      for(int i=0;i<next.Length;i++)Assert.Same(next[i],collection.View[i]);
      var senders=new List<object>();
      using var changed=collection.ItemUpdated.Subscribe(e=>senders.Add(e.Sender));
      old[50000].Change();
      Assert.Empty(senders);
      next[50000].Change();
      Assert.Single(senders);
      Assert.Same(next[50000],senders[0]);
    }

    [Fact]
    public void DuplicateReferencesReportOncePerRemainingOccurrenceAndDetachAfterClear()
    {
      using var collection=new PlaylistCollection<Item>();
      var row=new Item();
      collection.AddPlaylistRange(Enumerable.Repeat(row,100000));
      int updates=0;
      using var changed=collection.ItemUpdated.Subscribe(_=>updates++);
      collection.RemoveAt(99999);
      row.Change();
      Assert.Equal(99999,updates);
      collection.Clear();
      updates=0;
      row.Change();
      Assert.Equal(0,updates);
    }

    [Fact]
    public void MovingHugePlaylistRowsPreservesMembershipTrackingAndBothViewOrders()
    {
      using var collection=new PlaylistCollection<Item>();
      var rows=Rows();
      int added=0,removed=0;
      using var a=collection.ItemAdded.Subscribe(e=>{added++;e.EventArgs.InPlaylist=true;});
      using var r=collection.ItemRemoved.Subscribe(e=>{removed++;e.EventArgs.InPlaylist=false;});
      collection.AddPlaylistRange(rows);
      var view=new ListCollectionView(collection);
      added=0;
      collection.Move(0,99999);
      Assert.Equal(0,added);
      Assert.Equal(0,removed);
      Assert.All(rows,row=>Assert.True(row.InPlaylist));
      for(int i=0;i<rows.Length;i++)
      {
        var expected=rows[(i+1)%rows.Length];
        Assert.Same(expected,collection[i]);
        Assert.Same(expected,collection.View[i]);
        Assert.Same(expected,view.GetItemAt(i));
      }
      int updates=0;
      using var changed=collection.ItemUpdated.Subscribe(_=>updates++);
      rows[0].Change();
      Assert.Equal(1,updates);
    }

    [Fact]
    public void ValueEqualRowsStillHaveIndependentPropertyTracking()
    {
      using var collection=new PlaylistCollection<EqualItem>();
      var first=new EqualItem {Id=42};
      var second=new EqualItem {Id=42};
      collection.AddPlaylistRange(new[] {first,second});
      collection.RemoveAt(0);
      var senders=new List<object>();
      using var changed=collection.ItemUpdated.Subscribe(e=>senders.Add(e.Sender));
      second.Change();
      Assert.Single(senders);
      Assert.Same(second,senders[0]);
      senders.Clear();
      first.Change();
      Assert.Empty(senders);
    }
  }
}