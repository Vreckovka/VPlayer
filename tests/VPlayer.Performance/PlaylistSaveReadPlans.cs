using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using VPlayer.AudioStorage.AudioDatabase;
using VPlayer.AudioStorage.DomainClasses;

namespace VPlayer.Performance
{
  // Capture the actual SingleOrDefault command before running its expensive read.
  public static class PlaylistSaveReadPlans
  {
    private sealed class Captured : Exception {}
    private sealed class Capture : DbCommandInterceptor
    {
      public string Sql;
      public List<string> Plan=new List<string>();
      public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command,CommandEventData data,InterceptionResult<DbDataReader> result)
      {
        Sql=command.CommandText;
        using var explain=command.Connection.CreateCommand();
        explain.CommandText="EXPLAIN QUERY PLAN "+Sql;
        foreach(DbParameter parameter in command.Parameters)
        {
          var copy=explain.CreateParameter();
          copy.ParameterName=parameter.ParameterName;
          copy.Value=parameter.Value;
          explain.Parameters.Add(copy);
        }
        using(var reader=explain.ExecuteReader())
          while(reader.Read()) Plan.Add(reader.GetString(3));
        throw new Captured();
      }
    }
    private sealed class Context : AudioDatabaseContext
    {
      private readonly string database;
      private readonly Capture capture;
      public Context(string database,Capture capture) {this.database=database;this.capture=capture;}
      protected override void OnConfiguring(DbContextOptionsBuilder builder)=>builder
        .UseSqlite(new SqliteConnectionStringBuilder {DataSource=database,Mode=SqliteOpenMode.ReadOnly}.ToString())
        .AddInterceptors(capture);
    }
    public static void Run(string fixture,string output,string commit)
    {
      var database=Path.Combine(Path.GetFullPath(fixture),"VPlayerDatabase.db");
      var records=new List<object>();
      // Include the original saved queue and the worst stress queue.
      foreach(var id in new[] {625,658})
      {
        var capture=new Capture();
        using var context=new Context(database,capture);
        var timer=Stopwatch.StartNew();
        try
        {
          context.Set<SoundItemFilePlaylist>().AsNoTracking()
            .Include(playlist=>playlist.PlaylistItems)
            .ThenInclude(item=>item.ReferencedItem.FileInfoEntity)
            .Include(playlist=>playlist.ActualItem.ReferencedItem)
            .SingleOrDefault(playlist=>playlist.Id==id);
          throw new InvalidOperationException("Playlist plan was not captured.");
        }
        catch(Captured) {}
        timer.Stop();
        records.Add(new {PlaylistId=id,Milliseconds=timer.Elapsed.TotalMilliseconds,capture.Sql,capture.Plan});
      }
      using var stream=File.OpenRead(database);
      using var hashing=SHA256.Create();
      var sha=BitConverter.ToString(hashing.ComputeHash(stream)).Replace("-","");
      File.WriteAllText(output,JsonSerializer.Serialize(new {Commit=commit,FixtureSha256=sha,Runtime=Environment.Version.ToString(),
        Boundary="SQL compilation and EXPLAIN only; stored playlist read is not executed",Records=records},new JsonSerializerOptions {WriteIndented=true}));
      Console.WriteLine("Saved playlist SQL plans: "+output);
    }
  }
}