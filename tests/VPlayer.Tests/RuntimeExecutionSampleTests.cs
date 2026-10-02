using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using VPLayer.Domain.Diagnostics;
using Xunit;

namespace VPlayer.Tests
{
  public class RuntimeExecutionSampleTests
  {
    [Fact]
    public void BusyWorkHasCpuCostAndRuntimeCountersRemainValid()
    {
      var start=RuntimeExecutionSample.Capture();
      var watch=Stopwatch.StartNew();
      var end=start;
      do
      {
        Thread.SpinWait(10000);
        end=RuntimeExecutionSample.Capture();
      } while(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
        end.Since(start).ThreadCpuMilliseconds.GetValueOrDefault()<1 && watch.Elapsed<TimeSpan.FromSeconds(3));
      var delta=end.Since(start);
      if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
      {
        Assert.NotNull(delta.ThreadCpuMilliseconds);
        Assert.True(delta.ThreadCpuMilliseconds>=1);
        Assert.True(delta.ThreadCpuMilliseconds<=watch.Elapsed.TotalMilliseconds+20);
      }
      Assert.True(delta.Gen0Collections>=0 && delta.Gen1Collections>=0 && delta.Gen2Collections>=0);
      Assert.True(delta.AvailableWorkersStart>=0 && delta.AvailableWorkersEnd>=0);
      Assert.True(delta.WorkerCountStart>=0 && delta.WorkerCountEnd>=0);
      Assert.True(delta.PendingWorkStart>=0 && delta.PendingWorkEnd>=0);
    }

    [Fact]
    public void MovingBetweenThreadsDoesNotProduceAnInvalidCpuDelta()
    {
      var start=RuntimeExecutionSample.Capture();
      RuntimeExecutionMetrics delta=null;
      var thread=new Thread(()=>delta=RuntimeExecutionSample.Capture().Since(start));
      thread.Start();
      Assert.True(thread.Join(TimeSpan.FromSeconds(5)));
      Assert.NotNull(delta);
      Assert.Null(delta.ThreadCpuMilliseconds);
    }
  }
}
