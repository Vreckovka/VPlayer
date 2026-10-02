using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Logger;
using Moq;
using Prism.Events;
using VCore.WPF;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.Core.ViewModels;
using VPlayer.Core.ViewModels.SoundItems;
using VPlayer.WindowsPlayer.Players;
using VPlayer.WindowsPlayer.ViewModels;
using Xunit;

namespace VPlayer.Tests
{
  [Collection("UI synchronization")]
  public class PlaybackSelectionRaceTests
  {
    private sealed class UiContext : SynchronizationContext
    {
      internal readonly ConcurrentQueue<Action> Pending=new ConcurrentQueue<Action>();
      internal readonly List<Exception> Errors=new List<Exception>();
      internal int Active;
      public override void OperationStarted()=>Interlocked.Increment(ref Active);
      public override void OperationCompleted()=>Interlocked.Decrement(ref Active);
      public override void Post(SendOrPostCallback callback,object state)=>Pending.Enqueue(()=>callback(state));
      internal void Drain()
      {
        var previous=Current;
        SetSynchronizationContext(this);
        try {while(Pending.TryDequeue(out var work)){try {work();} catch(Exception error){Errors.Add(error);}}}
        finally {SetSynchronizationContext(previous);}
      }
    }
    private sealed class Gate
    {
      internal readonly TaskCompletionSource<bool> Entered=new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
      internal readonly TaskCompletionSource<bool> Release=new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
      internal readonly TaskCompletionSource<bool> Completed=new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
    }
    private sealed class ControlledPlayer : MusicPlayerViewModel
    {
      public ControlledPlayer() : base(null,null,null,null,null,null,null,null,null,null,null,null,null,null,null) {}
      internal ConcurrentDictionary<int,Gate> Gates;
      internal ConcurrentQueue<int> Notifications;
      internal List<int> Played;
      internal bool UseBasePlay;
      internal Gate Initialization;
      protected override Task BeforeSetMedia(SoundItem model)
      {
        var gate=Gates[model.Id];
        gate.Entered.TrySetResult(true);
        return gate.Release.Task;
      }
      protected override Task SetMedia(SoundItem model)
      {
        var task=base.SetMedia(model);
        if(model!=null) task.ContinueWith(completed=>
        {
          if(completed.IsFaulted) _=completed.Exception;
          Gates[model.Id].Completed.TrySetResult(true);
        },TaskScheduler.Default);
        return task;
      }
      public override void OnSetActualItem(SoundItemInPlaylistViewModel item,bool playing) {}
      protected override void OnActualItemChanged() {}
      protected override void OnIsPlayingChanged() {}
      public override void OnNewItemPlay(SoundItem model)=>Notifications.Enqueue(model.Id);
      protected override void OnPlay() {}
      protected override Task WaitForVlcInitilization()
      {
        Initialization.Entered.TrySetResult(true);
        return Initialization.Release.Task;
      }
      public override Task Play(){if(UseBasePlay)return base.Play();Played.Add(ActualItem?.Model.Id??-1);return Task.CompletedTask;}
      internal Task Load(SoundItem model)=>SetMedia(model);
    }
    private static FieldInfo Field(Type type,string name)
    {
      while(type!=null){var field=type.GetField(name,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.DeclaredOnly);if(field!=null)return field;type=type.BaseType;}
      throw new InvalidOperationException("Missing field: "+name);
    }
    private sealed class Fixture : IDisposable
    {
      internal readonly ControlledPlayer Player=(ControlledPlayer)FormatterServices.GetUninitializedObject(typeof(ControlledPlayer));
      internal readonly Mock<ILogger> Logger=new Mock<ILogger>();
      internal readonly UiContext Ui=new UiContext();
      internal readonly ConcurrentQueue<string> Applied=new ConcurrentQueue<string>();
      internal readonly PlaylistCollection<SoundItemInPlaylistViewModel> Items=new PlaylistCollection<SoundItemInPlaylistViewModel>();
      private readonly ReplaySubject<int> actual=new ReplaySubject<int>(1);
      private readonly SynchronizationContext previousUi;
      internal Fixture()
      {
        Player.Gates=new ConcurrentDictionary<int,Gate>();
        Player.Notifications=new ConcurrentQueue<int>();
        Player.Played=new List<int>();
        Player.Initialization=new Gate();
        var events=new EventAggregator();
        var storage=new Mock<VPlayer.AudioStorage.Interfaces.Storage.IStorageManager>().Object;
        Items.AddPlaylistRange(Enumerable.Range(1,100000).Select(id=>new SoundItemInPlaylistViewModel(
          new SoundItem {Id=id,FileInfoEntity=new FileInfoEntity {Name="Track "+id,Source="file:///D:/benchmark/"+id+".mp3"}},events,storage)));
        Field(Player.GetType(),"mediaChangeLock").SetValue(Player,new object());
        Field(Player.GetType(),"playList").SetValue(Player,Items);
        Field(Player.GetType(),"actualItemSubject").SetValue(Player,actual);
        Field(Player.GetType(),"shuffleList").SetValue(Player,new HashSet<SoundItemInPlaylistViewModel>());
        Field(Player.GetType(),"logger").SetValue(Player,Logger.Object);
        Field(Player.GetType(),"actualSavedPlaylist").SetValue(Player,new SoundItemFilePlaylist {Id=-1});
        Field(Player.GetType(),"isRepeate").SetValue(Player,false);
        var device=new Mock<IPlayer>();
        device.SetupProperty(x=>x.Media);
        device.Setup(x=>x.SetNewMedia(It.IsAny<Uri>(),It.IsAny<CancellationToken>())).Callback((Uri uri,CancellationToken token)=>
        {
          device.Object.Media=uri==null?null:new Mock<IMedia>().Object;
          if(uri!=null)Applied.Enqueue(uri.AbsoluteUri);
        });
        device.Setup(x=>x.Play()).Callback(()=>Player.Played.Add(Player.ActualItem?.Model.Id??-1));
        Player.MediaPlayer=device.Object;
        previousUi=VSynchronizationContext.UISynchronizationContext;
        VSynchronizationContext.UISynchronizationContext=Ui;
      }
      internal Gate Register(int id)=>Player.Gates.GetOrAdd(id,_=>new Gate());
      internal void Select(int index)
      {
        var previous=SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(Ui);
        try {Player.SetItemAndPlay(index,true);}
        finally {SynchronizationContext.SetSynchronizationContext(previous);}
      }
      internal async Task PumpUntil(Func<bool> condition)
      {
        var deadline=DateTime.UtcNow.AddSeconds(10);
        do {Ui.Drain();if(condition())return;await Task.Delay(5);}while(DateTime.UtcNow<deadline);
        throw new TimeoutException("Playback work did not finish.");
      }
      internal async Task ReleaseAll()
      {
        foreach(var gate in Player.Gates.Values)gate.Release.TrySetResult(true);
        await PumpUntil(()=>Ui.Active==0);
      }
      internal void ClearCurrent()=>Field(Player.GetType(),"actualItem").SetValue(Player,null);
      public void Dispose()
      {
        foreach(var gate in Player.Gates.Values)gate.Release.TrySetResult(true);
        VSynchronizationContext.UISynchronizationContext=previousUi;
        Items.Dispose();
        foreach(var item in Items)item.Dispose();
        actual.Dispose();
      }
    }

