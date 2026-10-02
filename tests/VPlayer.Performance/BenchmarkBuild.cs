using System;

namespace VPlayer.Performance
{
  internal static class BenchmarkBuild
  {
#if DEBUG
    internal static readonly string Configuration="Debug";
#else
    internal static readonly string Configuration="Release";
#endif
    internal static void EnsureDebug()
    {
      if(Configuration!="Debug")
        throw new InvalidOperationException("The user requires Debug benchmarks. Build with -c Debug -p:Platform=x64.");
    }
  }
}