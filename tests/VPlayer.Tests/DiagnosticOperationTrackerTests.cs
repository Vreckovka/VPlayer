using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VPLayer.Domain.Diagnostics;
using Xunit;

namespace VPlayer.Tests
{
  public class DiagnosticOperationTrackerTests
  {
    private sealed class Scope : IDisposable
    {
      private readonly Action complete;
      private int disposed;
      public Scope(Action complete) => this.complete=complete;
      public void Dispose()
      {
        Assert.Equal(0,Interlocked.Exchange(ref disposed,1));
        complete();
      }
    }
    [Fact]
    public async Task ConcurrentOperationsFinishExactlyOnceWithoutRetainingCompletedScopes()
    {
      var completed=new ConcurrentDictionary<int,int>();
      var tracker=new DiagnosticOperationTracker(name=>
      {
        int id=int.Parse(name);
        return new Scope(()=>completed.AddOrUpdate(id,1,(_,value)=>value+1));
      });
      await Task.WhenAll(Enumerable.Range(0,64).Select(worker=>Task.Run(()=>
      {
        for(int i=worker;i<10000;i+=64)
        {
          var operation=Guid.NewGuid();
          tracker.Begin(operation,i.ToString());
          Assert.True(tracker.End(operation));
          Assert.False(tracker.End(operation));
        }
      })));
      Assert.Equal(0,tracker.ActiveCount);
      Assert.Equal(10000,completed.Count);
      Assert.All(completed.Values,value=>Assert.Equal(1,value));
    }
    [Fact]
    public void DuplicateIdsDisposeNewScopeAndPreserveTheExistingOperation()
    {
      int disposed=0;
      var tracker=new DiagnosticOperationTracker(_=>new Scope(()=>disposed++));
      var id=Guid.NewGuid();
      tracker.Begin(id,"first");
      Assert.Throws<InvalidOperationException>(()=>tracker.Begin(id,"duplicate"));
      Assert.Equal(1,disposed);
      Assert.Equal(1,tracker.ActiveCount);
      Assert.True(tracker.End(id));
      Assert.Equal(2,disposed);
      Assert.False(tracker.End(id));
      Assert.Equal(0,tracker.ActiveCount);
    }
    [Fact]
    public void FactoryAndCompletionFailuresDoNotLeaveActiveOperations()
    {
      bool failFactory=true;
      var tracker=new DiagnosticOperationTracker(_=>
      {
        if(failFactory) throw new InvalidOperationException("Failed start.");
        return new Scope(()=>throw new InvalidOperationException("Failed finish."));
      });
      var id=Guid.NewGuid();
      Assert.Throws<InvalidOperationException>(()=>tracker.Begin(id,"failure"));
      Assert.Equal(0,tracker.ActiveCount);
      failFactory=false;
      tracker.Begin(id,"retry");
      Assert.Throws<InvalidOperationException>(()=>tracker.End(id));
      Assert.Equal(0,tracker.ActiveCount);
      Assert.False(tracker.End(id));
    }
  }
}