using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace VPLayer.Domain.Diagnostics
{
  // Opt-in diagnostics. With no output file, the application does not allocate timing scopes.
  public static class StartupMeasurements
  {
    private static readonly string output=Environment.GetEnvironmentVariable("VPLAYER_PERFORMANCE_RUN_FILE");
    private static readonly DateTime processStart=Enabled?Process.GetCurrentProcess().StartTime.ToUniversalTime():default;
    private static readonly List<object> phases=new List<object>();
    private static readonly Dictionary<string,long> observations=new Dictionary<string,long>();
    private static readonly List<string> active=new List<string>();
    private static readonly List<object> databaseReads=new List<object>();
    private static long diagnosticWrites;
    private static double diagnosticWriteMilliseconds;
    private static double longestDiagnosticWriteMilliseconds;
    private static readonly object writeMetricsGate=new object();
    private static readonly BufferedDiagnosticWriter writer;
    static StartupMeasurements()
    {
      if(!Enabled) return;
      writer=new BufferedDiagnosticWriter(WriteSnapshot);
      AppDomain.CurrentDomain.ProcessExit+=(sender,args)=> {try {writer.Flush();} catch {}};
    }
    public static void Flush() {writer?.Flush();}
    private static string status="Starting";
    private static string failure;
    public static bool Enabled=>!string.IsNullOrWhiteSpace(output);
    private static readonly bool cpuProfile=Enabled && Environment.GetEnvironmentVariable("VPLAYER_PERFORMANCE_CPU_PROFILE")=="1";
    public static IDisposable Measure(string name)=>Enabled?new Scope(name):null;
    public static IDisposable MeasureLibrary(string phase,Type model)=>Enabled?Measure("Application / library "+model.Name+" / "+phase):null;
    public static void Start()
    {
      if(Enabled) lock(phases) Write(status,failure);
    }
    public static void Complete()
    {
      if(!Enabled) return;
      lock(phases)
      {
        phases.Add(new {Name="Application / first window render",
          Milliseconds=(DateTime.UtcNow-processStart).TotalMilliseconds});
        status="Rendered";
        Write(status,failure);
      }
    }
    public static void RecordProcessMilestone(string name)
    {
      if(!Enabled) return;
      lock(phases)
      {
        phases.Add(new {Name=name,Milliseconds=(DateTime.UtcNow-processStart).TotalMilliseconds});
        Write(status,failure);
      }
    }
    public static void RecordObservation(string name,long value)
    {
      if(!Enabled) return;
      lock(phases) {observations[name]=value;Write(status,failure);}
    }
    public static void RecordDatabaseRead(string table,bool includesFileMetadata,DateTimeOffset start,double duration,int readOperations)
    {
      if(!Enabled) return;
      lock(phases) databaseReads.Add(new
      {
        Table=table,IncludesFileMetadata=includesFileMetadata,ReadOperations=readOperations,
        Milliseconds=duration,StartedMilliseconds=(start.UtcDateTime-processStart).TotalMilliseconds
      });
    }
    public static void Fail(Exception exception)
    {
      if(!Enabled) return;
      lock(phases)
      {
        status="Failed";
        var types=new List<string>();
        for(var current=exception;current!=null;current=current.InnerException) types.Add(current.GetType().FullName);
        failure=string.Join(" -> ",types);
        File.WriteAllText(Path.GetFullPath(output)+".failure.log",exception.ToString());
        Write(status,failure);
      }
    }
    private static void Write(string status,string failure)
    {
      object metrics;
      lock(writeMetricsGate) metrics=new {Count=diagnosticWrites,Milliseconds=diagnosticWriteMilliseconds,LongestMilliseconds=longestDiagnosticWriteMilliseconds};
      writer.Post(JsonSerializer.Serialize(new
      {
        Status=status,FailureType=failure,ActivePhases=active.ToArray(),Commit=Environment.GetEnvironmentVariable("VPLAYER_PERFORMANCE_COMMIT"),
        Runtime=Environment.Version.ToString(),CreatedUtc=DateTime.UtcNow,Phases=phases,Observations=observations,
        DatabaseReads=databaseReads,DiagnosticWrites=metrics,DiagnosticMode="buffered-v1",DiagnosticProfile=cpuProfile?"cpu-v1":"none"
      },new JsonSerializerOptions {WriteIndented=true}));
    }
    private static void WriteSnapshot(string snapshot)
    {
      var watch=Stopwatch.StartNew();
      var path=Path.GetFullPath(output);
      Directory.CreateDirectory(Path.GetDirectoryName(path));
      // An interrupted process retains the last complete JSON snapshot.
      var temporary=path+".tmp";
      File.WriteAllText(temporary,snapshot);
      File.Move(temporary,path,true);
      watch.Stop();
      lock(writeMetricsGate)
      {
        diagnosticWrites++;
        diagnosticWriteMilliseconds+=watch.Elapsed.TotalMilliseconds;
        longestDiagnosticWriteMilliseconds=Math.Max(longestDiagnosticWriteMilliseconds,watch.Elapsed.TotalMilliseconds);
      }
    }
    private sealed class Scope : IDisposable
    {
      private readonly string name;
      private readonly RuntimeExecutionSample executionStart=cpuProfile?RuntimeExecutionSample.Capture():default;
      private readonly double started=(DateTime.UtcNow-processStart).TotalMilliseconds;
      private readonly Stopwatch stopwatch=Stopwatch.StartNew();
      public Scope(string name)
      {
        this.name=name;
        lock(phases) {active.Add(name);Write(status,failure);}
      }
      public void Dispose()
      {
        stopwatch.Stop();
        var execution=cpuProfile?RuntimeExecutionSample.Capture().Since(executionStart):null;
        lock(phases)
        {
          active.Remove(name);
          phases.Add(new {Name=name,Milliseconds=stopwatch.Elapsed.TotalMilliseconds,
            StartedMilliseconds=started,CompletedMilliseconds=started+stopwatch.Elapsed.TotalMilliseconds,Execution=execution});
          Write(status,failure);
        }
      }
    }
  }
}
