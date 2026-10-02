using System;
using System.Collections.Concurrent;

namespace VPLayer.Domain.Diagnostics
{
  // Correlates EF start/end callbacks, including exceptions, without keeping
  // completed readers or connections alive. Used only by the profiling mode.
  public sealed class DiagnosticOperationTracker
  {
    private readonly Func<string,IDisposable> start;
    private readonly ConcurrentDictionary<Guid,IDisposable> scopes=new ConcurrentDictionary<Guid,IDisposable>();
    public DiagnosticOperationTracker(Func<string,IDisposable> start)
    {this.start=start ?? throw new ArgumentNullException(nameof(start));}
    public int ActiveCount=>scopes.Count;
    public void Begin(Guid id,string name)
    {
      var scope=start(name);
      if(scope==null) return;
      if(!scopes.TryAdd(id,scope))
      {
        scope.Dispose();
        throw new InvalidOperationException("A diagnostic operation with this ID is already active.");
      }
    }
    public bool End(Guid id)
    {
      if(!scopes.TryRemove(id,out var scope)) return false;
      scope.Dispose();
      return true;
    }
  }
}