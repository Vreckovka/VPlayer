using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using VCore.WPF;
using VCore.WPF.Modularity.RegionProviders;
using VPlayer.AudioStorage.AudioDatabase;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.DomainClasses.IPTV;
using VPlayer.AudioStorage.DomainClasses.Video;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Home.ViewModels.Statistics;
using Xunit;

namespace VPlayer.Tests
{
  [Collection("UI synchronization")]
  public class StatisticsTests
  {
    private sealed class Context : AudioDatabaseContext
    {
      private readonly SqliteConnection connection;
      public Context(SqliteConnection connection) => this.connection=connection;
      protected override void OnConfiguring(DbContextOptionsBuilder builder) => builder.UseSqlite(connection);
    }
    private sealed class Fixture : IDisposable
    {
      private readonly SqliteConnection connection=new SqliteConnection("Data Source=:memory:");
      public Context Database {get;}
      public StatisticsViewModel View {get;}
      public Mock<IStorageManager> Storage {get;}=new Mock<IStorageManager>();
      public Fixture()
      {
        connection.Open();
        Database=new Context(connection);
        Database.Database.EnsureCreated();
        var storage=Storage;
        storage.Setup(x=>x.GetTempRepository<SoundItem>()).Returns(Database.Set<SoundItem>().AsNoTracking());
        storage.Setup(x=>x.GetTempRepository<VideoItem>()).Returns(Database.Set<VideoItem>().AsNoTracking());
        storage.Setup(x=>x.GetTempRepository<TvShowEpisode>()).Returns(Database.Set<TvShowEpisode>().AsNoTracking());
        storage.Setup(x=>x.GetTempRepository<Song>()).Returns(Database.Set<Song>().AsNoTracking());
        storage.Setup(x=>x.GetTempRepository<SoundItemFilePlaylist>()).Returns(Database.Set<SoundItemFilePlaylist>().AsNoTracking());
        storage.Setup(x=>x.GetTempRepository<VideoFilePlaylist>()).Returns(Database.Set<VideoFilePlaylist>().AsNoTracking());
        storage.Setup(x=>x.GetTempRepository<TvPlaylist>()).Returns(Database.Set<TvPlaylist>().AsNoTracking());
        View=new StatisticsViewModel(new Mock<IRegionProvider>().Object,storage.Object);
      }
      public Task StartLoad() => (Task)typeof(StatisticsViewModel).GetMethod("LoadData",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(View,null);
      public async Task Load()
      {
        await StartLoad();
        await Dispatcher.CurrentDispatcher.InvokeAsync(()=>{},DispatcherPriority.ContextIdle);
      }
      public void Dispose() {View.Dispose();Database.Dispose();connection.Dispose();}
    }
    private static void WithDispatcher(Func<Task> action)
    {
      Sta.Run(() =>
      {
        var dispatcher=Dispatcher.CurrentDispatcher;
        var oldContext=VSynchronizationContext.UISynchronizationContext;
        var oldDispatcher=VSynchronizationContext.UIDispatcher;
        var oldThreadContext=SynchronizationContext.Current;
        try
        {
          var synchronization=new DispatcherSynchronizationContext(dispatcher);
          VSynchronizationContext.UISynchronizationContext=synchronization;
          VSynchronizationContext.UIDispatcher=dispatcher;
          SynchronizationContext.SetSynchronizationContext(synchronization);
          var frame=new DispatcherFrame();
          var task=action();
          task.ContinueWith(_=>dispatcher.BeginInvoke(new Action(()=>frame.Continue=false)),TaskScheduler.Default);
          Dispatcher.PushFrame(frame);
          task.GetAwaiter().GetResult();
        }
        finally
        {
          VSynchronizationContext.UISynchronizationContext=oldContext;
          VSynchronizationContext.UIDispatcher=oldDispatcher;
          SynchronizationContext.SetSynchronizationContext(oldThreadContext);
          dispatcher.InvokeShutdown();
        }
      },120);
    }

    private static void SeedReloadStress(Fixture fixture)
    {
      const int count=10000;
      fixture.Database.AddRange(Enumerable.Range(1,count).Select(i=>new SoundItem
      {
        Id=i,TimePlayed=TimeSpan.FromTicks(i),FileInfoEntity=new FileInfoEntity {Title="Stress sound "+i}
      }));
      fixture.Database.AddRange(Enumerable.Range(1,count).Select(i=>new SoundItemFilePlaylist
      {
        Id=i,HashCode=i,Name="Stress playlist "+i,TotalPlayedTime=TimeSpan.FromTicks(i*2L)
      }));
      fixture.Database.SaveChanges();
    }

