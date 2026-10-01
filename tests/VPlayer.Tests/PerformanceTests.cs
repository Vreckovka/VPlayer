using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using VPlayer.AudioStorage.DataLoader;
using Xunit;
using Xunit.Abstractions;

namespace VPlayer.Tests
{
  public partial class PerformanceTests
  {
    private readonly ITestOutputHelper output;
    public PerformanceTests(ITestOutputHelper output) => this.output=output;
    private static (double Milliseconds,long Allocated) Measure(Action action,int count)
    {
      for(int i=0;i<100;i++) action();
      var watch=new Stopwatch();
      var before=GC.GetAllocatedBytesForCurrentThread();
      watch.Start();
      for(int i=0;i<count;i++) action();
      watch.Stop();
      return (watch.Elapsed.TotalMilliseconds,GC.GetAllocatedBytesForCurrentThread()-before);
    }
    [Fact]
    [Trait("Category","Performance")]
    public void CompareLyricsSearchWithOriginalQuery()
    {
      var lyrics=LyricsTests.Create(Enumerable.Range(0,1000).Select(x=>(double)x).ToArray());
      int step=0;
      var original=Measure(()=>
      {
        var time=TimeSpan.FromSeconds((step++*37)%1000);
        var line=lyrics.AllLine.Where(x=>x.Model.Timestamp.HasValue && x.Model.Timestamp.Value<=time)
          .OrderByDescending(x=>x.Model.Timestamp).FirstOrDefault();
        GC.KeepAlive(line);
      },10000);
      step=0;
      var indexed=Measure(()=>lyrics.SetActualLine(TimeSpan.FromSeconds((step++*37)%1000)),10000);
      output.WriteLine($"10,000 lyric selections / 1,000 lines: original {original.Milliseconds:F2} ms / {original.Allocated} bytes; indexed {indexed.Milliseconds:F2} ms / {indexed.Allocated} bytes.");
      var directory=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"..","..","..","..","..","..","artifacts","performance"));
      Directory.CreateDirectory(directory);
      File.WriteAllText(Path.Combine(directory,"lyrics.json"),JsonSerializer.Serialize(new {
        Runtime=Environment.Version.ToString(),Selections=10000,Lines=1000,
        OriginalMilliseconds=original.Milliseconds,IndexedMilliseconds=indexed.Milliseconds,
        OriginalAllocatedBytes=original.Allocated,IndexedAllocatedBytes=indexed.Allocated
      },new JsonSerializerOptions {WriteIndented=true}));
      foreach(var seconds in new[] {0,15,999,500})
      {
        lyrics.SetActualLine(TimeSpan.FromSeconds(seconds));
        Assert.Equal(TimeSpan.FromSeconds(seconds),lyrics.ActualLine.Model.Timestamp);
      }
    }
    [Fact]
    [Trait("Category","Performance")]
    public void CompareDirectoryScanWithOriginalExtensionPasses()
    {
      var directory=Path.Combine(Path.GetTempPath(),"VPlayerBenchmark-"+Guid.NewGuid());
      Directory.CreateDirectory(directory);
      try
      {
        var extensions=new[] {".mp3",".flac",".m4a",".ogg",".wav"};
        for(int i=0;i<500;i++) File.WriteAllText(Path.Combine(directory,$"track-{i}"+extensions[i%5]),"");
        var patterns=extensions.Select(x=>"*"+x).ToArray();
        var info=new DirectoryInfo(directory);
        var original=Measure(()=>GC.KeepAlive(patterns.SelectMany(pattern=>info.GetFiles(pattern)).ToArray()),30);
        var single=Measure(()=>GC.KeepAlive(MediaFileDiscovery.EnumerateFiles(directory,patterns,false).ToArray()),30);
        output.WriteLine($"30 scans / 500 files / 5 extensions: original {original.Milliseconds:F2} ms / {original.Allocated} bytes; single enumeration {single.Milliseconds:F2} ms / {single.Allocated} bytes.");
        Assert.Equal(500,MediaFileDiscovery.EnumerateFiles(directory,patterns,false).Count());
        var artifacts=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"..","..","..","..","..","..","artifacts","performance"));
        Directory.CreateDirectory(artifacts);
        File.WriteAllText(Path.Combine(artifacts,"discovery.json"),JsonSerializer.Serialize(new {
          Runtime=Environment.Version.ToString(),Scans=30,Files=500,Extensions=5,
          OriginalMilliseconds=original.Milliseconds,SingleEnumerationMilliseconds=single.Milliseconds,
          OriginalAllocatedBytes=original.Allocated,SingleEnumerationAllocatedBytes=single.Allocated
        },new JsonSerializerOptions {WriteIndented=true}));
      }
      finally {Directory.Delete(directory,true);}
    }
  }
}
