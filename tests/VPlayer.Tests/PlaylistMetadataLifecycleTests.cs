using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Logger;
using Moq;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.WindowsPlayer.ViewModels;
using Xunit;

namespace VPlayer.Tests
{
  public class PlaylistMetadataLifecycleTests
  {
    private sealed class ControlledPlayer : MusicPlayerViewModel
    {
      // Construction is bypassed: these tests need no devices, network or views.
      public ControlledPlayer() : base(null,null,null,null,null,null,null,null,null,null,null,null,null,null,null) {}
      internal TaskCompletionSource<bool> ProbeEntered;
      internal TaskCompletionSource<bool> ProbeCompleted;
      internal int MetadataCalls;
      protected override Task GetMediaInfo(SoundItem model)
      {
        ProbeEntered.TrySetResult(true);
        return ProbeCompleted.Task;
      }
      protected override Task DownloadItemInfo(CancellationToken cancellationToken)
      {
        MetadataCalls++;
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
      }
    }
    private static MemberInfo Member(Type type,string name,bool method)
    {
      while(type!=null)
      {
        MemberInfo member=method?(MemberInfo)type.GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.DeclaredOnly):
          type.GetField(name,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.DeclaredOnly);
        if(member!=null) return member;
        type=type.BaseType;
      }
      throw new InvalidOperationException("Missing member: "+name);
    }
    private sealed class Harness : IDisposable
    {
      internal readonly ControlledPlayer Player=(ControlledPlayer)FormatterServices.GetUninitializedObject(typeof(ControlledPlayer));
      internal readonly Mock<ILogger> Logger=new Mock<ILogger>();
      internal readonly List<CancellationTokenSource> Active=new List<CancellationTokenSource>();
      private readonly List<CancellationTokenSource> owned=new List<CancellationTokenSource>();
      internal Harness()
      {
        Player.ProbeEntered=new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        Player.ProbeCompleted=new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        ((FieldInfo)Member(Player.GetType(),"cTSOnActualItemChangeds",false)).SetValue(Player,Active);
        ((FieldInfo)Member(Player.GetType(),"logger",false)).SetValue(Player,Logger.Object);
      }
      internal CancellationTokenSource Begin()
      {
        var source=(CancellationTokenSource)((MethodInfo)Member(Player.GetType(),"GetCTSAndCancel",true)).Invoke(Player,null);
        owned.Add(source);
        return source;
      }
      internal Task Refresh(CancellationTokenSource source)=>(Task)((MethodInfo)Member(Player.GetType(),"RefreshItemMetadataAsync",true))
        .Invoke(Player,new object[] {new SoundItem {Id=1},source});
      public void Dispose()
      {
        foreach(var source in owned) source.Dispose();
      }
    }

    [Fact]
    public void BeginningNewRefreshCancelsAndReplacesTheRegisteredGeneration()
    {
      using var h=new Harness();
      var first=h.Begin();
      var second=h.Begin();
      Assert.True(first.IsCancellationRequested);
      Assert.False(second.IsCancellationRequested);
      Assert.Same(second,Assert.Single(h.Active));
    }

    [Fact]
    public async Task CancelledProbeNeverStartsMetadataForTheReplacementPlaylist()
    {
      using var h=new Harness();
      var source=h.Begin();
      var refresh=h.Refresh(source);
      await h.Player.ProbeEntered.Task;
      source.Cancel();
      h.Player.ProbeCompleted.SetResult(true);
      await refresh;
      Assert.Equal(0,h.Player.MetadataCalls);
      Assert.Empty(h.Active);
      Assert.Empty(h.Logger.Invocations);
    }

    [Fact]
    public async Task DisposedTrackCompletionAfterCancellationIsObservedAndCleanedUp()
    {
      using var h=new Harness();
      var source=h.Begin();
      var refresh=h.Refresh(source);
      await h.Player.ProbeEntered.Task;
      source.Cancel();
      h.Player.ProbeCompleted.SetException(new ObjectDisposedException("old track"));
      await refresh;
      Assert.Equal(0,h.Player.MetadataCalls);
      Assert.Empty(h.Active);
      Assert.Empty(h.Logger.Invocations);
    }

    [Fact]
    public async Task ActiveRefreshFailureIsLoggedInsteadOfBecomingUnobserved()
    {
      using var h=new Harness();
      var source=h.Begin();
      var refresh=h.Refresh(source);
      await h.Player.ProbeEntered.Task;
      var failure=new InvalidOperationException("probe failure");
      h.Player.ProbeCompleted.SetException(failure);
      await refresh;
      Assert.Equal(0,h.Player.MetadataCalls);
      Assert.Empty(h.Active);
      Assert.Contains(h.Logger.Invocations,call=>call.Arguments.Any(argument=>ReferenceEquals(argument,failure)));
    }
  }
}