    [Fact]
    public void RapidReloadRequestsShareOneDatabaseLoadUntilUiPublicationCompletes()
    {
      WithDispatcher(async () =>
      {
        using var fixture=new Fixture();
        SeedReloadStress(fixture);
        var entered=new TaskCompletionSource<bool>();
        using var release=new ManualResetEventSlim();
        int reads=0;
        fixture.Storage.Setup(x=>x.GetTempRepository<SoundItem>()).Returns(() =>
        {
          Interlocked.Increment(ref reads);
          entered.TrySetResult(true);
          if(!release.Wait(TimeSpan.FromSeconds(15))) throw new TimeoutException("Test load was not released.");
          return fixture.Database.SoundItems.AsNoTracking();
        });
        var requests=Enumerable.Range(0,32).Select(_=>fixture.StartLoad()).ToArray();
        try
        {
          Assert.Same(entered.Task,await Task.WhenAny(entered.Task,Task.Delay(TimeSpan.FromSeconds(10))));
          Assert.True(fixture.View.LoadingStatus.IsLoading);
          Assert.All(requests,request=>Assert.Same(requests[0],request));
          Assert.Equal(1,Volatile.Read(ref reads));
        }
        finally
        {
          release.Set();
          // Drain old-implementation requests even when the regression fails.
          try {await Task.WhenAll(requests);} catch { }
        }
        Assert.Equal(30,fixture.View.ItemsView.Count);
        Assert.Equal(30,fixture.View.PlaylistView.Count);
        Assert.False(fixture.View.LoadingStatus.IsLoading);
        Assert.Equal(1,reads);
      });
    }

    [Fact]
    public void ReentrantLoadingNotificationsDoNotStartAdditionalScans()
    {
      WithDispatcher(async () =>
      {
        using var fixture=new Fixture();
        SeedReloadStress(fixture);
        var reentered=new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        Task[] nested=null;
        fixture.View.LoadingStatus.PropertyChanged+=(sender,args)=>
        {
          if(args.PropertyName=="IsLoading" && fixture.View.LoadingStatus.IsLoading && nested==null)
          {
            nested=Array.Empty<Task>();
            nested=Enumerable.Range(0,32).Select(_=>fixture.StartLoad()).ToArray();
            reentered.TrySetResult(true);
          }
        };
        var original=fixture.StartLoad();
        try
        {
          Assert.Same(reentered.Task,await Task.WhenAny(reentered.Task,Task.Delay(TimeSpan.FromSeconds(10))));
          Assert.All(nested,request=>Assert.Same(original,request));
        }
        finally
        {
          try {await Task.WhenAll((nested ?? Array.Empty<Task>()).Concat(new[] {original}));} catch { }
        }
        Assert.NotNull(fixture.View.ItemsView);
        Assert.NotNull(fixture.View.PlaylistView);
        Assert.False(fixture.View.LoadingStatus.IsLoading);
      });
    }

    [Fact]
    public void CompletingLoadMeansRowsAndTotalsHaveAlreadyBeenPublished()
    {
      WithDispatcher(async () =>
      {
        using var fixture=new Fixture();
        SeedReloadStress(fixture);
        var previous=VSynchronizationContext.UISynchronizationContext;
        var held=new System.Collections.Concurrent.ConcurrentQueue<Action>();
        var synchronization=new Mock<SynchronizationContext>();
        synchronization.Setup(x=>x.Post(It.IsAny<SendOrPostCallback>(),It.IsAny<object>()))
          .Callback((SendOrPostCallback callback,object state)=>held.Enqueue(()=>callback(state)));
        VSynchronizationContext.UISynchronizationContext=synchronization.Object;
        try
        {
          await fixture.StartLoad();
          Assert.NotNull(fixture.View.ItemsView);
          Assert.NotNull(fixture.View.PlaylistView);
          Assert.Equal(30,fixture.View.ItemsView.Count);
          Assert.Equal(30,fixture.View.PlaylistView.Count);
          Assert.Equal(TimeSpan.FromTicks(50005000),fixture.View.TotalWatchedItems);
          Assert.Equal(fixture.View.TotalWatchedItems,fixture.View.TotalWatched);
          Assert.False(fixture.View.LoadingStatus.IsLoading);
        }
        finally
        {
          VSynchronizationContext.UISynchronizationContext=previous;
          while(held.TryDequeue(out var publish)) publish();
        }
      });
    }

