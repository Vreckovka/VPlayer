using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using VPlayer.AudioStorage.AudioDatabase;
using VPlayer.AudioStorage.DomainClasses;
using Xunit;

namespace VPlayer.Tests
{
  public class DatabaseProfilingTests
  {
    private sealed class Context : AudioDatabaseContext
    {
      private readonly string database;
      private readonly IInterceptor[] interceptors;
      public Context(string database,params IInterceptor[] interceptors)
      {this.database=database;this.interceptors=interceptors;}
      protected override void OnConfiguring(DbContextOptionsBuilder builder)
      {builder.UseSqlite(new SqliteConnectionStringBuilder {DataSource=database}.ToString()).AddInterceptors(interceptors);}
    }
    private sealed class Scope : IDisposable
    {
      private readonly Action complete;
      private int disposed;
      public Scope(Action complete) => this.complete=complete;
      public void Dispose()
      {Assert.Equal(0,Interlocked.Exchange(ref disposed,1));complete();}
    }
    private static IInterceptor Interceptor(string name,Func<string,IDisposable> start)
    {
      var type=typeof(AudioDatabaseContext).Assembly.GetType("VPlayer.AudioStorage.AudioDatabase.Benchmark"+name+"Interceptor",true);
      return (IInterceptor)Activator.CreateInstance(type,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic,
        null,new object[] {start},null);
    }
    [Fact]
    public async Task NativeSqliteQueriesCompleteTheirProfilingScopesOnSyncAsyncAndFailurePaths()
    {
      var directory=Path.Combine(Path.GetTempPath(),"VPlayer-query-profile-"+Guid.NewGuid().ToString("N"));
      Directory.CreateDirectory(directory);
      var database=Path.Combine(directory,"fixture.db");
      try
      {
        using(var seed=new Context(database))
        {
          seed.Database.EnsureCreated();
          seed.Artists.AddRange(Enumerable.Range(1,10000)
            .Select(i=>new Artist {Name=new string('W',180)+" Artist "+i}));
          seed.SaveChanges();
        }
        var active=new ConcurrentDictionary<Guid,string>();
        var finished=new ConcurrentBag<string>();
        IDisposable Start(string name)
        {
          var id=Guid.NewGuid();
          Assert.True(active.TryAdd(id,name));
          return new Scope(()=>
          {
            Assert.True(active.TryRemove(id,out var completed));
            finished.Add(completed);
          });
        }
        using(var context=new Context(database,Interceptor("Execution",Start),Interceptor("Connection",Start)))
        {
          Assert.Equal(10000,context.Artists.AsNoTracking().ToArray().Length);
          Assert.Empty(active);
          Assert.Equal(10000,(await context.Artists.AsNoTracking().ToArrayAsync()).Length);
          Assert.Empty(active);
          Assert.Throws<SqliteException>(()=>context.Artists.FromSqlRaw("SELECT * FROM MissingArtists").ToArray());
          Assert.Empty(active);
          await Assert.ThrowsAsync<SqliteException>(()=>context.Artists.FromSqlRaw("SELECT * FROM MissingArtists").ToArrayAsync());
          Assert.Empty(active);
        }
        Assert.Equal(2,finished.Count(name=>name=="Database / execute / Artists"));
        Assert.Equal(2,finished.Count(name=>name=="Database / execute / other"));
        Assert.Equal(4,finished.Count(name=>name=="Database / connection open"));
      }
      finally {Directory.Delete(directory,true);}
    }
  }
}