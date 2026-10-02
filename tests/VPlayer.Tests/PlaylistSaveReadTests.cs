using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using VPlayer.AudioStorage.AudioDatabase;
using VPlayer.AudioStorage.DomainClasses;
using Xunit;

namespace VPlayer.Tests
{
  public class PlaylistSaveReadTests
  {
    private sealed class Audit : DbCommandInterceptor
    {
      public List<string> Plan=new List<string>();
      public int Commands;
      public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command,CommandEventData data,InterceptionResult<DbDataReader> result)
      {
        Commands++;
        using var explain=command.Connection.CreateCommand();
        explain.CommandText="EXPLAIN QUERY PLAN "+command.CommandText;
        foreach(DbParameter parameter in command.Parameters)
        {
          var copy=explain.CreateParameter();
          copy.ParameterName=parameter.ParameterName;
          copy.Value=parameter.Value;
          explain.Parameters.Add(copy);
        }
        using var reader=explain.ExecuteReader();
        while(reader.Read()) Plan.Add(reader.GetString(3));
        return result;
      }
    }
    private sealed class Context : AudioDatabaseContext
    {
      private readonly SqliteConnection connection;
      private readonly Audit audit;
      public Context(SqliteConnection connection,Audit audit) {this.connection=connection;this.audit=audit;}
      protected override void OnConfiguring(DbContextOptionsBuilder builder)=>builder.UseSqlite(connection).AddInterceptors(audit);
    }
    [Theory]
    [InlineData(1,28)]
    [InlineData(2,100000)]
    public void StoredReadUsesPlaylistIndexAndPreservesOccurrencesMetadataAndActualItem(int playlistId,int expectedCount)
    {
      using var connection=new SqliteConnection("Data Source=:memory:");
      connection.Open();
      var audit=new Audit();
      using var context=new Context(connection,audit);
      context.Database.EnsureCreated();
      var tracks=Enumerable.Range(1,4).Select(id=>new SoundItem {Id=id,IsPrivate=id%2==0,
        FileInfoEntity=new FileInfoEntity {Id=id,Title=new string('W',500)+id}}).ToArray();
      // Insert the full 100k workload directly, avoiding unrelated EF graph-attachment cost in fixture setup.
      context.SoundItems.AddRange(tracks);
      context.Set<SoundItemFilePlaylist>().AddRange(
        new SoundItemFilePlaylist {Id=1,Name="small",HashCode=-28,ItemCount=28,PlaylistItems=new List<PlaylistSoundItem>()},
        new SoundItemFilePlaylist {Id=2,Name="worst",HashCode=-100000,ItemCount=100000,PlaylistItems=new List<PlaylistSoundItem>()});
      context.SaveChanges();
      context.Database.ExecuteSqlRaw("WITH RECURSIVE n(i) AS (SELECT 1 UNION ALL SELECT i+1 FROM n WHERE i<28) INSERT INTO PlaylistSongs(Id,IdReferencedItem,OrderInPlaylist,SoundItemFilePlaylistId,Created) SELECT i,1+(i-1)%4,i,1,'2000-01-01 00:00:00' FROM n;");
      context.Database.ExecuteSqlRaw("WITH RECURSIVE n(i) AS (SELECT 1 UNION ALL SELECT i+1 FROM n WHERE i<100000) INSERT INTO PlaylistSongs(Id,IdReferencedItem,OrderInPlaylist,SoundItemFilePlaylistId,Created) SELECT i+28,1+(i-1)%4,i,2,'2000-01-01 00:00:00' FROM n;");
      context.Database.ExecuteSqlRaw("UPDATE SoundItemPlaylists SET ActualItemId=28 WHERE Id=1;");
      context.Database.ExecuteSqlRaw("UPDATE SoundItemPlaylists SET ActualItemId=100028 WHERE Id=2;");
      audit.Plan.Clear();
      audit.Commands=0;
      var query=(IQueryable<SoundItemFilePlaylist>)typeof(VPlayerStorageManager)
        .GetMethod("QuerySoundPlaylistForUpdate",BindingFlags.Static|BindingFlags.NonPublic)
        .Invoke(null,new object[] {context.Set<SoundItemFilePlaylist>().AsNoTracking()});
      var loaded=query.Single(playlist=>playlist.Id==playlistId);
      var ordered=loaded.PlaylistItems.OrderBy(item=>item.OrderInPlaylist).ThenBy(item=>item.Id).ToArray();
      Assert.Equal(expectedCount,ordered.Length);
      Assert.Equal(Enumerable.Range(1,expectedCount).Select(id=>playlistId==1?id:id+28),ordered.Select(item=>item.Id));
      Assert.Equal(Enumerable.Range(1,expectedCount).Select(id=>1+(id-1)%4),ordered.Select(item=>item.IdReferencedItem));
      Assert.All(ordered,item=>
      {
        Assert.NotNull(item.ReferencedItem.FileInfoEntity);
        Assert.Equal(item.IdReferencedItem,item.ReferencedItem.FileInfoEntity.Id);
        Assert.Equal(new string('W',500)+item.IdReferencedItem,item.ReferencedItem.FileInfoEntity.Title);
        Assert.Equal(item.IdReferencedItem%2==0,item.ReferencedItem.IsPrivate);
      });
      Assert.Equal(ordered.Last().Id,loaded.ActualItem.Id);
      Assert.Equal(ordered.Last().IdReferencedItem,loaded.ActualItem.ReferencedItem.Id);
      Assert.Contains(audit.Plan,line=>line.Contains("IX_PlaylistSongs_SoundItemFilePlaylistId"));
      Assert.DoesNotContain(audit.Plan,line=>line.StartsWith("SCAN TABLE PlaylistSongs",StringComparison.OrdinalIgnoreCase));
    }
  }
}