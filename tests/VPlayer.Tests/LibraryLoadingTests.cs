using System.Collections.Generic;
using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Logger;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using VCore;
using VCore.WPF;
using VCore.Standard.Factories.ViewModels;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.ViewModels.Artists;
using VPlayer.Home.ViewModels.LibraryViewModels;
using Xunit;
using Model=VPlayer.Tests.LibraryFilterTests.Model;

namespace VPlayer.Tests
{
  [CollectionDefinition("UI synchronization",DisableParallelization=true)]
  public class UiSynchronizationCollection {}

  [Collection("UI synchronization")]
  public class LibraryLoadingTests
  {
    private class Context : DbContext
    {
      public Context(DbContextOptions options) : base(options) {}
      protected override void OnModelCreating(ModelBuilder builder)=>builder.Entity<Model>().HasKey(x=>x.Id);
    }
    private sealed class ReaderAudit : DbCommandInterceptor
    {
      public List<int> ReadCounts {get;}=new List<int>();
      public bool FailNextRead {get;set;}
      public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,CommandEventData data,InterceptionResult<DbDataReader> result,CancellationToken cancellationToken=default)
      {
        if(FailNextRead) {FailNextRead=false;throw new InvalidOperationException("Simulated database read failure");}
        return new ValueTask<InterceptionResult<DbDataReader>>(result);
      }
      public override InterceptionResult DataReaderDisposing(DbCommand command,DataReaderDisposingEventData data,InterceptionResult result)
      {
        ReadCounts.Add(data.ReadCount);
        return result;
      }
    }    internal static void WithDispatcher(Func<Task> action)
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
      });
    }

    [Fact]
    public void ConcurrentLoadsPublishOnUiThreadAndCreateEachViewModelOnce()
    {
      WithDispatcher(async () =>
      {
        using var connection=new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using var context=new Context(new DbContextOptionsBuilder().UseSqlite(connection).Options);
        context.Database.EnsureCreated();
        context.AddRange(new Model {Id=1,Name="Alpha"},new Model {Id=2,Name="Beta"});
        context.SaveChanges();
        var storage=new Mock<IStorageManager>();
        storage.Setup(x=>x.GetTempRepository<Model>()).Returns(context.Set<Model>().AsNoTracking());
        var factory=new Mock<IViewModelsFactory>();
        factory.Setup(x=>x.Create<INamedEntityViewModel<Model>>(It.IsAny<object[]>()))
          .Returns((object[] args)=>
          {
            var model=(Model)args[0];
            var vm=new Mock<INamedEntityViewModel<Model>>();
            vm.SetupGet(x=>x.ModelId).Returns(model.Id);
            vm.SetupGet(x=>x.Name).Returns(model.Name);
            return vm.Object;
          });
        var library=new LibraryCollection<INamedEntityViewModel<Model>,Model>(factory.Object,storage.Object,new Mock<ILogger>().Object);
        int owner=Thread.CurrentThread.ManagedThreadId,callbacks=0,wrongThreadNotifications=0;
        library.PropertyChanged+=(sender,args)=> {if(Thread.CurrentThread.ManagedThreadId!=owner) Interlocked.Increment(ref wrongThreadNotifications);};
        library.DataLoadedCallback=()=>Interlocked.Increment(ref callbacks);
        var results=await Task.WhenAll(library.LoadInitilizedDataAsync().ToTask(),library.LoadInitilizedDataAsync().ToTask());
        Assert.All(results,Assert.True);
        Assert.True(library.WasLoaded);
        Assert.Equal(2,library.Items.Count);
        Assert.Equal(0,wrongThreadNotifications);
        Assert.Equal(1,callbacks);
        factory.Verify(x=>x.Create<INamedEntityViewModel<Model>>(It.IsAny<object[]>()),Times.Exactly(2));
        library.LoadQuery=Array.Empty<Model>().AsQueryable();
        Assert.True(await library.GetOrLoadDataAsync());
      });
    }

    [Fact]
    public void DatabaseSetupIsDeferredToLoadingWorkerAndClearDoesNotReopenIt()
    {
      WithDispatcher(async () =>
      {
        using var connection=new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using var context=new Context(new DbContextOptionsBuilder().UseSqlite(connection).Options);
        context.Database.EnsureCreated();
        var storage=new Mock<IStorageManager>();
        int creates=0,creationThread=0;
        storage.Setup(x=>x.GetTempRepository<Model>()).Returns(() =>
        {
          Interlocked.Increment(ref creates);
          creationThread=Thread.CurrentThread.ManagedThreadId;
          return context.Set<Model>().AsNoTracking();
        });
        var library=new LibraryCollection<INamedEntityViewModel<Model>,Model>(
          new Mock<IViewModelsFactory>().Object,storage.Object,new Mock<ILogger>().Object);
        int ownerThread=Thread.CurrentThread.ManagedThreadId;
        Assert.Equal(0,creates);
        Assert.True(await library.GetOrLoadDataAsync());
        Assert.Equal(1,creates);
        Assert.NotEqual(ownerThread,creationThread);
        library.Clear();
        Assert.Equal(1,creates);
        Assert.True(await library.GetOrLoadDataAsync());
        Assert.Equal(2,creates);
      });
    }
    [Fact]
    public void ConfiguredQueryIsDeferredAppliedOnceAndPreservedAfterClear()
    {
      WithDispatcher(async () =>
      {
        using var connection=new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using var context=new Context(new DbContextOptionsBuilder().UseSqlite(connection).Options);
        context.Database.EnsureCreated();
        context.AddRange(Enumerable.Range(1,10000).Select(i=>new Model {Id=i,Name=i%3000==0?"included":"excluded"}));
        context.SaveChanges();
        var storage=new Mock<IStorageManager>();
        int creates=0,configurations=0;
        storage.Setup(x=>x.GetTempRepository<Model>()).Returns(() =>
        {
          Interlocked.Increment(ref creates);
          return context.Set<Model>().AsNoTracking();
        });
        var factory=new Mock<IViewModelsFactory>();
        factory.Setup(x=>x.Create<INamedEntityViewModel<Model>>(It.IsAny<object[]>()))
          .Returns((object[] args)=>
          {
            var model=(Model)args[0];
            var view=new Mock<INamedEntityViewModel<Model>>();
            view.SetupGet(x=>x.ModelId).Returns(model.Id);
            view.SetupGet(x=>x.Name).Returns(model.Name);
            return view.Object;
          });
        var library=new LibraryCollection<INamedEntityViewModel<Model>,Model>(factory.Object,storage.Object,new Mock<ILogger>().Object);
        library.ConfigureQuery(query =>
        {
          Interlocked.Increment(ref configurations);
          return query.Where(x=>x.Name=="included").OrderByDescending(x=>x.Id).Take(2);
        });
        Assert.Equal(0,creates);
        Assert.Equal(0,configurations);
        var loads=await Task.WhenAll(Enumerable.Range(0,32).Select(_=>library.GetOrLoadDataAsync().ToTask()));
        Assert.All(loads,Assert.True);
        Assert.Equal(new[] {9000,6000},library.Items.Select(x=>x.ModelId));
        Assert.Equal(1,creates);
        Assert.Equal(1,configurations);
        library.Clear();
        Assert.Equal(1,creates);
        Assert.True(await library.GetOrLoadDataAsync());
        Assert.Equal(new[] {9000,6000},library.Items.Select(x=>x.ModelId));
        Assert.Equal(2,creates);
        Assert.Equal(2,configurations);
        factory.Verify(x=>x.Create<INamedEntityViewModel<Model>>(It.IsAny<object[]>()),Times.Exactly(4));
      });
    }
    [Fact]
    public void BoundedLookupSharesIdentityWithLaterFullLoadAndClearInvalidatesIt()
    {
      WithDispatcher(async () =>
      {
        using var connection=new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var audit=new ReaderAudit();
        using var context=new Context(new DbContextOptionsBuilder().UseSqlite(connection).AddInterceptors(audit).Options);
        context.Database.EnsureCreated();
        context.AddRange(Enumerable.Range(1,10000).Select(i=>new Model {Id=i,Name="Original "+i}));
        context.SaveChanges();
        var storage=new Mock<IStorageManager>();
        storage.Setup(x=>x.GetTempRepository<Model>()).Returns(context.Set<Model>().AsNoTracking());
        var factory=new Mock<IViewModelsFactory>();
        int owner=Thread.CurrentThread.ManagedThreadId,refreshThread=0;
        factory.Setup(x=>x.Create<INamedEntityViewModel<Model>>(It.IsAny<object[]>()))
          .Returns((object[] args)=>
          {
            var model=(Model)args[0];
            var view=new Mock<INamedEntityViewModel<Model>>();
            view.SetupGet(x=>x.ModelId).Returns(()=>model.Id);
            view.SetupGet(x=>x.Name).Returns(()=>model.Name);
            view.Setup(x=>x.RefreshModel(It.IsAny<Model>())).Callback<Model>(fresh=>
            {
              model=fresh;
              refreshThread=Thread.CurrentThread.ManagedThreadId;
            });
            return view.Object;
          });
        var library=new LibraryCollection<INamedEntityViewModel<Model>,Model>(factory.Object,storage.Object,new Mock<ILogger>().Object);
        int callbacks=0;
        library.DataLoadedCallback=()=>callbacks++;
        audit.ReadCounts.Clear();
        var lookups=await Task.WhenAll(Enumerable.Range(0,64).Select(_=>library.GetViewModelAsync(10000)));
        Assert.False(library.WasLoaded);
        Assert.InRange(audit.ReadCounts.Single(),1,3);
        Assert.All(lookups,x=>Assert.Same(lookups[0],x));
        Assert.Equal(10000,lookups[0].ModelId);
        Assert.Equal(0,callbacks);
        factory.Verify(x=>x.Create<INamedEntityViewModel<Model>>(It.IsAny<object[]>()),Times.Once);
        Assert.Null(await library.GetViewModelAsync(10001));
        context.Set<Model>().Single(x=>x.Id==10000).Name="Updated";
        context.SaveChanges();
        audit.FailNextRead=true;
        await library.RefreshCachedAsync(10000);
        Assert.StartsWith("Original ",lookups[0].Name);
        await library.RefreshCachedAsync(10000);
        Assert.Equal("Updated",lookups[0].Name);
        Assert.True(await library.GetOrLoadDataAsync());
        Assert.Equal(10000,library.Items.Count);
        Assert.Same(lookups[0],library.Items.Single(x=>x.ModelId==10000));
        Assert.Equal("Updated",lookups[0].Name);
        Assert.Equal(owner,refreshThread);
        Assert.Equal(1,callbacks);
        Assert.Same(lookups[0],await library.GetViewModelAsync(10000));
        // Simulate the UI synchronously waiting for song initialization.
        var offThreadLookup=Task.Run(()=>library.GetViewModelAsync(10000));
        Assert.True(offThreadLookup.Wait(TimeSpan.FromSeconds(2)));
        Assert.Same(lookups[0],offThreadLookup.Result);
        factory.Verify(x=>x.Create<INamedEntityViewModel<Model>>(It.IsAny<object[]>()),Times.Exactly(10000));
        library.Clear();
        var reloaded=await library.GetViewModelAsync(10000);
        Assert.False(library.WasLoaded);
       Assert.NotSame(lookups[0],reloaded);
        Assert.Equal("Updated",reloaded.Name);
        factory.Verify(x=>x.Create<INamedEntityViewModel<Model>>(It.IsAny<object[]>()),Times.Exactly(10001));
      });
    }

    [Fact]
    public void BoundedLookupHonorsConfiguredQueryAndDoesNotCacheMissingRows()
    {
      WithDispatcher(async () =>
      {
        using var connection=new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using var context=new Context(new DbContextOptionsBuilder().UseSqlite(connection).Options);
        context.Database.EnsureCreated();
        context.AddRange(Enumerable.Range(1,10000).Select(i=>new Model {Id=i,Name="excluded"}));
        context.SaveChanges();
        var storage=new Mock<IStorageManager>();
        storage.Setup(x=>x.GetTempRepository<Model>()).Returns(context.Set<Model>().AsNoTracking());
        var factory=new Mock<IViewModelsFactory>();
        factory.Setup(x=>x.Create<INamedEntityViewModel<Model>>(It.IsAny<object[]>()))
          .Returns((object[] args)=>
          {
            var model=(Model)args[0];
            var view=new Mock<INamedEntityViewModel<Model>>();
            view.SetupGet(x=>x.ModelId).Returns(model.Id);
            return view.Object;
          });
        var library=new LibraryCollection<INamedEntityViewModel<Model>,Model>(factory.Object,storage.Object,new Mock<ILogger>().Object);
        library.ConfigureQuery(query=>query.Where(x=>x.Name=="included"));
        Assert.Null(await library.GetViewModelAsync(10000));
        Assert.Null(await library.GetViewModelAsync(10001));
        context.Add(new Model {Id=10001,Name="included"});
        context.SaveChanges();
        Assert.Equal(10001,(await library.GetViewModelAsync(10001)).ModelId);
        Assert.False(library.WasLoaded);
        factory.Verify(x=>x.Create<INamedEntityViewModel<Model>>(It.IsAny<object[]>()),Times.Once);
      });
    }

    [Fact]
    public void ClearDuringLoadDoesNotPublishStaleData()
    {
      WithDispatcher(async () =>
      {
        using var connection=new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using var context=new Context(new DbContextOptionsBuilder().UseSqlite(connection).Options);
        context.Database.EnsureCreated();
        context.AddRange(Enumerable.Range(1,10000).Select(i=>new Model {Id=i,Name="Item "+i}));
        context.SaveChanges();
        using var entered=new ManualResetEventSlim();
        using var release=new ManualResetEventSlim();
        var storage=new Mock<IStorageManager>();
        storage.Setup(x=>x.GetTempRepository<Model>()).Returns(()=>
        {
          entered.Set();
          if(!release.Wait(TimeSpan.FromSeconds(10))) throw new TimeoutException();
          return context.Set<Model>().AsNoTracking();
        });
        var factory=new Mock<IViewModelsFactory>();
        factory.Setup(x=>x.Create<INamedEntityViewModel<Model>>(It.IsAny<object[]>()))
          .Returns((object[] args)=>
          {
            var model=(Model)args[0];
            var view=new Mock<INamedEntityViewModel<Model>>();
            view.SetupGet(x=>x.ModelId).Returns(model.Id);
            view.SetupGet(x=>x.Name).Returns(model.Name);
            return view.Object;
          });
        var library=new LibraryCollection<INamedEntityViewModel<Model>,Model>(factory.Object,storage.Object,new Mock<ILogger>().Object);
        int callbacks=0;
        library.DataLoadedCallback=()=>callbacks++;
        var load=library.GetOrLoadDataAsync().ToTask();
        try
        {
          Assert.True(await Task.Run(()=>entered.Wait(TimeSpan.FromSeconds(5))));
          library.Clear();
        }
        finally {release.Set();}
        Assert.False(await load);
        Assert.False(library.WasLoaded);
        Assert.Null(library.Items);
        Assert.Equal(0,callbacks);
        Assert.True(await library.GetOrLoadDataAsync());
        Assert.Equal(10000,library.Items.Count);
        Assert.Equal(1,callbacks);
        factory.Verify(x=>x.Create<INamedEntityViewModel<Model>>(It.IsAny<object[]>()),Times.Exactly(10000));
      });
    }
    [Fact]
    public void FailedLoadCanRetryWithoutDeadlocking()
    {
      WithDispatcher(async () =>
      {
        using var connection=new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using var context=new Context(new DbContextOptionsBuilder().UseSqlite(connection).Options);
        context.Database.EnsureCreated();
        var storage=new Mock<IStorageManager>();
        storage.Setup(x=>x.GetTempRepository<Model>()).Returns(Array.Empty<Model>().AsQueryable());
        var library=new LibraryCollection<INamedEntityViewModel<Model>,Model>(new Mock<IViewModelsFactory>().Object,storage.Object,new Mock<ILogger>().Object);
        Assert.False(await library.LoadInitilizedDataAsync());
        Assert.False(library.WasLoaded);
        library.LoadQuery=context.Set<Model>().AsNoTracking();
        Assert.True(await library.LoadInitilizedDataAsync());
        Assert.True(library.WasLoaded);
        Assert.Empty(library.Items);
      });
    }
  }
}
