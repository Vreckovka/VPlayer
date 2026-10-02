using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using System.Windows.Threading;
using VPlayer.Home.ViewModels.Statistics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Logger;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using Ninject;
using VPlayer.Core.Factories;
using VPlayer.Home.ViewModels.LibraryViewModels;
using VCore.WPF.Modularity.RegionProviders;
using VCore.Standard.Providers;
using PCloudClient;
using Prism.Events;
using VCore.Standard.Factories.ViewModels;
using VCore.WPF;
using VCore.WPF.Interfaces.Managers;
using VCore.WPF.LRC.Domain;
using VPlayer.AudioStorage.AudioDatabase;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.DomainClasses.Video;
using VPlayer.AudioStorage.DomainClasses.IPTV;
using VPlayer.AudioStorage.InfoDownloader.Clients.PCloud;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.ViewModels.SoundItems;
using VPlayer.Home.ViewModels;
using VPLayer.Domain;
using WinformsVisualization.Visualization;
using Point = WinformsVisualization.Visualization.SpectrumBase.SpectrumPointData;

namespace VPlayer.Performance
{
  public sealed class FixtureContext : AudioDatabaseContext
  {
    private readonly string database;
    public FixtureContext(string database) => this.database=database;
    protected override void OnConfiguring(DbContextOptionsBuilder builder) =>
      builder.UseSqlite(new SqliteConnectionStringBuilder {DataSource=database,Mode=SqliteOpenMode.ReadOnly}.ToString());
  }
  public sealed class Metric
  {
    public string Name {get;set;}
    public string Category {get;set;}
    public string Boundary {get;set;}
    public string Workload {get;set;}
    public double FirstMilliseconds {get;set;}
    public double MedianMilliseconds {get;set;}
    public double P95Milliseconds {get;set;}
    public long MedianAllocatedBytes {get;set;}
    public double[] SamplesMilliseconds {get;set;}
  }
  public static class Program
  {
    private static readonly List<Metric> metrics=new List<Metric>();
    private static readonly JsonSerializerOptions json=new JsonSerializerOptions {WriteIndented=true};
    [STAThread]
    public static int Main(string[] args)
    {
      try
      {
        if(args.Length<3) throw new ArgumentException("prepare <source.db> <fixture-directory> [factor] OR run <fixture-directory> <output.json> <commit>");
        if(args[0]=="window-activation")
          WindowActivationChecks.Run(args[1],args[2],args.Length>3 && args[3]=="topmost");
        else if(args[0]=="window-foreground-probe")
          WindowActivationChecks.ShowProbe(args[1],args[2]=="topmost");
        else if(args[0]=="prepare")
          Prepare(args[1],args[2],args.Length>3?int.Parse(args[3]):5);
        else if(args[0]=="prepare-ui")
          PrepareUi(args[1],args[2],args.Length>3?int.Parse(args[3]):5000);
        else if(args[0]=="prepare-statistics")
          PrepareStatistics(args[1],args[2]);
        else if(args[0]=="statistics")
          RunStatistics(args[1],args[2],args.Length>3?args[3]:"unknown");
        else if(args[0]=="statistics-reload")
          RunStatistics(args[1],args[2],args.Length>3?args[3]:"unknown",32);
        else if(args[0]=="graphics")
        {
          using var context=new FixtureContext(Path.Combine(Path.GetFullPath(args[1]),"VPlayerDatabase.db"));
          var events=new EventAggregator();
          var storage=new Mock<IStorageManager>().Object;
          var soundModels=context.SoundItems.AsNoTracking().Include(x=>x.FileInfoEntity).ToList();
          Console.WriteLine("Metadata: "+soundModels.Count(x=>x.FileInfoEntity!=null)+" items with file info, "+soundModels.Count(x=>!string.IsNullOrWhiteSpace(x.Name))+" items with names.");
          RunGraphics(soundModels
            .Select(x=>new SoundItemInPlaylistViewModel(x,events,storage)).ToList());
        }
        else if(args[0]=="search")
          SearchBenchmarks.Run(args[1],args[2],args.Length>3?args[3]:"unknown");
        else if(args[0]=="playlist-collection")
          PlaylistCollectionBenchmarks.Run(args[1],args[2],args.Length>3?args[3]:"unknown",args.Length>4 && args[4]=="repeated-tracks");
        else if(args[0]=="saved-playlist-views")
          SavedPlaylistViewBenchmarks.Run(args[1],args[2],args.Length>3?args[3]:"unknown",args.Length>4 && args[4]=="transient-windows");
        else if(args[0]=="playlist-snapshot")
          PlaylistSnapshotBenchmarks.Run(args[1],args[2],args.Length>3?args[3]:"unknown");
        else if(args[0]=="profile-save-read")
          PlaylistSaveReadPlans.Run(args[1],args[2],args.Length>3?args[3]:"unknown");
        else if(args[0]=="profile-playlist")
          ProfilePlaylist(args[1]);
        else if(args[0]=="run")
          Run(args[1],args[2],args.Length>3?args[3]:"unknown");
        else throw new ArgumentException("Unknown command.");
        return 0;
      }
      catch(Exception ex)
      {
        Console.Error.WriteLine(ex);
        return 1;
      }
    }
    private static string Quote(string identifier)=>"\""+identifier.Replace("\"","\"\"")+"\"";
    private static void Execute(SqliteConnection connection,string sql)
    {
      using var command=connection.CreateCommand();
      command.CommandText=sql;
      command.ExecuteNonQuery();
    }
    private static void Prepare(string source,string directory,int factor)
    {
      if(factor<1 || factor>20) throw new ArgumentOutOfRangeException(nameof(factor));
      directory=Path.GetFullPath(directory);
      Directory.CreateDirectory(directory);
      var database=Path.Combine(directory,"VPlayerDatabase.db");
      if(File.Exists(database)) throw new IOException("Fixture already exists; use a new directory to preserve the baseline.");
      using(var input=new SqliteConnection(new SqliteConnectionStringBuilder {DataSource=Path.GetFullPath(source),Mode=SqliteOpenMode.ReadOnly}.ToString()))
      using(var copy=new SqliteConnection(new SqliteConnectionStringBuilder {DataSource=database}.ToString()))
      {
        input.Open();
        copy.Open();
        input.BackupDatabase(copy);
        using var transaction=copy.BeginTransaction();
        // Copy all mapped columns verbatim except identity. File and song references remain valid.
        var columns=new List<string>();
        using(var command=copy.CreateCommand())
        {
          command.Transaction=transaction;
          command.CommandText="PRAGMA table_info(SoundItems)";
          using var reader=command.ExecuteReader();
          while(reader.Read()) if(reader.GetString(1)!="Id") columns.Add(reader.GetString(1));
        }
        using(var command=copy.CreateCommand())
        {
          command.Transaction=transaction;
          command.CommandText="SELECT MAX(Id) FROM SoundItems";
          var maximum=Convert.ToInt64(command.ExecuteScalar());
          var list=string.Join(",",columns.Select(Quote));
          command.CommandText=$"INSERT INTO SoundItems ({list}) SELECT {list} FROM SoundItems WHERE Id <= $max";
          command.Parameters.AddWithValue("$max",maximum);
          for(int i=1;i<factor;i++) command.ExecuteNonQuery();
        }
        transaction.Commit();
      }
      // Add deterministic large playlists to the copied database using the production EF model.
      using(var context=new WritableFixtureContext(database))
      {
        var ids=context.SoundItems.OrderBy(x=>x.Id).Select(x=>x.Id).ToArray();
        if(ids.Length==0) throw new InvalidOperationException("The source contains no sound items.");
        foreach(int size in new[] {1000,10000,100000})
        {
          var playlist=new SoundItemFilePlaylist
          {
            Name="VPlayer benchmark "+size,IsUserCreated=true,HashCode=-size,ItemCount=size,
            PlaylistItems=Enumerable.Range(0,size).Select(i=>new PlaylistSoundItem {IdReferencedItem=ids[i%ids.Length],OrderInPlaylist=i}).ToList()
          };
          context.SoundItemPlaylists.Add(playlist);
          context.SaveChanges();
        }
      }
      using var stream=File.OpenRead(database);
      var checksum=BitConverter.ToString(SHA256.Create().ComputeHash(stream)).Replace("-","");
      using var countContext=new FixtureContext(database);
      File.WriteAllText(Path.Combine(directory,"fixture.json"),JsonSerializer.Serialize(new
      {
        CreatedUtc=DateTime.UtcNow,ExpansionFactor=factor,DatabaseSha256=checksum,
        SoundItems=countContext.SoundItems.Count(),Playlists=countContext.SoundItemPlaylists.Count(),
        PlaylistSizes=new[] {1000,10000,100000}
      },json));
      Console.WriteLine("Prepared "+directory+"; "+countContext.SoundItems.Count()+" sound items.");
    }
    private static void PrepareUi(string parent,string directory,int count)
    {
      if(count<1000 || count>10000) throw new ArgumentOutOfRangeException(nameof(count));
      parent=Path.GetFullPath(parent);
      directory=Path.GetFullPath(directory);
      Directory.CreateDirectory(directory);
      var database=Path.Combine(directory,"VPlayerDatabase.db");
      if(File.Exists(database)) throw new IOException("Fixture exists; choose a new directory.");
      var parentMetadata=JsonSerializer.Deserialize<JsonElement>(File.ReadAllText(Path.Combine(parent,"fixture.json")));
      var parentDatabase=Path.Combine(parent,"VPlayerDatabase.db");
      using(var input=File.OpenRead(parentDatabase))
      {
        var actual=BitConverter.ToString(SHA256.Create().ComputeHash(input)).Replace("-","");
        if(actual!=parentMetadata.GetProperty("DatabaseSha256").GetString())
          throw new InvalidOperationException("Parent fixture changed.");
      }
      using(var input=new SqliteConnection(new SqliteConnectionStringBuilder {DataSource=parentDatabase,Mode=SqliteOpenMode.ReadOnly}.ToString()))
      using(var copy=new SqliteConnection(new SqliteConnectionStringBuilder {DataSource=database}.ToString()))
      {
        input.Open();
        copy.Open();
        input.BackupDatabase(copy);
      }
      using(var context=new WritableFixtureContext(database))
      {
        var ids=context.SoundItems.OrderBy(x=>x.Id).Select(x=>x.Id).Take(count).ToArray();
        for(int i=0;i<count;i++)
          context.SoundItemPlaylists.Add(new SoundItemFilePlaylist
          {
            Name="Grouped UI stress "+i.ToString("D5")+" "+new string('W',180),
            IsUserCreated=true,HashCode=-200000-i,ItemCount=1,
            PlaylistItems=new List<PlaylistSoundItem>
            {new PlaylistSoundItem {IdReferencedItem=ids[i%ids.Length],OrderInPlaylist=0}}
          });
        context.SaveChanges();
      }
      using var stream=File.OpenRead(database);
      var checksum=BitConverter.ToString(SHA256.Create().ComputeHash(stream)).Replace("-","");
      using var counts=new FixtureContext(database);
      File.WriteAllText(Path.Combine(directory,"fixture.json"),JsonSerializer.Serialize(new
      {
        CreatedUtc=DateTime.UtcNow,DatabaseSha256=checksum,
        ParentSha256=parentMetadata.GetProperty("DatabaseSha256").GetString(),
        SoundItems=counts.SoundItems.Count(),Playlists=counts.SoundItemPlaylists.Count(),
        AdditionalUserPlaylists=count,PlaylistSizes=new[] {1000,10000,100000}
      },json));
      Console.WriteLine("Prepared grouped UI fixture: "+counts.SoundItemPlaylists.Count()+" playlists.");
    }
    private static void PrepareStatistics(string parent,string directory)
    {
      parent=Path.GetFullPath(parent);directory=Path.GetFullPath(directory);
      var metadata=JsonSerializer.Deserialize<JsonElement>(File.ReadAllText(Path.Combine(parent,"fixture.json")));
      var parentDatabase=Path.Combine(parent,"VPlayerDatabase.db");
      using(var input=File.OpenRead(parentDatabase))
        if(BitConverter.ToString(SHA256.Create().ComputeHash(input)).Replace("-","")!=metadata.GetProperty("DatabaseSha256").GetString())
          throw new InvalidOperationException("Parent fixture changed.");
      Directory.CreateDirectory(directory);
      var database=Path.Combine(directory,"VPlayerDatabase.db");
      if(File.Exists(database)) throw new IOException("Fixture exists; choose a new directory.");
      using(var input=new SqliteConnection(new SqliteConnectionStringBuilder {DataSource=parentDatabase,Mode=SqliteOpenMode.ReadOnly}.ToString()))
      using(var copy=new SqliteConnection(new SqliteConnectionStringBuilder {DataSource=database}.ToString()))
      {
        input.Open();copy.Open();input.BackupDatabase(copy);
      }
      using(var context=new WritableFixtureContext(database))
      {
        var foreignKey=context.Model.FindEntityType(typeof(SoundItem)).FindNavigation(nameof(SoundItem.FileInfoEntity)).ForeignKey.Properties.Single().Name;
        using var connection=new SqliteConnection(new SqliteConnectionStringBuilder {DataSource=database}.ToString());
        connection.Open();
        var columns=new List<string>();
        using(var command=connection.CreateCommand())
        {
          command.CommandText="PRAGMA table_info(FileInfos)";
          using var reader=command.ExecuteReader();
          while(reader.Read()) columns.Add(reader.GetString(1));
        }
        long offset;
        using(var command=connection.CreateCommand())
        {
          command.CommandText="SELECT COALESCE(MAX(Id),0) FROM FileInfos";
          offset=Convert.ToInt64(command.ExecuteScalar());
        }
        var selected=string.Join(",",columns.Select(column=>column=="Id"?"s.Id+"+offset:"f."+Quote(column)));
        Execute(connection,"INSERT INTO FileInfos ("+string.Join(",",columns.Select(Quote))+") SELECT "+selected+
          " FROM SoundItems s JOIN FileInfos f ON f.Id=s."+Quote(foreignKey));
        Execute(connection,"UPDATE SoundItems SET "+Quote(foreignKey)+"=Id+"+offset+" WHERE "+Quote(foreignKey)+" IS NOT NULL");
      }
      using var file=File.OpenRead(database);
      var checksum=BitConverter.ToString(SHA256.Create().ComputeHash(file)).Replace("-","");
      using var counts=new FixtureContext(database);
      File.WriteAllText(Path.Combine(directory,"fixture.json"),JsonSerializer.Serialize(new
      {
        CreatedUtc=DateTime.UtcNow,DatabaseSha256=checksum,
        ParentSha256=metadata.GetProperty("DatabaseSha256").GetString(),
        SoundItems=counts.SoundItems.Count(),Playlists=counts.SoundItemPlaylists.Count(),
        FileInfos=counts.FileInfos.Count(),UniqueSoundFileMetadata=true,AdditionalUserPlaylists=5000,
        PlaylistSizes=new[] {1000,10000,100000}
      },json));
      Console.WriteLine("Prepared statistics fixture with unique sound-file metadata.");
    }
    private static void RunStatistics(string directory,string output,string commit,int reloadRequests=1)
    {
      if(!System.Text.RegularExpressions.Regex.IsMatch(commit ?? "", "^[0-9a-fA-F]{40}$"))
        throw new ArgumentException("Statistics benchmarks require a full Git commit SHA.",nameof(commit));
      directory=Path.GetFullPath(directory);
      var database=Path.Combine(directory,"VPlayerDatabase.db");
      var fixture=File.ReadAllText(Path.Combine(directory,"fixture.json"));
      using var file=File.OpenRead(database);
      var checksum=BitConverter.ToString(SHA256.Create().ComputeHash(file)).Replace("-","");
      var metadata=JsonSerializer.Deserialize<JsonElement>(fixture);
      if(checksum!=metadata.GetProperty("DatabaseSha256").GetString()) throw new InvalidOperationException("Fixture changed.");
      var dispatcher=Dispatcher.CurrentDispatcher;
      var prior=VSynchronizationContext.UISynchronizationContext;
      var priorDispatcher=VSynchronizationContext.UIDispatcher;
      var priorThread=SynchronizationContext.Current;
      var synchronization=new DispatcherSynchronizationContext(dispatcher);
      VSynchronizationContext.UISynchronizationContext=synchronization;
      VSynchronizationContext.UIDispatcher=dispatcher;
      SynchronizationContext.SetSynchronizationContext(synchronization);
      var contexts=new System.Collections.Concurrent.ConcurrentBag<FixtureContext>();
      var repositoryCounts=new List<int>();
      IQueryable<T> Repository<T>() where T:class
      {
        var context=new FixtureContext(database);
        contexts.Add(context);
        return context.Set<T>().AsNoTracking();
      }
      var storage=new Mock<IStorageManager>();
      storage.Setup(x=>x.GetTempRepository<SoundItem>()).Returns(()=>Repository<SoundItem>());
      storage.Setup(x=>x.GetTempRepository<VideoItem>()).Returns(()=>Repository<VideoItem>());
      storage.Setup(x=>x.GetTempRepository<TvShowEpisode>()).Returns(()=>Repository<TvShowEpisode>());
      storage.Setup(x=>x.GetTempRepository<Song>()).Returns(()=>Repository<Song>());
      storage.Setup(x=>x.GetTempRepository<SoundItemFilePlaylist>()).Returns(()=>Repository<SoundItemFilePlaylist>());
      storage.Setup(x=>x.GetTempRepository<VideoFilePlaylist>()).Returns(()=>Repository<VideoFilePlaylist>());
      storage.Setup(x=>x.GetTempRepository<TvPlaylist>()).Returns(()=>Repository<TvPlaylist>());
      try
      {
        Measure(reloadRequests==1?"Data / statistics":"UI / statistics reload burst",reloadRequests==1?"Data":"UI",
          reloadRequests==1?"Production StatisticsViewModel.LoadData and UI publication; no view rendering":
          "Production StatisticsViewModel.LoadData and UI publication; no view rendering; "+reloadRequests+" overlapping requests",
          metadata.GetProperty("SoundItems").GetInt32()+" sound items, "+metadata.GetProperty("Playlists").GetInt32()+" playlists; unique file metadata",()=>
        {
          using var view=new StatisticsViewModel(new Mock<IRegionProvider>().Object,storage.Object);
          try
          {
            var load=typeof(StatisticsViewModel).GetMethod("LoadData",BindingFlags.Instance|BindingFlags.NonPublic);
            var task=Task.WhenAll(Enumerable.Range(0,reloadRequests).Select(_=>(Task)load.Invoke(view,null)));
            var frame=new DispatcherFrame();
            task.ContinueWith(_=>dispatcher.BeginInvoke(new Action(()=>frame.Continue=false),DispatcherPriority.ContextIdle),TaskScheduler.Default);
            Dispatcher.PushFrame(frame);
            task.GetAwaiter().GetResult();
            GC.KeepAlive(view.ItemsView.ToArray());GC.KeepAlive(view.SoundsItemsView.ToArray());
            GC.KeepAlive(view.VideosItemsView.ToArray());GC.KeepAlive(view.PlaylistView.ToArray());
          }
          finally
          {
            repositoryCounts.Add(contexts.Count);
            while(contexts.TryTake(out var context)) context.Dispose();
          }
        });
      }
      finally
      {
        VSynchronizationContext.UISynchronizationContext=prior;
        VSynchronizationContext.UIDispatcher=priorDispatcher;
        SynchronizationContext.SetSynchronizationContext(priorThread);
      }
      Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)));
      if(File.Exists(output)) throw new IOException("Output exists; measurements are immutable.");
      File.WriteAllText(output,JsonSerializer.Serialize(new
      {
        SchemaVersion=1,Commit=commit,CreatedUtc=DateTime.UtcNow,Runtime=Environment.Version.ToString(),
        OS=Environment.OSVersion.ToString(),ProcessorCount=Environment.ProcessorCount,Configuration="Release",
        FixtureSha256=checksum,Fixture=metadata,Metrics=metrics,ReloadRequests=reloadRequests,RepositoryCounts=repositoryCounts
      },json));
    }
    private sealed class WritableFixtureContext : AudioDatabaseContext
    {
      private readonly string database;
      public WritableFixtureContext(string database)=>this.database=database;
      protected override void OnConfiguring(DbContextOptionsBuilder builder)=>builder.UseSqlite(
        new SqliteConnectionStringBuilder {DataSource=database}.ToString());
    }
    private static void Measure(string name,string category,string boundary,string workload,Action action,int samples=9)
    {
      Console.WriteLine("Starting "+name);
      var times=new List<double>();
      var allocations=new List<long>();
      for(int i=0;i<samples;i++)
      {
        // No forced GC or warm-up before the first sample. Later samples are explicitly warm.
        long bytes=GC.GetTotalAllocatedBytes(false);
        var watch=Stopwatch.StartNew();
        action();
        watch.Stop();
        times.Add(watch.Elapsed.TotalMilliseconds);
        allocations.Add(GC.GetTotalAllocatedBytes(false)-bytes);
      }
      var sorted=times.Skip(1).OrderBy(x=>x).ToArray();
      var allocated=allocations.Skip(1).OrderBy(x=>x).ToArray();
      var metric=new Metric
      {
        Name=name,Category=category,Boundary=boundary,Workload=workload,FirstMilliseconds=times[0],
        MedianMilliseconds=sorted[sorted.Length/2],P95Milliseconds=sorted[(int)Math.Ceiling(sorted.Length*.95)-1],
        MedianAllocatedBytes=allocated[allocated.Length/2],SamplesMilliseconds=times.ToArray()
      };
      metrics.Add(metric);
      Console.WriteLine(name+": first "+metric.FirstMilliseconds.ToString("F2")+" ms; warm median "+metric.MedianMilliseconds.ToString("F2")+" ms");
    }
    private static void Run(string directory,string output,string commit)
    {
      directory=Path.GetFullPath(directory);
      var database=Path.Combine(directory,"VPlayerDatabase.db");
      var fixture=File.ReadAllText(Path.Combine(directory,"fixture.json"));
      using var stream=File.OpenRead(database);
      var checksum=BitConverter.ToString(SHA256.Create().ComputeHash(stream)).Replace("-","");
      using var metadata=JsonDocument.Parse(fixture);
      if(checksum!=metadata.RootElement.GetProperty("DatabaseSha256").GetString()) throw new InvalidOperationException("Fixture has changed since preparation.");
      int count=metadata.RootElement.GetProperty("SoundItems").GetInt32();
      List<SoundItem> sounds=null;
      Measure("Data / sound items","Data","New EF context, SQLite query and entity materialization",count+" copied sound items",()=>
      {
        using var context=new FixtureContext(database);
        sounds=context.SoundItems.AsNoTracking().Include(x=>x.FileInfoEntity).ToList();
        if(sounds.Count!=count) throw new InvalidOperationException("Incomplete data load.");
      });
      Measure("Data / artists","Data","SQLite artist query and materialization","All copied artists",()=>
      {
        using var context=new FixtureContext(database);
        GC.KeepAlive(context.Artists.AsNoTracking().ToList());
      });
      Measure("Data / albums","Data","SQLite albums and artist relationships","All copied albums",()=>
      {
        using var context=new FixtureContext(database);
        GC.KeepAlive(context.Albums.AsNoTracking().Include(x=>x.Artist).ToList());
      });
      Measure("Playlist / summaries","Playlist","SQLite playlist metadata ordered by last played","All copied playlists",()=>
      {
        using var context=new FixtureContext(database);
        GC.KeepAlive(context.SoundItemPlaylists.AsNoTracking().Where(x=>!x.IsPrivate).OrderByDescending(x=>x.LastPlayed).ToList());
      });

      var storage=new Mock<IStorageManager>().Object;
      var events=new EventAggregator();
      List<SoundItemInPlaylistViewModel> soundViews=null;
      Measure("UI / playlist view models","UI","Production SoundItemInPlaylistViewModel constructors",count+" items, no playback",()=>
      {
        soundViews=sounds.Select(x=>new SoundItemInPlaylistViewModel(x,events,storage)).ToList();
        foreach(var view in soundViews) view.Dispose();
      });

      // Run the production GetItemsToPlay method. Only services outside this boundary are replaced.
      foreach(int size in new[] {1000,10000,100000})
      {
        int actualSize=size;
        Measure("Playlist / "+size+" entries","Playlist","SongsPlaylistViewModel.GetItemsToPlay plus full enumeration",size+" entries",()=>
        {
          using var context=new FixtureContext(database);
          var model=context.SoundItemPlaylists.AsNoTracking().Single(x=>x.Name=="VPlayer benchmark "+actualSize);
          var service=new Mock<IStorageManager>();
          service.Setup(x=>x.GetTempRepository<SoundItemFilePlaylist>()).Returns(context.SoundItemPlaylists.AsNoTracking());
          using var kernel=new StandardKernel();
          kernel.Bind<IEventAggregator>().ToConstant(events);
          kernel.Bind<IStorageManager>().ToConstant(service.Object);
          var factory=new VPlayerViewModelsFactory(kernel);
          var logger=new Mock<ILogger>().Object;
          var collection=new LibraryCollection<SongsPlaylistViewModel,SoundItemFilePlaylist>(factory,service.Object,logger);
          var owner=new SoundItemPlaylistsViewModel(new Mock<IRegionProvider>().Object,factory,service.Object,
            new Mock<ISettingsProvider>().Object,collection,events);
          var list=new SongsPlaylistViewModel(model,events,factory,owner,
            new Mock<IVPlayerCloudService>().Object,service.Object,logger,new Mock<IWindowManager>().Object);
          var loaded=list.GetItemsToPlay().GetAwaiter().GetResult().ToArray();
          if(loaded.Length!=actualSize) throw new InvalidOperationException("Incomplete playlist.");
          foreach(var view in loaded) view.Dispose();
          list.Dispose();
        },6);
      }
      // Text filtering boundary; end-to-end LibraryCollection.Filter has a separate scenario below.
      Measure("UI / title search","UI","Case-insensitive title filter over loaded production models",count+" items",()=>
      {
        GC.KeepAlive(sounds.Where(x=>x.Name?.IndexOf("love",StringComparison.OrdinalIgnoreCase)>=0).ToArray());
      });
      Measure("UI / title sort","UI","Default name ordering over loaded production models",count+" items",()=>
        GC.KeepAlive(sounds.OrderBy(x=>x.Name).ToArray()));
      RunGraphics(sounds.Select(x=>new SoundItemInPlaylistViewModel(x,events,storage)).ToList());
      Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)));
      if(File.Exists(output)) throw new IOException("Output exists. Baselines and runs are immutable; choose a new file.");
      File.WriteAllText(output,JsonSerializer.Serialize(new
      {
        SchemaVersion=1,Commit=commit,CreatedUtc=DateTime.UtcNow,Runtime=Environment.Version.ToString(),
        OS=Environment.OSVersion.ToString(),ProcessorCount=Environment.ProcessorCount,
        Configuration=typeof(Program).Assembly.GetCustomAttributes(typeof(System.Reflection.AssemblyConfigurationAttribute),false)
          .Cast<System.Reflection.AssemblyConfigurationAttribute>().FirstOrDefault()?.Configuration,
        FixtureSha256=checksum,Fixture=JsonSerializer.Deserialize<JsonElement>(fixture),Metrics=metrics
      },json));
    }
    private static void ProfilePlaylist(string directory)
    {
      var database=Path.Combine(Path.GetFullPath(directory),"VPlayerDatabase.db");
      foreach(int size in new[] {1000,10000,100000})
      {
        using var context=new FixtureContext(database);
        var model=context.SoundItemPlaylists.AsNoTracking().Single(x=>x.Name=="VPlayer benchmark "+size);
        var watch=Stopwatch.StartNew();
        var playlist=context.SoundItemPlaylists.AsNoTracking()
          .Include(x=>x.PlaylistItems).ThenInclude(x=>x.ReferencedItem).ThenInclude(x=>x.FileInfoEntity)
          .Single(x=>x.Id==model.Id);
        watch.Stop();
        Console.WriteLine(size+" entries: existing collection query "+watch.Elapsed.TotalMilliseconds.ToString("F2")+" ms");
        watch.Restart();
        var ordered=playlist.PlaylistItems.OrderBy(x=>x.OrderInPlaylist).ToList();
        watch.Stop();
        Console.WriteLine(size+" entries: order "+watch.Elapsed.TotalMilliseconds.ToString("F2")+" ms");
        var events=new EventAggregator();
        using var kernel=new StandardKernel();
        kernel.Bind<IEventAggregator>().ToConstant(events);
        kernel.Bind<IStorageManager>().ToConstant(new Mock<IStorageManager>().Object);
        var factory=new VPlayerViewModelsFactory(kernel);
        watch.Restart();
        var views=ordered.Select(x=>factory.Create<SoundItemInPlaylistViewModel>(x.ReferencedItem)).ToArray();
        watch.Stop();
        Console.WriteLine(size+" entries: production factory "+watch.Elapsed.TotalMilliseconds.ToString("F2")+" ms");
        foreach(var view in views) view.Dispose();
        watch.Restart();
        var flat=context.SoundItemPlaylists.AsNoTracking()
          .Where(x=>x.Id==model.Id)
          .SelectMany(x=>x.PlaylistItems)
          .Include(x=>x.ReferencedItem).ThenInclude(x=>x.FileInfoEntity)
          .OrderBy(x=>x.OrderInPlaylist).ThenBy(x=>x.Id).ToList();
        watch.Stop();
        if(flat.Count!=size) throw new InvalidOperationException("Incomplete flat query.");
        Console.WriteLine(size+" entries: candidate flat query "+watch.Elapsed.TotalMilliseconds.ToString("F2")+" ms");
      }
    }
    private static void RunGraphics(List<SoundItemInPlaylistViewModel> views)
    {
      var dispatcher=System.Windows.Threading.Dispatcher.CurrentDispatcher;
      var prior=VSynchronizationContext.UISynchronizationContext;
      var priorDispatcher=VSynchronizationContext.UIDispatcher;
      VSynchronizationContext.UISynchronizationContext=new System.Windows.Threading.DispatcherSynchronizationContext(dispatcher);
      VSynchronizationContext.UIDispatcher=dispatcher;
      try
      {
        var windows=new Mock<IWindowManager>().Object;
        var provider=new PCloudLyricsProvider(new Mock<IPCloudService>().Object,windows,new Mock<IPCloudProvider>().Object);
        foreach(int lineCount in new[] {1000,10000,100000})
        {
          var file=new LRCFile(Enumerable.Range(0,lineCount).Select(i=>new LRCLyricLine {Timestamp=TimeSpan.FromSeconds(i),Text="Line "+i}).ToList());
          var lyrics=new LRCFileViewModel(file,VCore.WPF.LRC.LRCProviders.Local,provider,windows);
          Measure("Lyrics / "+lineCount+" lines","Lyrics","10,000 production SetActualLine calls with varying seeks",lineCount+" timed lines",()=>
          {
            for(int i=0;i<10000;i++) lyrics.SetActualLine(TimeSpan.FromSeconds((i*37)%lineCount));
          });
        }
        var renderer=new LineSpectrum {BarWidth=18};
        var bitmap=new WriteableBitmap(576,240,96,96,PixelFormats.Pbgra32,null);
        var points=Enumerable.Range(0,32).Select(i=>new Point {SpectrumPointIndex=i,Value=50+i}).ToArray();
        Measure("Spectrum / stable frames","Spectrum","1,000 production bitmap updates","576x240, 32 unchanged bars",()=>
        {
          for(int frame=0;frame<1000;frame++) renderer.UpdateSpectrumBitmap(bitmap,points,System.Drawing.Color.Red,System.Drawing.Color.Blue);
        });
        Measure("Spectrum / changing frames","Spectrum","1,000 production bitmap updates","576x240, 32 changing bars",()=>
        {
          for(int frame=0;frame<1000;frame++)
          {
            for(int i=0;i<points.Length;i++) points[i].Value=40+50*Math.Abs(Math.Sin((frame+i)*.1));
            renderer.UpdateSpectrumBitmap(bitmap,points,System.Drawing.Color.Red,System.Drawing.Color.Blue);
          }
        });
        var template=new DataTemplate();
        var text=new FrameworkElementFactory(typeof(TextBlock));
        text.SetBinding(TextBlock.TextProperty,new Binding("Name"));
        template.VisualTree=text;
        var list=new ListBox {ItemsSource=views,ItemTemplate=template,Width=900,Height=600};
        // Explicit templates make the offscreen control independent of Application/theme lookup.
        var scroll=new FrameworkElementFactory(typeof(ScrollViewer));
        scroll.SetValue(ScrollViewer.CanContentScrollProperty,true);
        scroll.SetValue(ScrollViewer.BackgroundProperty,Brushes.White);
        scroll.AppendChild(new FrameworkElementFactory(typeof(ItemsPresenter)));
        list.Template=new ControlTemplate(typeof(ListBox)) {VisualTree=scroll};
        list.ItemsPanel=new ItemsPanelTemplate(new FrameworkElementFactory(typeof(VirtualizingStackPanel)));
        var rowContent=new FrameworkElementFactory(typeof(TextBlock));
        rowContent.SetValue(TextBlock.ForegroundProperty,Brushes.Black);
        rowContent.SetValue(TextBlock.BackgroundProperty,Brushes.White);
        rowContent.SetValue(TextBlock.FontSizeProperty,14.0);
        rowContent.SetBinding(TextBlock.TextProperty,new Binding("Name") {TargetNullValue="Missing title",FallbackValue="Missing title"});
        var rowStyle=new Style(typeof(ListBoxItem));
        rowStyle.Setters.Add(new Setter(Control.TemplateProperty,new ControlTemplate(typeof(ListBoxItem)) {VisualTree=rowContent}));
        list.ItemContainerStyle=rowStyle;
        list.Background=Brushes.White;
        list.Foreground=Brushes.Black;
        list.ApplyTemplate();
        VirtualizingPanel.SetIsVirtualizing(list,true);
        VirtualizingPanel.SetVirtualizationMode(list,VirtualizationMode.Recycling);
        ScrollViewer.SetCanContentScroll(list,true);
        Measure("UI / virtualized list layout","UI","WPF measure, arrange, and offscreen render","900x600 text-row ListBox, "+views.Count+" items; component scenario",()=>
        {
          list.Measure(new Size(900,600));list.Arrange(new Rect(0,0,900,600));list.UpdateLayout();
          var image=new RenderTargetBitmap(900,600,96,96,PixelFormats.Pbgra32);image.Render(list);
        });
        if(list.ItemContainerGenerator.ContainerFromIndex(0)==null)
          throw new InvalidOperationException("Offscreen list did not generate any visible rows.");
        var snapshot=new RenderTargetBitmap(900,600,96,96,PixelFormats.Pbgra32);
        snapshot.Render(list);
        var pixels=new byte[900*600*4];
        snapshot.CopyPixels(pixels,900*4,0);
        if(!Enumerable.Range(0,900*600).Any(i=>pixels[i*4+3]>0 && pixels[i*4]>0))
          throw new InvalidOperationException("Offscreen list rendered no visible row backgrounds.");
        var encoder=new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(snapshot));
        var visualDirectory=Path.Combine(Environment.CurrentDirectory,"artifacts","visual-tests");
        Directory.CreateDirectory(visualDirectory);
        using(var png=File.Create(Path.Combine(visualDirectory,"performance-list.png"))) encoder.Save(png);
        int index=0;
        Measure("UI / virtualized list scroll","UI","ScrollIntoView, layout and offscreen render","Same ListBox, jumps of 997 rows; component scenario",()=>
        {
          index=(index+997)%views.Count;list.ScrollIntoView(views[index]);list.UpdateLayout();
          if(list.ItemContainerGenerator.ContainerFromIndex(index)==null)
            throw new InvalidOperationException("Scroll did not realize the requested row.");
          var image=new RenderTargetBitmap(900,600,96,96,PixelFormats.Pbgra32);image.Render(list);
        });
      }
      finally
      {
        VSynchronizationContext.UISynchronizationContext=prior;
        VSynchronizationContext.UIDispatcher=priorDispatcher;
      }
    }
  }
}
