using System;
using System.Reflection;
using System.Runtime.Serialization;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Windows.Threading;
using VPlayer.WindowsPlayer.ViewModels;
using Xunit;

namespace VPlayer.Tests
{
  [Collection("UI synchronization")]
  public class PlaylistSearchDispatchTests
  {
    private sealed class Player : MusicPlayerViewModel
    {
      public Player() : base(null,null,null,null,null,null,null,null,null,null,null,null,null,null,null) {}
      internal Dispatcher Owner;
      internal TaskCompletionSource<(string Query,bool OnUi)> Completed;
      protected override void FilterByActualSearch(string query)=>Completed.TrySetResult((query,Owner.CheckAccess()));
    }
    private static MemberInfo Find(Type type,string name,bool method)
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
    [Theory]
    [InlineData(1)]
    [InlineData(32)]
    public void DebouncedWorkerRequestsPublishTheLatestSearchOnTheUiDispatcher(int requests)
    {
      LibraryLoadingTests.WithDispatcher(async () =>
      {
        var player=(Player)FormatterServices.GetUninitializedObject(typeof(Player));
        player.Owner=Dispatcher.CurrentDispatcher;
        player.Completed=new TaskCompletionSource<(string Query,bool OnUi)>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var source=new ReplaySubject<string>(1);
        ((FieldInfo)Find(player.GetType(),"actualSearchSubject",false)).SetValue(player,source);
        using var subscription=(IDisposable)((MethodInfo)Find(player.GetType(),"SubscribeToActualSearch",true)).Invoke(player,null);
        await Task.Run(()=>{for(int i=0;i<requests;i++) source.OnNext("query-"+i);});
        var winner=await Task.WhenAny(player.Completed.Task,Task.Delay(2000));
        Assert.Same(player.Completed.Task,winner);
        var result=await player.Completed.Task;
        Assert.Equal("query-"+(requests-1),result.Query);
        Assert.True(result.OnUi,"Filtering must not enumerate the UI playlist from a timer worker.");
      });
    }
  }
}
