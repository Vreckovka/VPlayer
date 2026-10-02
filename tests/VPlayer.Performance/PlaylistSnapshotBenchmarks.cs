using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.Core.ViewModels;

namespace VPlayer.Performance
{
  internal static class PlaylistSnapshotBenchmarks
  {
    internal static void Run(string directory, string output, string commit)
    {
      if (File.Exists(output)) throw new InvalidOperationException("Benchmark output is immutable.");
      var database=Path.Combine(Path.GetFullPath(directory),"VPlayerDatabase.db");
      using var context=new FixtureContext(database);
      var playlist=context.SoundItemPlaylists.AsNoTracking().Single(x=>x.Id==658);
      playlist.PlaylistItems=context.SoundItemPlaylists.AsNoTracking().Where(x=>x.Id==658)
        .SelectMany(x=>x.PlaylistItems).Include(x=>x.ReferencedItem.FileInfoEntity)
        .OrderBy(x=>x.OrderInPlaylist).ThenBy(x=>x.Id).ToList();
      if(playlist.PlaylistItems.Count!=100000)
        throw new InvalidOperationException("Requires 100k stored occurrences; found "+playlist.PlaylistItems.Count);
      var missingModels=playlist.PlaylistItems.Count(x=>x.ReferencedItem==null);
      var missingFileInfo=playlist.PlaylistItems.Count(x=>x.ReferencedItem!=null && x.ReferencedItem.FileInfoEntity==null);
      Console.WriteLine("Stored occurrences: "+playlist.PlaylistItems.Count+", missing tracks: "+missingModels+", missing file info: "+missingFileInfo);
      playlist.ActualItem=playlist.PlaylistItems[playlist.PlaylistItems.Count-1];
      playlist.ActualItemId=playlist.ActualItem.Id;
      playlist.LastItemIndex=playlist.PlaylistItems.Count-1;
      var samples=new List<object>();
      for(int iteration=0;iteration<5;iteration++)
      {
        var allocated=GC.GetAllocatedBytesForCurrentThread();
        var timer=Stopwatch.StartNew();
        var snapshot=PlaylistSaveSnapshot.Create(playlist);
        timer.Stop();
        allocated=GC.GetAllocatedBytesForCurrentThread()-allocated;
        if(snapshot.PlaylistItems.Count!=playlist.PlaylistItems.Count ||
          !ReferenceEquals(snapshot.ActualItem,snapshot.PlaylistItems[snapshot.LastItemIndex]))
          throw new InvalidOperationException("Snapshot lost current occurrence identity.");
        for(int i=0;i<snapshot.PlaylistItems.Count;i++)
        {
          var source=playlist.PlaylistItems[i];
          var saved=snapshot.PlaylistItems[i];
          if(ReferenceEquals(source,saved) || (source.ReferencedItem!=null && ReferenceEquals(source.ReferencedItem,saved.ReferencedItem)) ||
            (source.ReferencedItem?.FileInfoEntity!=null && ReferenceEquals(source.ReferencedItem.FileInfoEntity,saved.ReferencedItem?.FileInfoEntity)) ||
            source.Id!=saved.Id || source.OrderInPlaylist!=saved.OrderInPlaylist ||
            source.ReferencedItem?.Name!=saved.ReferencedItem?.Name || source.ReferencedItem?.Source!=saved.ReferencedItem?.Source)
            throw new InvalidOperationException("Snapshot changed or shared stored data.");
        }
        samples.Add(new {Iteration=iteration,Milliseconds=timer.Elapsed.TotalMilliseconds,AllocatedBytes=allocated});
        GC.KeepAlive(snapshot);
      }
      using var stream=File.OpenRead(database);
      using var hash=SHA256.Create();
      var fixtureHash=BitConverter.ToString(hash.ComputeHash(stream)).Replace("-","");
      File.WriteAllText(output,JsonSerializer.Serialize(new {SchemaVersion=1,Commit=commit,CreatedUtc=DateTime.UtcNow,
        Runtime=Environment.Version.ToString(),Configuration="Release",Environment.ProcessorCount,
        FixtureSha256=fixtureHash,Entries=playlist.PlaylistItems.Count,MissingModels=missingModels,MissingFileInfo=missingFileInfo,
        Boundary="Production save snapshot only; database reads, verification and persistence excluded",
        Samples=samples},new JsonSerializerOptions {WriteIndented=true}));
      Console.WriteLine("Verified five independent 100k-entry snapshots: "+output);
    }
  }
}
