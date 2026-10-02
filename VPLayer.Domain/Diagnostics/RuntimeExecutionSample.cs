using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace VPLayer.Domain.Diagnostics
{
  public readonly struct RuntimeExecutionSample
  {
    public int ThreadId {get;}
    public double? ThreadCpuMilliseconds {get;}
    public int Gen0 {get;}
    public int Gen1 {get;}
    public int Gen2 {get;}
    public int AvailableWorkers {get;}
    public int WorkerCount {get;}
    public long PendingWork {get;}

    private RuntimeExecutionSample(int threadId,double? cpu,int gen0,int gen1,int gen2,int available,int workers,long pending)
    {
      ThreadId=threadId;ThreadCpuMilliseconds=cpu;Gen0=gen0;Gen1=gen1;Gen2=gen2;
      AvailableWorkers=available;WorkerCount=workers;PendingWork=pending;
    }

    public static RuntimeExecutionSample Capture()
    {
      ThreadPool.GetAvailableThreads(out var available,out _);
      return new RuntimeExecutionSample(Thread.CurrentThread.ManagedThreadId,ReadThreadCpu(),
        GC.CollectionCount(0),GC.CollectionCount(1),GC.CollectionCount(2),available,
        ThreadPool.ThreadCount,ThreadPool.PendingWorkItemCount);
    }

    public RuntimeExecutionMetrics Since(RuntimeExecutionSample start) => new RuntimeExecutionMetrics
    {
      // Async scopes can resume on another thread; their CPU delta is unavailable.
      ThreadCpuMilliseconds=ThreadId==start.ThreadId && ThreadCpuMilliseconds.HasValue && start.ThreadCpuMilliseconds.HasValue
        ? ThreadCpuMilliseconds-start.ThreadCpuMilliseconds : null,
      Gen0Collections=Gen0-start.Gen0,Gen1Collections=Gen1-start.Gen1,Gen2Collections=Gen2-start.Gen2,
      AvailableWorkersStart=start.AvailableWorkers,AvailableWorkersEnd=AvailableWorkers,
      WorkerCountStart=start.WorkerCount,WorkerCountEnd=WorkerCount,
      PendingWorkStart=start.PendingWork,PendingWorkEnd=PendingWork
    };

    private static double? ReadThreadCpu()
    {
      if(!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return null;
      if(!GetThreadTimes(GetCurrentThread(),out _,out _,out var kernel,out var user)) return null;
      return (kernel.Ticks+user.Ticks)/10000d;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct FileTime
    {
      public uint Low,High;
      public ulong Ticks => ((ulong)High<<32)|Low;
    }
    [DllImport("kernel32.dll",ExactSpelling=true)]
    private static extern IntPtr GetCurrentThread();
    [DllImport("kernel32.dll",ExactSpelling=true)]
    [return:MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetThreadTimes(IntPtr thread,out FileTime creation,out FileTime exit,out FileTime kernel,out FileTime user);
  }

  public sealed class RuntimeExecutionMetrics
  {
    public double? ThreadCpuMilliseconds {get;set;}
    public int Gen0Collections {get;set;}
    public int Gen1Collections {get;set;}
    public int Gen2Collections {get;set;}
    public int AvailableWorkersStart {get;set;}
    public int AvailableWorkersEnd {get;set;}
    public int WorkerCountStart {get;set;}
    public int WorkerCountEnd {get;set;}
    public long PendingWorkStart {get;set;}
    public long PendingWorkEnd {get;set;}
  }
}
