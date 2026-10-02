using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.Serialization;
using Moq;
using Prism.Events;
using VCore.ItemsCollections;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.ViewModels.SoundItems;
using VPlayer.WindowsPlayer.ViewModels;
using Xunit;

namespace VPlayer.Tests
{
  public class PlaylistDurationTests
  {
    private sealed class Player : IDisposable
    {
      internal readonly MusicPlayerViewModel Model=(MusicPlayerViewModel)FormatterServices.GetUninitializedObject(typeof(MusicPlayerViewModel));
      internal readonly RxObservableCollection<SoundItemInPlaylistViewModel> Items=new RxObservableCollection<SoundItemInPlaylistViewModel>();
      private readonly EventAggregator events=new EventAggregator();
      private readonly IStorageManager storage=new Mock<IStorageManager>().Object;
      private readonly List<SoundItemInPlaylistViewModel> owned=new List<SoundItemInPlaylistViewModel>();
      internal int Notifications;
      internal TimeSpan PublishedDuration;

      internal Player()
      {
        var type=Model.GetType();
        FieldInfo field=null;
        while(type!=null && field==null)
        {
          field=type.GetField("playList",BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.DeclaredOnly);
          type=type.BaseType;
        }
        if(field==null) throw new InvalidOperationException("Missing playlist field");
        field.SetValue(Model,Items);
        Call("HookToPlaylistCollectionChanged");
        Model.PropertyChanged+=(sender,args)=>
        {
          if(args.PropertyName==nameof(Model.TotalPlaylistDuration))
          {
            Notifications++;
            // Read the real getter on every notification, as the WPF bindings do.
            PublishedDuration=Model.TotalPlaylistDuration;
          }
        };
      }

      internal SoundItemInPlaylistViewModel Item(int id,int seconds)
      {
        var item=new SoundItemInPlaylistViewModel(new SoundItem {Id=id,Duration=seconds},events,storage);
        owned.Add(item);
        return item;
      }
      internal void Call(string name,params object[] args)
      {
        try {Model.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(Model,args);}
        catch(TargetInvocationException ex) when(ex.InnerException!=null)
        {
          ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
          throw;
        }
      }
      public void Dispose()
      {
        Call("UnHookToPlaylistCollectionChanged");
        Items.Dispose();
        foreach(var item in owned) item.Dispose();
      }
    }

    [Fact]
    public void ReplacePublishesOneFinalDurationAndPreservesEveryOccurrence()
    {
      using var player=new Player();
      var old=player.Item(1,7);
      old.IsInPlaylist=true;
      player.Items.Add(old);
      player.Notifications=0;
      var incoming=Enumerable.Range(0,10000).Select(i=>player.Item(i%97,180+i%19)).ToArray();
      player.Call("ReplacePlaylistItems",(object)incoming);

      Assert.Equal(1,player.Notifications);
      Assert.Equal(TimeSpan.FromSeconds(incoming.Sum(x=>x.Duration)),player.PublishedDuration);
      Assert.False(old.IsInPlaylist);
      Assert.Equal(incoming.Length,player.Items.Count);
      for(int i=0;i<incoming.Length;i++) Assert.Same(incoming[i],player.Items[i]);
    }

    [Fact]
    public void AppendPublishesOneTotalAndSingleTrackEditsStillPublishImmediately()
    {
      using var player=new Player();
      var old=player.Item(1,7);
      old.IsInPlaylist=true;
      player.Items.Add(old);
      player.Notifications=0;
      var incoming=Enumerable.Range(0,10000).Select(i=>player.Item(i%97,200+i%13)).ToArray();
      player.Call("AddPlaylistItems",(object)incoming);

      Assert.Equal(1,player.Notifications);
      Assert.Equal(TimeSpan.FromSeconds(7+incoming.Sum(x=>x.Duration)),player.PublishedDuration);
      Assert.True(old.IsInPlaylist);
      Assert.Same(old,player.Items[0]);
      for(int i=0;i<incoming.Length;i++) Assert.Same(incoming[i],player.Items[i+1]);
      player.Items.Remove(old);
      Assert.Equal(2,player.Notifications);
      Assert.Equal(TimeSpan.FromSeconds(incoming.Sum(x=>x.Duration)),player.PublishedDuration);
      player.Items.Clear();
      Assert.Equal(3,player.Notifications);
      Assert.Equal(TimeSpan.Zero,player.PublishedDuration);
    }

    [Fact]
    public void FailedBulkInsertionPublishesPartialTotalAndRestoresOrdinaryNotifications()
    {
      using var player=new Player();
      var first=player.Item(1,17);
      var second=player.Item(2,11);
      NotifyCollectionChangedEventHandler fail=(sender,args)=>throw new InvalidOperationException("observer failure");
      player.Items.CollectionChanged+=fail;
      Assert.Throws<InvalidOperationException>(()=>player.Call("AddPlaylistItems",(object)new[] {first,second}));
      player.Items.CollectionChanged-=fail;

      Assert.Single(player.Items);
      Assert.Equal(1,player.Notifications);
      Assert.Equal(TimeSpan.FromSeconds(17),player.PublishedDuration);
      player.Items.Add(second);
      Assert.Equal(2,player.Notifications);
      Assert.Equal(TimeSpan.FromSeconds(28),player.PublishedDuration);
      player.Items.Remove(first);
      Assert.Equal(3,player.Notifications);
      Assert.Equal(TimeSpan.FromSeconds(11),player.PublishedDuration);
    }
  }
}
