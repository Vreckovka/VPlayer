using System;
using System.IO;
using System.Threading;

namespace VPLayer.Domain.Diagnostics
{
  // Coalesce progress snapshots on a dedicated thread. Callers never wait for
  // file I/O; Flush is the explicit boundary for the final complete snapshot.
  public sealed class BufferedDiagnosticWriter : IDisposable
  {
    private readonly object gate=new object();
    private readonly Action<string> write;
    private readonly Thread thread;
    private string pending;
    private long revision,savedRevision;
    private bool stopping;
    private Exception failure;

    public BufferedDiagnosticWriter(Action<string> write)
    {
      this.write=write ?? throw new ArgumentNullException(nameof(write));
      thread=new Thread(Work) {IsBackground=true,Name="VPlayer diagnostics"};
      thread.Start();
    }

    public void Post(string snapshot)
    {
      if(snapshot==null) throw new ArgumentNullException(nameof(snapshot));
      lock(gate)
      {
        CheckFailure();
        if(stopping) throw new ObjectDisposedException(nameof(BufferedDiagnosticWriter));
        pending=snapshot;
        revision++;
        Monitor.PulseAll(gate);
      }
    }

    public void Flush()
    {
      lock(gate)
      {
        long target=revision;
        while(savedRevision<target)
        {
          CheckFailure();
          Monitor.PulseAll(gate);
          Monitor.Wait(gate);
        }
        CheckFailure();
      }
    }

    private void CheckFailure()
    {
      if(failure!=null) throw new IOException("Could not write benchmark diagnostics.",failure);
    }

    private void Work()
    {
      while(true)
      {
        string snapshot;
        long target;
        lock(gate)
        {
          while(revision==savedRevision && !stopping) Monitor.Wait(gate);
          if(revision==savedRevision && stopping) return;
        }
        Thread.Sleep(100);
        lock(gate) {snapshot=pending;target=revision;}
        try {write(snapshot);}
        catch(Exception error)
        {
          lock(gate) {failure=error;Monitor.PulseAll(gate);}
          return;
        }
        lock(gate) {savedRevision=target;Monitor.PulseAll(gate);}
      }
    }

    public void Dispose()
    {
      try {Flush();}
      finally
      {
        lock(gate) {stopping=true;Monitor.PulseAll(gate);}
        thread.Join();
      }
    }
  }
}
