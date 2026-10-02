using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Logger;
using Moq;
using Prism.Events;
using VCore.WPF;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.ViewModels.SoundItems;
using VPlayer.WindowsPlayer.ViewModels;
using Xunit;

namespace VPlayer.Tests
{
  [Collection("UI synchronization")]
  public class PlaybackProgressRaceTests
  {
    private sealed class QueuedContext : SynchronizationContext
    {
      internal readonly Queue<Action> Actions=new Queue<Action>();
      public override void Post(SendOrPostCallback callback,object state)=>Actions.Enqueue(()=>callback(state));
      internal void Drain(){while(Actions.Count>0) Actions.Dequeue()();}
    }
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
    private static MethodInfo SaveMethod(Type type)
    {
      while(type!=null)
      {
        var method=type.GetMethod("SavePlaybackProgressAsync",BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.DeclaredOnly);
        if(method!=null) return method;
        type=type.BaseType;
      }
      throw new InvalidOperationException("Missing progress method");
    }
    private sealed class Fixture : IDisposable
    {
      internal readonly object Player=FormatterServices.GetUninitializedObject(typeof(MusicPlayerViewModel));
      internal readonly Mock<IStorageManager> Storage=new Mock<IStorageManager>();
      internal readonly Mock<ILogger> Logger=new Mock<ILogger>();
      internal readonly QueuedContext Ui=new QueuedContext();
      internal readonly SoundItemInPlaylistViewModel Original;
      internal readonly SoundItemInPlaylistViewModel Replacement;
      internal readonly List<SoundItem> SavedModels=new List<SoundItem>();
      internal Action AfterPlaylistSave;
      internal Exception EntityFailure;
      internal int OriginalNotifications,ReplacementNotifications;
      private readonly FieldInfo actual;
      private readonly SemaphoreSlim semaphore=new SemaphoreSlim(1,1);
      private readonly SynchronizationContext previous;
      internal Fixture()
      {
        actual=Field(Player.GetType(),"actualItem");
        Original=new SoundItemInPlaylistViewModel(new SoundItem {Id=1,TimePlayed=TimeSpan.FromSeconds(123)},new EventAggregator(),Storage.Object);
        Replacement=new SoundItemInPlaylistViewModel(new SoundItem {Id=2,TimePlayed=TimeSpan.FromSeconds(456)},new EventAggregator(),Storage.Object);
        Original.PropertyChanged+=(sender,args)=>{if(args.PropertyName=="Model") OriginalNotifications++;};
        Replacement.PropertyChanged+=(sender,args)=>{if(args.PropertyName=="Model") ReplacementNotifications++;};
        Current=Original;
        Field(Player.GetType(),"storageManager").SetValue(Player,Storage.Object);
        Field(Player.GetType(),"logger").SetValue(Player,Logger.Object);
        Field(Player.GetType(),"playlistSemaphore").SetValue(Player,semaphore);
        Field(Player.GetType(),"actualSavedPlaylist").SetValue(Player,new SoundItemFilePlaylist {Id=1,Name="Worst queue",
          PlaylistItems=Enumerable.Range(1,100000).Select(id=>new PlaylistSoundItem {Id=id,IdReferencedItem=1+id%2,OrderInPlaylist=id}).ToList()});
        SoundItemFilePlaylist updated=null;
        Storage.Setup(x=>x.UpdatePlaylist<SoundItemFilePlaylist,PlaylistSoundItem,SoundItem>(It.IsAny<SoundItemFilePlaylist>(),out updated))
          .Returns(()=>{AfterPlaylistSave?.Invoke();return false;});
        Storage.Setup(x=>x.UpdateEntityAsync(It.IsAny<SoundItem>())).Returns((SoundItem model)=>
        {
          SavedModels.Add(model);
          return EntityFailure==null?Task.FromResult(true):Task.FromException<bool>(EntityFailure);
        });
        previous=VSynchronizationContext.UISynchronizationContext;
        VSynchronizationContext.UISynchronizationContext=Ui;
      }
      internal SoundItemInPlaylistViewModel Current {set=>actual.SetValue(Player,value);}
      internal void BeginDisposal()=>Field(Player.GetType(),"isDisposing").SetValue(Player,true);
      internal Task Save(SoundItemInPlaylistViewModel captured)=>(Task)SaveMethod(Player.GetType()).Invoke(Player,new object[]{captured});
      public void Dispose(){VSynchronizationContext.UISynchronizationContext=previous;semaphore.Dispose();Original.Dispose();Replacement.Dispose();}
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ClearOrSwitchDuringPlaylistSavePersistsCapturedTrackAndSkipsStaleUi(bool replace)
    {
      using var f=new Fixture();
      f.AfterPlaylistSave=()=>f.Current=replace?f.Replacement:null;
      await f.Save(f.Original);
      Assert.Same(f.Original.Model,Assert.Single(f.SavedModels));
      f.Ui.Drain();
      Assert.Equal(0,f.OriginalNotifications);
      Assert.Equal(0,f.ReplacementNotifications);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task QueuedUiCallbackDoesNotDereferenceClearedOrReplacementTrack(bool replace)
    {
      using var f=new Fixture();
      await f.Save(f.Original);
      Assert.Same(f.Original.Model,Assert.Single(f.SavedModels));
      Assert.Single(f.Ui.Actions);
      f.Current=replace?f.Replacement:null;
      f.Ui.Drain();
      Assert.Equal(0,f.OriginalNotifications);
      Assert.Equal(0,f.ReplacementNotifications);
    }

    [Fact]
    public async Task DelayedSaveForPreviousTrackDoesNotSaveReplacement()
    {
      using var f=new Fixture();
      f.Current=f.Replacement;
      await f.Save(f.Original);
      Assert.Empty(f.SavedModels);
      Assert.Empty(f.Ui.Actions);
    }

    [Fact]
    public async Task DisposalBeforeDelayedSaveSkipsStorageAndUi()
    {
      using var f=new Fixture();
      f.BeginDisposal();
      await f.Save(f.Original);
      Assert.Empty(f.SavedModels);
      Assert.Empty(f.Ui.Actions);
    }

    [Fact]
    public async Task DisposalAfterStorageSkipsQueuedUiNotification()
    {
      using var f=new Fixture();
      await f.Save(f.Original);
      Assert.Same(f.Original.Model,Assert.Single(f.SavedModels));
      f.BeginDisposal();
      f.Ui.Drain();
      Assert.Equal(0,f.OriginalNotifications);
    }

    [Fact]
    public async Task CurrentTrackProgressPersistsAndNotifiesOnce()
    {
      using var f=new Fixture();
      await f.Save(f.Original);
      Assert.Same(f.Original.Model,Assert.Single(f.SavedModels));
      f.Ui.Drain();
      Assert.Equal(1,f.OriginalNotifications);
      Assert.Equal(0,f.ReplacementNotifications);
    }

    [Fact]
    public async Task BackgroundStorageFailureIsObservedAndLogged()
    {
      using var f=new Fixture();
      var failure=new InvalidOperationException("Injected progress write failure");
      f.EntityFailure=failure;
      await f.Save(f.Original);
      Assert.Empty(f.Ui.Actions);
      f.Logger.Verify(x=>x.Log(failure),Times.Once);
    }
  }
}