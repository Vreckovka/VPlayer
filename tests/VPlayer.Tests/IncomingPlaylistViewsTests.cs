using System;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.Serialization;
using Logger;
using Moq;
using Prism.Events;
using VCore.Standard.Factories.ViewModels;
using VCore.WPF.Interfaces.Managers;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.InfoDownloader;
using VPlayer.AudioStorage.InfoDownloader.Clients.MusixMatch;
using VPlayer.AudioStorage.InfoDownloader.Clients.PCloud;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Events;
using VPlayer.Core.Interfaces.ViewModels;
using VPlayer.Core.ViewModels.SoundItems;
using VPlayer.WindowsPlayer.ViewModels;
using Xunit;

namespace VPlayer.Tests
{
  public class IncomingPlaylistViewsTests
  {
    private sealed class Harness : IDisposable
    {
      internal readonly MusicPlayerViewModel Player=(MusicPlayerViewModel)FormatterServices.GetUninitializedObject(typeof(MusicPlayerViewModel));
      internal readonly Mock<IViewModelsFactory> Factory=new Mock<IViewModelsFactory>(MockBehavior.Strict);
      internal readonly SoundItemInPlaylistViewModel[] Incoming;
      private readonly EventAggregator events=new EventAggregator();
      private readonly IStorageManager storage=new Mock<IStorageManager>().Object;
      private readonly IAlbumsViewModel albums=new Mock<IAlbumsViewModel>().Object;
      private readonly IArtistsViewModel artists=new Mock<IArtistsViewModel>().Object;
      private readonly ILogger logger=new Mock<ILogger>().Object;
      private readonly IWindowManager windows=new Mock<IWindowManager>().Object;
      private readonly AudioInfoDownloader downloader=Empty<AudioInfoDownloader>();
      private readonly PCloudLyricsProvider cloud=Empty<PCloudLyricsProvider>();
      private readonly MusixMatchLyricsProvider lyrics=Empty<MusixMatchLyricsProvider>();
      internal Harness()
      {
        Type type=Player.GetType();
        FieldInfo field=null;
        while(type!=null && field==null)
        {
          field=type.GetField("viewModelsFactory",BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.DeclaredOnly);
          type=type.BaseType;
        }
        if(field==null) throw new InvalidOperationException("Missing factory");
        field.SetValue(Player,Factory.Object);
        Incoming=Enumerable.Range(0,100000).Select(i=>new SoundItemInPlaylistViewModel(new SoundItem
          {Id=i%97,IsFavorite=i%2==0,Source=i%3==0?"https://example.invalid/audio":"local",Duration=200+i%13},events,storage)
          {ActualPosition=i%200,IsPlaying=i==99999,IsSelected=i%5==0}).ToArray();
      }
      internal SongInPlayListViewModel Create(Song song)=>new SongInPlayListViewModel(events,albums,artists,downloader,
        cloud,song,logger,storage,windows,Factory.Object,lyrics);
      internal void Prepare(PlayItemsEventData<SoundItemInPlaylistViewModel> data)
      {
        try {Player.GetType().GetMethod("PrepareIncomingViews",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(Player,new object[] {data});}
        catch(TargetInvocationException ex) when(ex.InnerException!=null){ExceptionDispatchInfo.Capture(ex.InnerException).Throw();throw;}
      }
      public void Dispose(){foreach(var item in Incoming) item.Dispose();}
      private static T Empty<T>() where T:class => (T)FormatterServices.GetUninitializedObject(typeof(T));
    }

    [Theory]
    [InlineData(EventAction.PlayFromPlaylist)]
    [InlineData(EventAction.PlayFromPlaylistLast)]
    [InlineData(EventAction.InitSetPlaylist)]
    public void SavedPlaylistPreparationKeepsIncomingOccurrencesWithoutConstructingDiscardedViews(EventAction action)
    {
      using var h=new Harness();
      var saved=new SoundItemFilePlaylist {Id=658,PlaylistItems=h.Incoming.Select((x,i)=>new PlaylistSoundItem
        {Id=i+1,OrderInPlaylist=i+1,ReferencedItem=x.Model}).ToList()};
      var data=new PlayItemsEventData<SoundItemInPlaylistViewModel>(h.Incoming,action,saved);
      h.Prepare(data);
      Assert.Same(h.Incoming,data.Items);
      var result=data.Items.ToArray();
      Assert.Equal(100000,result.Length);
      for(int i=0;i<result.Length;i++) Assert.Same(h.Incoming[i],result[i]);
      Assert.Same(saved,data.Model);
      h.Factory.Verify(x=>x.Create<SongInPlayListViewModel>(It.IsAny<object[]>()),Times.Never());
    }

    [Theory]
    [InlineData(EventAction.Play)]
    [InlineData(EventAction.Add)]
    public void OrdinaryPlayAndAddStillCreateSongViewsAndKeepAllTrackState(EventAction action)
    {
      using var h=new Harness();
      h.Factory.Setup(x=>x.Create<SongInPlayListViewModel>(It.IsAny<object[]>()))
        .Returns((object[] args)=>h.Create((Song)args[0]));
      // A playlist-shaped model alone must not skip conversion for Play/Add.
      var data=new PlayItemsEventData<SoundItemInPlaylistViewModel>(h.Incoming,action,new SoundItemFilePlaylist {Id=658});
      h.Prepare(data);
      var result=data.Items.Cast<SongInPlayListViewModel>().ToArray();
      try
      {
        Assert.Equal(100000,result.Length);
        Assert.Equal(100000,result.Distinct().Count());
        for(int i=0;i<result.Length;i++)
        {
          var incoming=h.Incoming[i];
          Assert.NotSame(incoming,result[i]);
          Assert.Same(incoming.Model,result[i].Model);
          Assert.Same(incoming.Model,result[i].SongModel.ItemModel);
          Assert.Equal(incoming.ActualPosition,result[i].ActualPosition);
          Assert.Equal(incoming.IsFavorite,result[i].IsFavorite);
          Assert.Equal(incoming.IsPlaying,result[i].IsPlaying);
          Assert.Equal(incoming.IsSelected,result[i].IsSelected);
          Assert.Equal(incoming.Duration,result[i].Duration);
        }
        h.Factory.Verify(x=>x.Create<SongInPlayListViewModel>(It.IsAny<object[]>()),Times.Exactly(100000));
      }
      finally {foreach(var item in result) item.Dispose();}
    }
  }
}