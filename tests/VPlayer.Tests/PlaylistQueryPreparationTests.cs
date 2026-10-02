using System;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using VPlayer.AudioStorage.AudioDatabase;
using VPlayer.AudioStorage.DomainClasses;
using Xunit;

namespace VPlayer.Tests
{
  [Collection("UI synchronization")]
  public class PlaylistQueryPreparationTests
  {
    private sealed class Audit : DbConnectionInterceptor
    {
      public int Opens;
      public override InterceptionResult ConnectionOpening(DbConnection connection,ConnectionEventData data,InterceptionResult result)
      {
        Interlocked.Increment(ref Opens);
        return result;
      }
      public override ValueTask<InterceptionResult> ConnectionOpeningAsync(DbConnection connection,ConnectionEventData data,
        InterceptionResult result,CancellationToken cancellationToken=default)
      {
        Interlocked.Increment(ref Opens);
        return new ValueTask<InterceptionResult>(result);
      }
    }
    private class Context : AudioDatabaseContext
    {
      private readonly SqliteConnection connection;
      private readonly Audit audit;
      private readonly Action<string> log;
      public bool Disposed {get;private set;}
      public Context(SqliteConnection connection,Audit audit,Action<string> log)
      {this.connection=connection;this.audit=audit;this.log=log;}
      protected override void OnConfiguring(DbContextOptionsBuilder builder) =>
        builder.UseSqlite(connection).AddInterceptors(audit)
          .LogTo(log,new[] {CoreEventId.QueryCompilationStarting},LogLevel.Debug);
      public override void Dispose(){Disposed=true;base.Dispose();}
    }
    private sealed class RetryContext : Context
    {
      public RetryContext(SqliteConnection connection,Audit audit):base(connection,audit,_=>{}) {}
    }
    private sealed class BrokenContext : AudioDatabaseContext
    {
      public bool Disposed {get;private set;}
      protected override void OnConfiguring(DbContextOptionsBuilder builder) => builder.UseSqlite("Data Source=:memory:");
      protected override void OnModelCreating(ModelBuilder builder)=>throw new InvalidOperationException("Invalid model.");
      public override void Dispose(){Disposed=true;base.Dispose();}
    }

    [Fact]
    public void PreparationCompilesWithoutOpeningDatabaseAndTheRealLoadReusesIt()
    {
      LibraryLoadingTests.WithDispatcher(async () =>
      {
      using var connection=new SqliteConnection("Data Source=:memory:");
      connection.Open();
      var audit=new Audit();
      int compilations=0;
      Action<string> log=_=>Interlocked.Increment(ref compilations);
      using(var seed=new Context(connection,audit,log))
      {
        seed.Database.EnsureCreated();
        seed.AddRange(Enumerable.Range(1,10000).Select(i=>new SoundItemFilePlaylist
        {Id=i,HashCode=i,Name="Playlist "+i,LastPlayed=new DateTime(2020,1,1).AddSeconds(i),IsPrivate=i%2==0}));
        seed.SaveChanges();
      }
      // A second unopened connection keeps preparation independent of the live
      // in-memory database, making accidental reads fail immediately.
      using var unopened=new SqliteConnection("Data Source=:memory:");
      Context prepared=null;
      int owner=Thread.CurrentThread.ManagedThreadId,worker=owner;
      Assert.True(await PlaylistQueries.PrepareAsync(() =>
      {
        worker=Thread.CurrentThread.ManagedThreadId;
        return prepared=new Context(unopened,audit,log);
      }));
      Assert.NotEqual(owner,worker);
      Assert.True(prepared.Disposed);
      Assert.Equal(System.Data.ConnectionState.Closed,unopened.State);
      Assert.Equal(0,audit.Opens);
      Assert.Equal(1,compilations);
      using var real=new Context(connection,audit,log);
      var rows=await PlaylistQueries.Public(real.SoundItemPlaylists.AsNoTracking()).ToListAsync();
      Assert.Equal(Enumerable.Range(1,10000).Where(i=>i%2!=0).Reverse(),rows.Select(x=>x.Id));
      Assert.Equal(1,compilations);
      });
    }

    [Fact]
    public async Task PreparationFailuresDisposeTheirContextAndDoNotPreventRetry()
    {
      Assert.False(await PlaylistQueries.PrepareAsync(()=>throw new InvalidOperationException("Factory failed.")));
      var broken=new BrokenContext();
      Assert.False(await PlaylistQueries.PrepareAsync(()=>broken));
      Assert.True(broken.Disposed);
      using var connection=new SqliteConnection("Data Source=:memory:");
      var audit=new Audit();
      Context prepared=null;
      Assert.True(await PlaylistQueries.PrepareAsync(()=>prepared=new RetryContext(connection,audit)));
      Assert.True(prepared.Disposed);
      Assert.Equal(0,audit.Opens);
    }
  }
}
