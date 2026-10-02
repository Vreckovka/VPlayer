using System;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Prism.Events;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.ViewModels.SoundItems;
using VPlayer.WindowsPlayer.ViewModels;
using VPlayer.WindowsPlayer.Views;
using Xunit;

namespace VPlayer.Tests
{
  [Collection("UI synchronization")]
  public class MusicVideoRefreshTests
  {
    private static FieldInfo Field(Type type,string name)
    {
      while(type!=null)
      {
        var field=type.GetField(name,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.DeclaredOnly);
        if(field!=null) return field;
        type=type.BaseType;
      }
      throw new InvalidOperationException("Missing field: "+name);
    }
    private static Task Refresh(object player)=>(Task)typeof(MusicPlayerViewModel)
      .GetMethod("PlayVideo",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(player,null);

    [Fact]
    public void EmptyPlaylistStopsVideoWithoutWaitingForViewInitialization()
    {
      LibraryLoadingTests.WithDispatcher(async () =>
      {
        var player=FormatterServices.GetUninitializedObject(typeof(MusicPlayerViewModel));
        var initialized=new TaskCompletionSource<bool>();
        using var semaphore=new SemaphoreSlim(1,1);
        Field(player.GetType(),"semaphoreVideoSlim").SetValue(player,semaphore);
        Field(player.GetType(),"ViewInitializedTask").SetValue(player,initialized);
        var constants=LyricsConstants.Instance;
        bool priorVideo=constants.IsVideo;
        string priorPath=constants.VideoPath;
        Task refresh=null;
        try
        {
          constants.IsVideo=true;constants.VideoPath="cached-video.mp4";
          refresh=Refresh(player);
          await Task.WhenAny(refresh,Task.Delay(2000));
          Assert.True(refresh.IsCompleted,"Clearing a playlist must not wait for the music view to open.");
          await refresh;
          Assert.False(constants.IsVideo);
          Assert.Equal(1,semaphore.CurrentCount);
        }
        finally
        {
          initialized.TrySetResult(true);
          if(refresh!=null) {try{await refresh;}catch{}}
          constants.IsVideo=priorVideo;constants.VideoPath=priorPath;
        }
      });
    }

    [Fact]
    public void TrackClearedDuringViewInitializationIsNotDereferencedOrPlayed()
    {
      LibraryLoadingTests.WithDispatcher(async () =>
      {
        var player=FormatterServices.GetUninitializedObject(typeof(MusicPlayerViewModel));
        var initialized=new TaskCompletionSource<bool>();
        using var semaphore=new SemaphoreSlim(1,1);
        Field(player.GetType(),"semaphoreVideoSlim").SetValue(player,semaphore);
        Field(player.GetType(),"ViewInitializedTask").SetValue(player,initialized);
        var item=new SoundItemInPlaylistViewModel(new SoundItem {Id=1,VideoPath="stale-video.mp4"},new EventAggregator(),new Mock<IStorageManager>().Object);
        var actual=Field(player.GetType(),"actualItem");
        actual.SetValue(player,item);
        var constants=LyricsConstants.Instance;
        bool priorVideo=constants.IsVideo;
        string priorPath=constants.VideoPath;
        Task refresh=null;
        try
        {
          constants.IsVideo=false;constants.VideoPath=null;
          refresh=Refresh(player);
          Assert.False(refresh.IsCompleted);
          actual.SetValue(player,null);
          initialized.SetResult(true);
          await refresh;
          Assert.False(constants.IsVideo);
          Assert.Equal(1,semaphore.CurrentCount);
          // A following empty refresh must also complete, proving the gate recovered.
          await Refresh(player);
        }
        finally
        {
          initialized.TrySetResult(true);
          if(refresh!=null) {try{await refresh;}catch{}}
          constants.IsVideo=priorVideo;constants.VideoPath=priorPath;
          item.Dispose();
        }
      });
    }
  }
}
