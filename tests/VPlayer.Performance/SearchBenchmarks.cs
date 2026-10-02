using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Windows.Threading;
using Logger;
using Microsoft.EntityFrameworkCore;
using Moq;
using VCore.ItemsCollections;
using VCore.Standard.Factories.ViewModels;
using VCore.WPF;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.ViewModels;
using VPlayer.Home.ViewModels.LibraryViewModels;

namespace VPlayer.Performance
{
  // The production collection/filter/publication path with lightweight named
  // views. Entity loading and view creation are outside this component boundary.
  internal static class SearchBenchmarks
  {
    private sealed class NameView : NamedEntityViewModel<SoundItem>
    {
      public NameView(SoundItem model):base(model) {}
      public override void Update(SoundItem model)=>RefreshModel(model);
    }
    public static void Run(string directory,string output,string commit)
    {
      directory=Path.GetFullPath(directory);
      output=Path.GetFullPath(output);
      if(File.Exists(output)) throw new IOException("Output exists; search samples are immutable.");
      var database=Path.Combine(directory,"VPlayerDatabase.db");
      using var stream=File.OpenRead(database);
      var checksum=BitConverter.ToString(SHA256.Create().ComputeHash(stream)).Replace("-","");
      var fixture=JsonSerializer.Deserialize<JsonElement>(File.ReadAllText(Path.Combine(directory,"fixture.json")));
      if(checksum!=fixture.GetProperty("DatabaseSha256").GetString()) throw new InvalidOperationException("Fixture changed.");
      using var context=new FixtureContext(database);
      var models=context.SoundItems.AsNoTracking().OrderBy(x=>x.Id).Select(x=>new SoundItem {Id=x.Id,Name=x.Name}).ToList();
      if(models.Count!=fixture.GetProperty("SoundItems").GetInt32()) throw new InvalidOperationException("Incomplete search workload.");
      var longest=models.Where(x=>!string.IsNullOrEmpty(x.Name)).OrderByDescending(x=>x.Name.Length).First().Name;
      var near=longest.Substring(0,longest.Length/2)+"¤"+longest.Substring(longest.Length/2+1);
      var queries=new[] {new {Name="long no-match",Text=new string('¤',longest.Length)},new {Name="long near-match",Text=near}};
      var dispatcher=Dispatcher.CurrentDispatcher;
      var prior=VSynchronizationContext.UISynchronizationContext;
      var priorDispatcher=VSynchronizationContext.UIDispatcher;
      var priorThread=SynchronizationContext.Current;
      var synchronization=new DispatcherSynchronizationContext(dispatcher);
      VSynchronizationContext.UISynchronizationContext=synchronization;
      VSynchronizationContext.UIDispatcher=dispatcher;
      SynchronizationContext.SetSynchronizationContext(synchronization);
      var metrics=new List<Metric>();
      var results=new List<object>();
      var views=models.Select(x=>new NameView(x)).ToArray();
      try
      {
        var storage=new Mock<IStorageManager>();
        storage.Setup(x=>x.GetTempRepository<SoundItem>()).Returns(Array.Empty<SoundItem>().AsQueryable());
        var library=new LibraryCollection<NameView,SoundItem>(new Mock<IViewModelsFactory>().Object,storage.Object,new Mock<ILogger>().Object)
          {Items=new RxObservableCollection<NameView>(views)};
        foreach(var query in queries)
        {
          var times=new List<double>();
          var allocations=new List<long>();
          string resultHash=null;
          int resultCount=0;
          for(int i=0;i<3;i++)
          {
            long bytes=GC.GetTotalAllocatedBytes(false);
            var watch=Stopwatch.StartNew();
            library.Filter(query.Text);
            watch.Stop();
            times.Add(watch.Elapsed.TotalMilliseconds);
            allocations.Add(GC.GetTotalAllocatedBytes(false)-bytes);
            var ids=library.FilteredItems.Select(x=>x.ModelId).ToArray();
            if(!ids.SequenceEqual(library.FilteredItemsCollection.Select(x=>x.ModelId))) throw new InvalidOperationException("Search publication differs.");
            var hash=BitConverter.ToString(SHA256.Create().ComputeHash(System.Text.Encoding.UTF8.GetBytes(string.Join(",",ids)))).Replace("-","");
            if(resultHash!=null && resultHash!=hash) throw new InvalidOperationException("Search results changed between samples.");
            resultHash=hash;resultCount=ids.Length;
            Console.WriteLine(query.Name+" sample "+i+": "+watch.Elapsed.TotalMilliseconds.ToString("F2")+" ms; matches="+resultCount);
          }
          var sorted=times.Skip(1).OrderBy(x=>x).ToArray();
          var allocated=allocations.Skip(1).OrderBy(x=>x).ToArray();
          metrics.Add(new Metric {Name="UI / library fuzzy "+query.Name,Category="UI",
            Boundary="Production LibraryCollection.Filter and both materialized result publications; no entity loading or XAML",
            Workload=models.Count+" copied titles; query length "+query.Text.Length,
            FirstMilliseconds=times[0],MedianMilliseconds=(sorted[0]+sorted[1])/2,P95Milliseconds=sorted[1],
            MedianAllocatedBytes=(allocated[0]+allocated[1])/2,SamplesMilliseconds=times.ToArray()});
          results.Add(new {Name="UI / library fuzzy "+query.Name,Query=query.Text,Count=resultCount,OrderedIdsSha256=resultHash});
        }
      }
      finally
      {
        foreach(var view in views) view.Dispose();
        VSynchronizationContext.UISynchronizationContext=prior;
        VSynchronizationContext.UIDispatcher=priorDispatcher;
        SynchronizationContext.SetSynchronizationContext(priorThread);
      }
      Directory.CreateDirectory(Path.GetDirectoryName(output));
      File.WriteAllText(output,JsonSerializer.Serialize(new {SchemaVersion=1,Commit=commit,CreatedUtc=DateTime.UtcNow,
        Runtime=Environment.Version.ToString(),OS=Environment.OSVersion.ToString(),ProcessorCount=Environment.ProcessorCount,
        Configuration="Release",FixtureSha256=checksum,Fixture=fixture,Metrics=metrics,Results=results},new JsonSerializerOptions {WriteIndented=true}));
    }
  }
}