    [Fact]
    public async Task HundredOutOfOrderSelectionsCommitOnlyTheFinalTrack()
    {
      using var f=new Fixture();
      for(int index=99900;index<100000;index++)
      {
        var gate=f.Register(index+1);
        f.Select(index);
        await f.PumpUntil(()=>gate.Entered.Task.IsCompleted);
      }
      await Task.WhenAll(f.Player.Gates.Values.Select(g=>g.Entered.Task));
      var latest=f.Register(100000);
      latest.Release.TrySetResult(true);
      await f.PumpUntil(()=>f.Player.Played.Count>0);
      for(int id=99999;id>=99901;id--)
      {
        var old=f.Register(id);
        old.Release.TrySetResult(true);
        await f.PumpUntil(()=>old.Completed.Task.IsCompleted);
      }
      await f.ReleaseAll();
      Assert.Empty(f.Ui.Errors);
      Assert.Equal(100000,f.Player.ActualItem.Model.Id);
      Assert.Equal("file:///D:/benchmark/100000.mp3",Assert.Single(f.Applied));
      Assert.Equal(100000,Assert.Single(f.Player.Notifications));
      Assert.Equal(100000,Assert.Single(f.Player.Played));
    }

    [Fact]
    public async Task ClearingDuringPreparationNeverAutoplaysOrThrows()
    {
      using var f=new Fixture();
      var gate=f.Register(100000);
      f.Select(99999);
      await gate.Entered.Task;
      f.ClearCurrent();
      await f.Player.Load(null);
      gate.Release.TrySetResult(true);
      await f.ReleaseAll();
      Assert.Empty(f.Ui.Errors);
      Assert.Empty(f.Applied);
      Assert.Empty(f.Player.Notifications);
      Assert.Empty(f.Player.Played);
    }

    [Fact]
    public async Task EarlierOccurrenceOfTheSameTrackCannotReplayTheNewSelection()
    {
      using var f=new Fixture();
      var first=f.Register(99999);
      f.Select(99998);
      await first.Entered.Task;
      f.Items[99999].Model=f.Items[99998].Model;
      f.Select(99999);
      first.Release.TrySetResult(true);
      await f.ReleaseAll();
      Assert.Empty(f.Ui.Errors);
      Assert.Single(f.Player.Played);
      Assert.Same(f.Items[99999],f.Player.ActualItem);
      Assert.Single(f.Applied);
    }

    [Fact]
    public async Task RepeatedRequestsForTheSameOccurrenceAutoplayOnlyOnce()
    {
      using var f=new Fixture();
      var gate=f.Register(100000);
      f.Select(99999);
      await gate.Entered.Task;
      f.Select(99999);
      gate.Release.TrySetResult(true);
      await f.ReleaseAll();
      Assert.Empty(f.Ui.Errors);
      Assert.Single(f.Player.Played);
      Assert.Single(f.Applied);
    }

