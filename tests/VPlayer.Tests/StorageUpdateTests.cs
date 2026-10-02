using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using ChromeDriverScrapper;
using Logger;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using VPlayer.AudioStorage.AudioDatabase;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.InfoDownloader;
using VPlayer.AudioStorage.InfoDownloader.Clients.MusixMatch;
using VPlayer.AudioStorage.InfoDownloader.Clients.PCloud.Images;
using VPlayer.Core.Managers.Status;
using Xunit;

namespace VPlayer.Tests
{
  public class StorageUpdateTests
  {
    private sealed class Context : AudioDatabaseContext
    {
      private readonly SqliteConnection connection;
      private readonly Action beforeSave;
      private readonly Action disposed;
      public Context(SqliteConnection connection,Action beforeSave=null,Action disposed=null)
      {this.connection=connection;this.beforeSave=beforeSave;this.disposed=disposed;}
      protected override void OnConfiguring(DbContextOptionsBuilder builder) => builder.UseSqlite(connection);
      public override int SaveChanges() {beforeSave?.Invoke();return base.SaveChanges();}
      public override void Dispose() {try {base.Dispose();} finally {disposed?.Invoke();}}
    }
    private sealed class Storage : VPlayerStorageManager
    {
      public Func<AudioDatabaseContext> ContextFactory {get;set;}
      public Storage() : base(
        new AudioInfoDownloader(new Mock<ILogger>().Object,new Mock<IStatusManager>().Object,
          new Mock<IPCloudAlbumCoverProvider>().Object,
          new MusixMatchLyricsProvider(new Mock<IChromeDriverProvider>().Object,new Mock<ILogger>().Object)),
        new Mock<ILogger>().Object) {}
      protected override AudioDatabaseContext CreateEntityUpdateContext() => ContextFactory();
    }
    private sealed class Fixture : IDisposable
    {
      private readonly SqliteConnection connection=new SqliteConnection("Data Source=:memory:");
      public Storage Storage {get;}=new Storage();
      public Artist Original {get;}
      public Action BeforeSave {get;set;}
      private TaskCompletionSource<bool> disposed;
      public Fixture()
      {
        connection.Open();
        using var context=new Context(connection);
        context.Database.EnsureCreated();
        context.Artists.AddRange(Enumerable.Range(1,10000)
          .Select(i=>new Artist {Id=i,Name=new string('W',180)+" Artist "+i}));
        context.SaveChanges();
        Original=context.Artists.AsNoTracking().Single(x=>x.Id==10000);
        Storage.ContextFactory=()=>new Context(connection,BeforeSave,()=>disposed.TrySetResult(true));
      }
      public Task<bool> UpdateAsync(Artist artist)
      {
        disposed=new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        return Storage.UpdateEntityAsync(artist);
      }
      public Task WaitForDisposedAsync() => AwaitWithTimeout(disposed.Task);
      public Artist Read()
      {
        using var context=new Context(connection);
        return context.Artists.AsNoTracking().Single(x=>x.Id==Original.Id);
      }
      public void Dispose() {Storage.Dispose();connection.Dispose();}
    }
    private static async Task AwaitWithTimeout(Task task)
    {
      Assert.Same(task,await Task.WhenAny(task,Task.Delay(TimeSpan.FromSeconds(10))));
      await task;
    }

    [Fact]
    public async Task CompletionWaitsForCommittedWriteAndNotification()
    {
      using var fixture=new Fixture();
      using var release=new ManualResetEventSlim();
      var entered=new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
      fixture.BeforeSave=()=>
      {
        entered.TrySetResult(true);
        if(!release.Wait(TimeSpan.FromSeconds(10))) throw new TimeoutException("Save gate was not released.");
      };
      int notifications=0;
      Artist notified=null;
      using var subscription=fixture.Storage.ObserveOnItemChange<Artist>().Subscribe(change=>
      {
        notified=change.Item;
        Interlocked.Increment(ref notifications);
      });
      var replacement=new Artist {Id=fixture.Original.Id,Name="Changed "+new string('W',180)};
      var pending=fixture.UpdateAsync(replacement);
      try
      {
        await AwaitWithTimeout(entered.Task);
        Assert.False(pending.IsCompleted);
        Assert.Equal(0,Volatile.Read(ref notifications));
      }
      finally
      {
        release.Set();
        await fixture.WaitForDisposedAsync();
      }
      Assert.True(await pending);
      Assert.Equal(replacement.Name,fixture.Read().Name);
      Assert.Equal(1,notifications);
      Assert.Equal(replacement.Name,notified.Name);
    }

    [Fact]
    public async Task MissingEntityReportsFailureWithoutNotification()
    {
      using var fixture=new Fixture();
      int notifications=0;
      using var subscription=fixture.Storage.ObserveOnItemChange<Artist>().Subscribe(_=>notifications++);
      var pending=fixture.UpdateAsync(new Artist {Id=10001,Name="Missing"});
      await fixture.WaitForDisposedAsync();
      Assert.False(await pending);
      Assert.Equal(0,notifications);
      Assert.Equal(fixture.Original.Name,fixture.Read().Name);
    }

    [Fact]
    public async Task FailedSaveReportsFailureAndCanRetry()
    {
      using var fixture=new Fixture();
      int notifications=0;
      using var subscription=fixture.Storage.ObserveOnItemChange<Artist>().Subscribe(_=>notifications++);
      fixture.BeforeSave=()=>throw new InvalidOperationException("Simulated failed database write.");
      var replacement=new Artist {Id=fixture.Original.Id,Name="Changed "+new string('W',180)};
      var failed=fixture.UpdateAsync(replacement);
      await fixture.WaitForDisposedAsync();
      Assert.False(await failed);
      Assert.Equal(0,notifications);
      Assert.Equal(fixture.Original.Name,fixture.Read().Name);
      fixture.BeforeSave=null;
      var retry=fixture.UpdateAsync(replacement);
      await fixture.WaitForDisposedAsync();
      Assert.True(await retry);
      Assert.Equal(1,notifications);
      Assert.Equal(replacement.Name,fixture.Read().Name);
    }

    [Fact]
    public async Task UnchangedEntityReportsNoWriteAndNoNotification()
    {
      using var fixture=new Fixture();
      int notifications=0;
      using var subscription=fixture.Storage.ObserveOnItemChange<Artist>().Subscribe(_=>notifications++);
      var pending=fixture.UpdateAsync(fixture.Original);
      await fixture.WaitForDisposedAsync();
      Assert.False(await pending);
      Assert.Equal(0,notifications);
      Assert.Equal(fixture.Original.Name,fixture.Read().Name);
    }
  }
}