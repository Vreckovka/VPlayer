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
    private static readonly List<object> phases=new List<object>();
    private static readonly List<string> active=new List<string>();
    private static string status="Starting";
    private static string failure;
    public static bool Enabled=>!string.IsNullOrWhiteSpace(output);
    public static IDisposable Measure(string name)=>Enabled?new Scope(name):null;
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
          Milliseconds=(DateTime.UtcNow-Process.GetCurrentProcess().StartTime.ToUniversalTime()).TotalMilliseconds});
        status="Rendered";
        Write(status,failure);
      }
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
        Write(status,failure);
      }
    }
    private static void Write(string status,string failure)
    {
      var path=Path.GetFullPath(output);
      Directory.CreateDirectory(Path.GetDirectoryName(path));
      File.WriteAllText(path,JsonSerializer.Serialize(new
      {
        Status=status,FailureType=failure,ActivePhases=active.ToArray(),Commit=Environment.GetEnvironmentVariable("VPLAYER_PERFORMANCE_COMMIT"),
        Runtime=Environment.Version.ToString(),CreatedUtc=DateTime.UtcNow,Phases=phases
      },new JsonSerializerOptions {WriteIndented=true}));
    }
    private sealed class Scope : IDisposable
    {
      private readonly string name;
      private readonly Stopwatch stopwatch=Stopwatch.StartNew();
      public Scope(string name)
      {
        this.name=name;
        lock(phases) {active.Add(name);Write(status,failure);}
      }
      public void Dispose()
      {
        stopwatch.Stop();
        lock(phases)
        {
          active.Remove(name);
          phases.Add(new {Name=name,Milliseconds=stopwatch.Elapsed.TotalMilliseconds});
          Write(status,failure);
        }
      }
    }
  }
}