    [Fact]
    public void FailedPlaylistReloadKeepsThePreviousSnapshotAndCanBeRetried()
    {
      WithDispatcher(async () =>
      {
        using var fixture=new Fixture();
        SeedReloadStress(fixture);
        await fixture.Load();
        var originalItems=fixture.View.ItemsView;
        var originalPlaylists=fixture.View.PlaylistView;
        var originalTotal=fixture.View.TotalWatchedItems;
        var originalPlaylistTotal=fixture.View.TotalWatched;
        fixture.Database.SoundItems.Find(10000).TimePlayed=TimeSpan.FromDays(100);
        fixture.Database.SaveChanges();
        fixture.Storage.Setup(x=>x.GetTempRepository<SoundItemFilePlaylist>()).Throws(new InvalidOperationException("Playlist read failed."));
        await Assert.ThrowsAsync<InvalidOperationException>(()=>fixture.Load());
        Assert.Same(originalItems,fixture.View.ItemsView);
        Assert.Same(originalPlaylists,fixture.View.PlaylistView);
        Assert.Equal(originalTotal,fixture.View.TotalWatchedItems);
        Assert.Equal(originalPlaylistTotal,fixture.View.TotalWatched);
        Assert.False(fixture.View.LoadingStatus.IsLoading);
        fixture.Storage.Setup(x=>x.GetTempRepository<SoundItemFilePlaylist>()).Returns(fixture.Database.SoundItemPlaylists.AsNoTracking());
        await fixture.Load();
        Assert.NotSame(originalItems,fixture.View.ItemsView);
        Assert.True(fixture.View.TotalWatchedItems>originalTotal);
        Assert.Equal(10000,fixture.View.ItemsView[0].Id);
        Assert.False(fixture.View.LoadingStatus.IsLoading);
      });
    }

    [Fact]
    public void HighestRankedRowsAreKeptAndSharedIdsDoNotReplaceOtherMediaTypes()
    {
      WithDispatcher(async () =>
      {
        using var fixture=new Fixture();
        var sound=new SoundItem {Id=1,TimePlayed=TimeSpan.FromDays(3),FileInfoEntity=new FileInfoEntity {Title="Sound"}};
        var video=new VideoItem {Id=1,Name="Video",TimePlayed=TimeSpan.FromDays(5),FileInfoEntity=new FileInfoEntity {Title="Video"}};
        var episode=new TvShowEpisode {Id=1,TimePlayed=TimeSpan.FromDays(4),VideoItem=new VideoItem {Id=2,Name="Episode",IsPrivate=true,FileInfoEntity=new FileInfoEntity {Title="Episode"}}};
        fixture.Database.AddRange(sound,video,episode,new Song {Id=777,ItemModel=sound},
          new SoundItemFilePlaylist {Id=1,Name="Playlist",TotalPlayedTime=TimeSpan.FromDays(20)});
        fixture.Database.SaveChanges();
        await fixture.Load();
        var items=fixture.View.ItemsView.ToArray();
        Assert.Equal(3,items.Length);
        Assert.IsType<VideoItem>(items[0]);
        Assert.IsType<TvShowEpisode>(items[1]);
        Assert.Equal(777,Assert.IsType<Song>(items[2]).Id);
        Assert.Equal("Video",((VideoItem)items[0]).Name);
        Assert.Equal("Episode",((TvShowEpisode)items[1]).Name);
        Assert.Equal(777,Assert.IsType<Song>(Assert.Single(fixture.View.SoundsItemsView)).Id);
        Assert.IsType<VideoItem>(Assert.Single(fixture.View.VideosItemsView));
        Assert.Equal("Playlist",Assert.Single(fixture.View.PlaylistView).Name);
        Assert.Equal(TimeSpan.FromDays(12),fixture.View.TotalWatchedItems);
        Assert.Equal(TimeSpan.FromDays(8),fixture.View.TotalWatched);
      });
    }

