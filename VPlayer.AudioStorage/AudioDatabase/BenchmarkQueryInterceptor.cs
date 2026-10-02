using System.Data.Common;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore.Diagnostics;
using VPLayer.Domain.Diagnostics;

namespace VPlayer.AudioStorage.AudioDatabase
{
  // Enabled only for benchmark output. Capture reader lifetime (including row
  // enumeration) in memory; do not write another file for each query.
  internal sealed class BenchmarkQueryInterceptor : DbCommandInterceptor
  {
    public static readonly BenchmarkQueryInterceptor Instance=new BenchmarkQueryInterceptor();
    public override InterceptionResult DataReaderDisposing(DbCommand command,DataReaderDisposingEventData eventData,InterceptionResult result)
    {
      var match=Regex.Match(command.CommandText,"FROM\\s+\"([^\"]+)\"");
      StartupMeasurements.RecordDatabaseRead(match.Success?match.Groups[1].Value:"other",
        command.CommandText.Contains("\"FileInfos\""),eventData.StartTime,eventData.Duration.TotalMilliseconds,eventData.ReadCount);
      return result;
    }
  }
}
