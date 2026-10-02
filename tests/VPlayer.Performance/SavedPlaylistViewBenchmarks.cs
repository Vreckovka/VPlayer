using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VPlayer.Core.ViewModels.SoundItems;
using VPlayer.TestSupport;

namespace VPlayer.Performance
{
  internal static class SavedPlaylistViewBenchmarks
  {
    internal static void Run(string directory,string output,string commit)
    {
      if(File.Exists(output)) throw new InvalidOperationException("Benchmark output is immutable.");
      var database=Path.Combine(Path.GetFullPath(directory),"VPlayerDatabase.db");
      using var context=new FixtureContext(database);
      var rows=context.SoundItemPlaylists.AsNoTracking().Where(x=>x.Id==658)
        .SelectMany(x=>x.PlaylistItems).Include(x=>x.ReferencedItem.FileInfoEntity)
        .OrderBy(x=>x.OrderInPlaylist).ThenBy(x=>x.Id).ToArray();
      if(rows.Length!=100000 || rows.Any(x=>x.ReferencedItem==null)) throw new InvalidOperationException("Requires 100k available stored occurrences.");
      using var fixture=new SavedSongViewFixture();
      var samples=new List<object>();
      for(int iteration=0;iteration<5;iteration++)
      {
        var allocated=GC.GetAllocatedBytesForCurrentThread();
        var timer=Stopwatch.StartNew();
        var views=fixture.Create(rows).ToArray();
        timer.Stop();
        allocated=GC.GetAllocatedBytesForCurrentThread()-allocated;
        try
        {
          if(views.Length!=100000 || new HashSet<SoundItemInPlaylistViewModel>(views).Count!=100000)
            throw new InvalidOperationException("Playlist occurrences were reused or lost.");
          for(int i=0;i<views.Length;i++)
            if(!ReferenceEquals(rows[i].ReferencedItem,views[i].Model) || !(views[i] is SongInPlayListViewModel song) ||
              !ReferenceEquals(song.SongModel.ItemModel,rows[i].ReferencedItem) || views[i].Name!=rows[i].ReferencedItem.Name)
              throw new InvalidOperationException("Stored metadata or occurrence order changed.");
          samples.Add(new {Iteration=iteration,Milliseconds=timer.Elapsed.TotalMilliseconds,AllocatedBytes=allocated});
        }
        finally {foreach(var view in views)view.Dispose();}
      }
      using var stream=File.OpenRead(database);
      using var hash=SHA256.Create();
      File.WriteAllText(output,JsonSerializer.Serialize(new {SchemaVersion=1,Commit=commit,CreatedUtc=DateTime.UtcNow,
        Runtime=Environment.Version.ToString(),Configuration="Release",Environment.ProcessorCount,
        FixtureSha256=BitConverter.ToString(hash.ComputeHash(stream)).Replace("-",""),Entries=rows.Length,
        MissingFileInfo=rows.Count(x=>x.ReferencedItem.FileInfoEntity==null),
        Boundary="Production MusicPlayerViewModel saved view creation; real Ninject, shared stub services; database reads, UI rendering, verification and disposal excluded",
        Samples=samples},new JsonSerializerOptions {WriteIndented=true}));
      Console.WriteLine("Verified five independent 100k-entry saved view batches: "+output);
    }
  }
}