    [Fact]
    public void TenThousandUniqueFilesAndPlaylistsKeepLimitsTotalsAndPrivateFiltering()
    {
      WithDispatcher(async () =>
      {
        using var fixture=new Fixture();
        const int count=10000;
        fixture.Database.AddRange(Enumerable.Range(1,count).Select(i=>new SoundItem
        {
          Id=i,TimePlayed=TimeSpan.FromTicks(i),FileInfoEntity=new FileInfoEntity {Title="Sound "+i}
        }));
        fixture.Database.AddRange(Enumerable.Range(1,count).Select(i=>new SoundItemFilePlaylist
        {
          Id=i,HashCode=i,Name="Playlist "+i,TotalPlayedTime=TimeSpan.FromTicks(i*2L)
        }));
        fixture.Database.AddRange(new SoundItem {Id=count+1,IsPrivate=true,TimePlayed=TimeSpan.FromDays(100)},
          new SoundItemFilePlaylist {Id=count+1,IsPrivate=true,TotalPlayedTime=TimeSpan.FromDays(100)});
        fixture.Database.SaveChanges();
        int owner=Thread.CurrentThread.ManagedThreadId,wrongThread=0;
        fixture.View.PropertyChanged+=(sender,args)=> {if(Thread.CurrentThread.ManagedThreadId!=owner) Interlocked.Increment(ref wrongThread);};
        await fixture.Load();
        Assert.Equal(Enumerable.Range(count-29,30).Reverse(),fixture.View.ItemsView.Select(x=>x.Id));
        Assert.Equal(Enumerable.Range(count-14,15).Reverse(),fixture.View.SoundsItemsView.Select(x=>x.Id));
        Assert.Empty(fixture.View.VideosItemsView);
        Assert.Equal(Enumerable.Range(count-29,30).Reverse(),fixture.View.PlaylistView.Select(x=>x.Id));
        Assert.Equal(TimeSpan.FromTicks(count*(count+1L)/2),fixture.View.TotalWatchedItems);
        Assert.Equal(fixture.View.TotalWatchedItems,fixture.View.TotalWatched);
        Assert.All(fixture.View.ItemsView,x=>Assert.False(((SoundItem)x).IsPrivate));
        Assert.Equal(0,wrongThread);
        Assert.False(fixture.View.LoadingStatus.IsLoading);
      });
    }

    [Fact]
    public void EqualTimesUseStableTypeThenIdOrderWithoutRequiringMetadata()
    {
      WithDispatcher(async () =>
      {
        using var fixture=new Fixture();
        var time=TimeSpan.FromDays(100)+TimeSpan.FromTicks(123);
        fixture.Database.AddRange(Enumerable.Range(1,40).Reverse().Select(i=>new SoundItem {Id=i,TimePlayed=time}));
        fixture.Database.AddRange(Enumerable.Range(1,40).Reverse().Select(i=>new VideoItem {Id=i,TimePlayed=time}));
        fixture.Database.AddRange(Enumerable.Range(1,40).Reverse().Select(i=>new TvShowEpisode {Id=i,TimePlayed=time}));
        fixture.Database.AddRange(Enumerable.Range(1,40).Reverse().Select(i=>new SoundItemFilePlaylist {Id=i,HashCode=i,TotalPlayedTime=time}));
        fixture.Database.AddRange(Enumerable.Range(1,40).Reverse().Select(i=>new VideoFilePlaylist {Id=i,HashCode=i,TotalPlayedTime=time}));
        fixture.Database.AddRange(Enumerable.Range(1,40).Reverse().Select(i=>new TvPlaylist {Id=i,TotalPlayedTime=time}));
        fixture.Database.SaveChanges();
        await fixture.Load();
        Assert.Equal(Enumerable.Range(1,30),fixture.View.ItemsView.Select(x=>x.Id));
        Assert.All(fixture.View.ItemsView,x=>Assert.IsType<SoundItem>(x));
        Assert.Equal(Enumerable.Range(1,15),fixture.View.SoundsItemsView.Select(x=>x.Id));
        Assert.Equal(Enumerable.Range(1,15),fixture.View.VideosItemsView.Select(x=>x.Id));
        Assert.All(fixture.View.VideosItemsView,x=>Assert.IsType<VideoItem>(x));
        Assert.Equal(Enumerable.Range(1,30),fixture.View.PlaylistView.Select(x=>x.Id));
        Assert.All(fixture.View.PlaylistView,x=>Assert.IsType<SoundItemFilePlaylist>(x));
        Assert.Equal(new TimeSpan(time.Ticks*120),fixture.View.TotalWatchedItems);
        Assert.Equal(TimeSpan.Zero,fixture.View.TotalWatched);
      });
    }

