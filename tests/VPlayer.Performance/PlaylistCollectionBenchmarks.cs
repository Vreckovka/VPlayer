using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Windows.Data;
using Microsoft.EntityFrameworkCore;
using VPlayer.Core.ViewModels;
using VPlayer.Core.ViewModels.SoundItems;
using VPlayer.TestSupport;

namespace VPlayer.Performance
{
  internal static class PlaylistCollectionBenchmarks
  {
    internal static void Run(string directory,string output,string commit)
    {
      if(File.Exists(output)) throw new InvalidOperationException("Benchmark output is immutable.");
      var database=Path.Combine(Path.GetFullPath(directory),"VPlayerDatabase.db");
      using var context=new FixtureContext(database);
      var rows=context.SoundItemPlaylists.AsNoTracking().Where(x=>x.Id==658)
        .SelectMany(x=>x.PlaylistItems).Include(x=>x.ReferencedItem.FileInfoEntity)
        .OrderBy(x=>x.OrderInPlaylist).ThenBy(x=>x.Id).ToArray();
      if(rows.Length!=100000 || rows.Any(x=>x.ReferencedItem==null))
        throw new InvalidOperationException("Requires 100k available stored occurrences.");
      using var fixture=new SavedSongViewFixture(true);
      var views=fixture.Create(rows).ToArray();
      var samples=new List<object>();
      try
      {
        for(int iteration=0;iteration<5;iteration++)
        {
          foreach(var item in views)item.IsInPlaylist=false;
          using var collection=new PlaylistCollection<SoundItemInPlaylistViewModel>();
          var view=new ListCollectionView(collection);
          int added=0,removed=0,updated=0,cleared=0,resets=0;
          using var add=collection.ItemAdded.Subscribe(e=>{added++;e.EventArgs.IsInPlaylist=true;});
          using var remove=collection.ItemRemoved.Subscribe(e=>{removed++;e.EventArgs.IsInPlaylist=false;});
          using var update=collection.ItemUpdated.Subscribe(_=>updated++);
          using var clear=collection.Cleared.Subscribe(_=>cleared++);
          collection.CollectionChanged+=(_,e)=>{if(e.Action==System.Collections.Specialized.NotifyCollectionChangedAction.Reset)resets++;};
          var bytes=GC.GetAllocatedBytesForCurrentThread();
          var timer=Stopwatch.StartNew();
          collection.AddPlaylistRange(views);
          timer.Stop();
          var addMilliseconds=timer.Elapsed.TotalMilliseconds;
          var addBytes=GC.GetAllocatedBytesForCurrentThread()-bytes;
          if(collection.Count!=100000 || collection.View.Count!=100000 || view.Count!=100000 ||
            added!=100000 || removed!=0 || cleared!=0 || resets!=1)
            throw new InvalidOperationException("Batch lost reactive events or published extra WPF resets.");
          for(int i=0;i<views.Length;i++)
            if(!ReferenceEquals(collection[i],views[i]) || !ReferenceEquals(view.GetItemAt(i),views[i]) ||
              !ReferenceEquals(collection.View[i],views[i]) || !views[i].IsInPlaylist ||
              !ReferenceEquals(views[i].Model,rows[i].ReferencedItem))
              throw new InvalidOperationException("Batch changed occurrence identity, order, state or stored metadata.");
          updated=0;
          views[99999].IsSelected=!views[99999].IsSelected;
          if(updated==0)throw new InvalidOperationException("Property tracking missing after batch.");
          bytes=GC.GetAllocatedBytesForCurrentThread();
          timer.Restart();
          collection.Clear();
          timer.Stop();
          var clearBytes=GC.GetAllocatedBytesForCurrentThread()-bytes;
          if(collection.Count!=0 || collection.View.Count!=0 || view.Count!=0 || cleared!=1 || resets!=2)
            throw new InvalidOperationException("Clear left stale collection rows.");
          samples.Add(new {Iteration=iteration,AddMilliseconds=addMilliseconds,AddAllocatedBytes=addBytes,
            ClearMilliseconds=timer.Elapsed.TotalMilliseconds,ClearAllocatedBytes=clearBytes});
        }
      }
      finally {foreach(var item in views)item.Dispose();}
      using var stream=File.OpenRead(database);
      using var hash=SHA256.Create();
      File.WriteAllText(output,JsonSerializer.Serialize(new {SchemaVersion=1,Commit=commit,CreatedUtc=DateTime.UtcNow,
        Runtime=Environment.Version.ToString(),Configuration="Release",Environment.ProcessorCount,
        FixtureSha256=BitConverter.ToString(hash.ComputeHash(stream)).Replace("-",""),Entries=rows.Length,
        DuplicateTracks=rows.Length-rows.Select(x=>x.ReferencedItem.Id).Distinct().Count(),
        MissingFileInfo=rows.Count(x=>x.ReferencedItem.FileInfoEntity==null),
        Boundary="Production playlist collection with 100k real saved song views, synchronous membership observers and WPF ListCollectionView; database reads, view construction, dispatcher scheduling, verification, disposal and UI painting excluded",
        Samples=samples},new JsonSerializerOptions {WriteIndented=true}));
      Console.WriteLine("Verified five 100k-entry collection publication/clear samples: "+output);
    }
  }
}