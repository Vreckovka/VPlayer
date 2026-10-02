using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VPLayer.Domain.Diagnostics;
using Xunit;

namespace VPlayer.Tests
{
  public class BufferedDiagnosticWriterTests
  {
    [Fact]
    public void SlowDiskDoesNotBlockProducersAndFlushSavesLatestSnapshot()
    {
      using var entered=new ManualResetEventSlim();
      using var release=new ManualResetEventSlim();
      var written=new List<string>();
      using var writer=new BufferedDiagnosticWriter(value=>
      {
        entered.Set();
        if(!release.Wait(TimeSpan.FromSeconds(10))) throw new TimeoutException();
        written.Add(value);
      });
      try
      {
        writer.Post("initial");
        Assert.True(entered.Wait(TimeSpan.FromSeconds(5)));
        var producers=Task.Run(()=>Parallel.For(0,10000,i=>writer.Post(i.ToString())));
        Assert.True(producers.Wait(TimeSpan.FromSeconds(5)));
        writer.Post("final");
        var flush=Task.Run(()=>writer.Flush());
        Assert.False(flush.Wait(TimeSpan.FromMilliseconds(50)));
        release.Set();
        Assert.True(flush.Wait(TimeSpan.FromSeconds(5)));
        Assert.Equal("final",written.Last());
      }
      finally {release.Set();}
    }

    [Fact]
    public void DisposeWaitsForPendingSnapshotAndRejectsLaterPosts()
    {
      string saved=null;
      var writer=new BufferedDiagnosticWriter(value=>saved=value);
      writer.Post("final");
      writer.Dispose();
      Assert.Equal("final",saved);
      Assert.Throws<ObjectDisposedException>(()=>writer.Post("late"));
    }

    [Fact]
    public void DiskFailureIsReportedByFlushWithoutHanging()
    {
      var expected=new UnauthorizedAccessException("Simulated inaccessible output");
      var writer=new BufferedDiagnosticWriter(value=>throw expected);
      writer.Post("snapshot");
      var failure=Assert.Throws<IOException>(()=>writer.Flush());
      Assert.Same(expected,failure.InnerException);
      Assert.Throws<IOException>(()=>writer.Dispose());
    }
  }
}