    [Fact]
    public async Task EndingThePlaylistInvalidatesThePendingLastTrack()
    {
      using var f=new Fixture();
      var gate=f.Register(100000);
      f.Select(99999);
      await gate.Entered.Task;
      f.Select(100000);
      await f.ReleaseAll();
      Assert.Empty(f.Ui.Errors);
      Assert.Empty(f.Applied);
      Assert.Empty(f.Player.Played);
      Assert.True(f.Player.IsPlayFnished);
    }

    [Fact]
    public async Task DisposingDuringPreparationDropsPendingCallbacks()
    {
      using var f=new Fixture();
      var gate=f.Register(100000);
      f.Select(99999);
      await gate.Entered.Task;
      Field(f.Player.GetType(),"isDisposing").SetValue(f.Player,true);
      await f.ReleaseAll();
      Assert.Empty(f.Ui.Errors);
      Assert.Empty(f.Applied);
      Assert.Empty(f.Player.Notifications);
      Assert.Empty(f.Player.Played);
    }

    [Fact]
    public async Task LateDeviceInitializationCannotResumeAfterClearing()
    {
      using var f=new Fixture();
      f.Player.UseBasePlay=true;
      var gate=f.Register(100000);
      f.Select(99999);
      await gate.Entered.Task;
      gate.Release.TrySetResult(true);
      await f.PumpUntil(()=>f.Player.Initialization.Entered.Task.IsCompleted);
      f.ClearCurrent();
      await f.Player.Load(null);
      f.Player.Initialization.Release.TrySetResult(true);
      await f.ReleaseAll();
      Assert.Empty(f.Ui.Errors);
      Assert.Empty(f.Player.Played);
    }

    [Fact]
    public async Task ReplacingTheMediaDeviceDropsPreparedWorkForTheOldDevice()
    {
      using var f=new Fixture();
      var gate=f.Register(100000);
      f.Select(99999);
      await gate.Entered.Task;
      var replacement=new Mock<IPlayer>();
      f.Player.MediaPlayer=replacement.Object;
      await f.ReleaseAll();
      Assert.Empty(f.Ui.Errors);
      Assert.Empty(f.Applied);
      Assert.Empty(f.Player.Played);
      replacement.Verify(x=>x.SetNewMedia(It.IsAny<Uri>(),It.IsAny<CancellationToken>()),Times.Never);
    }

    [Fact]
    public async Task MediaClearWhileKeepingTheSelectionCannotAutoplayOldPreparation()
    {
      using var f=new Fixture();
      var gate=f.Register(100000);
      f.Select(99999);
      await gate.Entered.Task;
      await f.Player.Load(null);
      await f.ReleaseAll();
      Assert.Empty(f.Ui.Errors);
      Assert.Empty(f.Applied);
      Assert.Empty(f.Player.Played);
    }

    [Fact]
    public async Task MediaClearDuringInitializationCannotRestartTheSameSelection()
    {
      using var f=new Fixture();
      f.Player.UseBasePlay=true;
      var gate=f.Register(100000);
      f.Select(99999);
      await gate.Entered.Task;
      gate.Release.TrySetResult(true);
      await f.PumpUntil(()=>f.Player.Initialization.Entered.Task.IsCompleted);
      await f.Player.Load(null);
      f.Player.Initialization.Release.TrySetResult(true);
      await f.ReleaseAll();
      Assert.Empty(f.Ui.Errors);
      Assert.Empty(f.Player.Played);
    }

    [Fact]
    public async Task CurrentPreparedSelectionStillStartsTheDeviceOnce()
    {
      using var f=new Fixture();
      f.Player.UseBasePlay=true;
      f.Player.Initialization.Release.TrySetResult(true);
      var gate=f.Register(100000);
      f.Select(99999);
      await gate.Entered.Task;
      await f.ReleaseAll();
      Assert.Empty(f.Ui.Errors);
      Assert.Equal(100000,Assert.Single(f.Player.Played));
      Assert.True(f.Player.IsPlaying);
      Assert.Single(f.Applied);
      Assert.Single(f.Player.Notifications);
    }

    [Fact]
    public async Task PreviousFromFirstSelectsTheLastOccurrence()
    {
      using var f=new Fixture();
      Field(f.Player.GetType(),"actualItem").SetValue(f.Player,f.Items[0]);
      Field(f.Player.GetType(),"actualItemIndex").SetValue(f.Player,0);
      var last=f.Register(100000);
      var previous=SynchronizationContext.Current;
      SynchronizationContext.SetSynchronizationContext(f.Ui);
      try {f.Player.PlayPrevious();}finally {SynchronizationContext.SetSynchronizationContext(previous);}
      await f.PumpUntil(()=>last.Entered.Task.IsCompleted || f.Ui.Active==0);
      await f.ReleaseAll();
      Assert.Equal(99999,f.Player.ActualItemIndex);
      Assert.Same(f.Items[99999],f.Player.ActualItem);
      Assert.Equal(100000,Assert.Single(f.Player.Played));
    }
  }
}