    [Fact]
    public void ReloadReplacesRowsAndPrivatePlaylistHistoryIsExcluded()
    {
      WithDispatcher(async () =>
      {
        using var fixture=new Fixture();
        var sound=new SoundItem {TimePlayed=TimeSpan.FromDays(2)};
        var playlist=new VideoFilePlaylist {Name="Visible",TotalPlayedTime=TimeSpan.FromDays(3)};
        fixture.Database.AddRange(sound,playlist,
          new TvPlaylist {IsPrivate=true,TotalPlayedTime=TimeSpan.FromDays(100)},
          new SoundItemFilePlaylist {IsPrivate=true,TotalPlayedTime=TimeSpan.FromDays(100)});
        fixture.Database.SaveChanges();
        await fixture.Load();
        Assert.Single(fixture.View.ItemsView);
        Assert.Equal("Visible",Assert.Single(fixture.View.PlaylistView).Name);
        Assert.Equal(TimeSpan.FromDays(1),fixture.View.TotalWatched);
        sound.IsPrivate=true;
        playlist.IsPrivate=true;
        fixture.Database.SaveChanges();
        await fixture.Load();
        Assert.Empty(fixture.View.ItemsView);
        Assert.Empty(fixture.View.SoundsItemsView);
        Assert.Empty(fixture.View.PlaylistView);
        Assert.Equal(TimeSpan.Zero,fixture.View.TotalWatchedItems);
        Assert.Equal(TimeSpan.Zero,fixture.View.TotalWatched);
      });
    }
    [Fact]
    public void LegacyZeroTimeStringsDoNotDuplicateSampledLeaderboardIds()
    {
      WithDispatcher(async () =>
      {
        using var fixture=new Fixture();
        fixture.Database.AddRange(Enumerable.Range(1,10000).Select(i=>new SoundItem {Id=i}));
        fixture.Database.SaveChanges();
        fixture.Database.Database.ExecuteSqlRaw("UPDATE SoundItems SET TimePlayed='00:00:00.0000000'");
        await fixture.Load();
        Assert.Equal(Enumerable.Range(1,30),fixture.View.ItemsView.Select(x=>x.Id));
        Assert.Equal(Enumerable.Range(1,15),fixture.View.SoundsItemsView.Select(x=>x.Id));
        Assert.Equal(TimeSpan.Zero,fixture.View.TotalWatchedItems);
      });
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LargeZeroAndNegativeTimeLibrariesPreserveRankingAndFullTotals(bool mixed)
    {
      WithDispatcher(async () =>
      {
        using var fixture=new Fixture();
        var sounds=Enumerable.Range(1,10000).Select(i=>new SoundItem
        {
          Id=i,IsPrivate=i==101,
          TimePlayed=!mixed || i<=40 || i%101==0 ? TimeSpan.Zero : TimeSpan.FromTicks(i>9995 ? i : -i)
        }).ToArray();
        fixture.Database.AddRange(sounds);
        fixture.Database.SaveChanges();
        await fixture.Load();
        var expected=sounds.Where(x=>!x.IsPrivate).OrderByDescending(x=>x.TimePlayed).ThenBy(x=>x.Id).ToArray();
        Assert.Equal(expected.Take(30).Select(x=>x.Id),fixture.View.ItemsView.Select(x=>x.Id));
        Assert.Equal(expected.Take(15).Select(x=>x.Id),fixture.View.SoundsItemsView.Select(x=>x.Id));
        Assert.Equal(new TimeSpan(expected.Sum(x=>x.TimePlayed.Ticks)),fixture.View.TotalWatchedItems);
        Assert.Empty(fixture.View.VideosItemsView);
      });
    }
    [Theory]
    [InlineData(0)]
    [InlineData(10000)]
    public void EmptyAndEntirelyPrivateLibrariesHaveNoPlaceholderRows(int count)
    {
      WithDispatcher(async () =>
      {
        using var fixture=new Fixture();
        fixture.Database.AddRange(Enumerable.Range(1,count).Select(i=>new SoundItem {Id=i,IsPrivate=true,TimePlayed=TimeSpan.FromDays(2)}));
        fixture.Database.SaveChanges();
        await fixture.Load();
        Assert.Empty(fixture.View.ItemsView);
        Assert.Empty(fixture.View.SoundsItemsView);
        Assert.Empty(fixture.View.VideosItemsView);
        Assert.Empty(fixture.View.PlaylistView);
        Assert.Equal(TimeSpan.Zero,fixture.View.TotalWatchedItems);
        Assert.Equal(TimeSpan.Zero,fixture.View.TotalWatched);
      });
    }
  }
}
