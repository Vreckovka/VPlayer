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
