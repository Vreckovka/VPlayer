using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Diagnostics;
using VPLayer.Domain.Diagnostics;

namespace VPlayer.AudioStorage.AudioDatabase
{
  internal sealed class BenchmarkConnectionInterceptor : DbConnectionInterceptor
  {
    public static readonly BenchmarkConnectionInterceptor Instance=new BenchmarkConnectionInterceptor(StartupMeasurements.MeasureDatabase);
    private readonly DiagnosticOperationTracker operations;
    internal BenchmarkConnectionInterceptor(Func<string,IDisposable> start) => operations=new DiagnosticOperationTracker(start);
    public override InterceptionResult ConnectionOpening(DbConnection connection,ConnectionEventData data,InterceptionResult result)
    {operations.Begin(data.ConnectionId,"Database / connection open");return result;}
    public override ValueTask<InterceptionResult> ConnectionOpeningAsync(
      DbConnection connection,ConnectionEventData data,InterceptionResult result,CancellationToken cancellationToken=default)
    {operations.Begin(data.ConnectionId,"Database / connection open");return new ValueTask<InterceptionResult>(result);}
    public override void ConnectionOpened(DbConnection connection,ConnectionEndEventData data) => operations.End(data.ConnectionId);
    public override Task ConnectionOpenedAsync(DbConnection connection,ConnectionEndEventData data,CancellationToken cancellationToken=default)
    {operations.End(data.ConnectionId);return Task.CompletedTask;}
    public override void ConnectionFailed(DbConnection connection,ConnectionErrorEventData data) => operations.End(data.ConnectionId);
    public override Task ConnectionFailedAsync(DbConnection connection,ConnectionErrorEventData data,CancellationToken cancellationToken=default)
    {operations.End(data.ConnectionId);return Task.CompletedTask;}
  }
}