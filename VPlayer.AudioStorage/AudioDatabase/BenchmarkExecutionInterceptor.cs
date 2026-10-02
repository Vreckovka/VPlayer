using System;
using System.Data.Common;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Diagnostics;
using VPLayer.Domain.Diagnostics;

namespace VPlayer.AudioStorage.AudioDatabase
{
  internal sealed class BenchmarkExecutionInterceptor : DbCommandInterceptor
  {
    public static readonly BenchmarkExecutionInterceptor Instance=new BenchmarkExecutionInterceptor(StartupMeasurements.MeasureDatabase);
    private readonly DiagnosticOperationTracker operations;
    internal BenchmarkExecutionInterceptor(Func<string,IDisposable> start) => operations=new DiagnosticOperationTracker(start);
    private void Begin(DbCommand command,CommandEventData data)
    {
      var match=Regex.Match(command.CommandText,"FROM\\s+\"([^\"]+)\"");
      operations.Begin(data.CommandId,"Database / execute / "+(match.Success?match.Groups[1].Value:"other"));
    }
    public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command,CommandEventData data,InterceptionResult<DbDataReader> result)
    {Begin(command,data);return result;}
    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
      DbCommand command,CommandEventData data,InterceptionResult<DbDataReader> result,CancellationToken cancellationToken=default)
    {Begin(command,data);return new ValueTask<InterceptionResult<DbDataReader>>(result);}
    public override DbDataReader ReaderExecuted(DbCommand command,CommandExecutedEventData data,DbDataReader result)
    {operations.End(data.CommandId);return result;}
    public override ValueTask<DbDataReader> ReaderExecutedAsync(
      DbCommand command,CommandExecutedEventData data,DbDataReader result,CancellationToken cancellationToken=default)
    {operations.End(data.CommandId);return new ValueTask<DbDataReader>(result);}
    public override void CommandFailed(DbCommand command,CommandErrorEventData data) => operations.End(data.CommandId);
    public override Task CommandFailedAsync(DbCommand command,CommandErrorEventData data,CancellationToken cancellationToken=default)
    {operations.End(data.CommandId);return Task.CompletedTask;}
  }
}