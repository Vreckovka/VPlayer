using System;
using System.IO;
using LibVLCSharp.Shared;

namespace VPlayer.VlcCache
{
  internal static class Program
  {
    public static int Main(string[] args)
    {
      try
      {
        if(args.Length!=1) throw new ArgumentException("Expected the published win-x64 LibVLC directory.");
        var directory=Path.GetFullPath(args[0]);
        var plugins=Path.Combine(directory,"plugins");
        if(!File.Exists(Path.Combine(directory,"libvlc.dll")) || !File.Exists(Path.Combine(directory,"libvlccore.dll")) ||
           !Directory.Exists(plugins) || Directory.GetFiles(plugins,"*.dll",SearchOption.AllDirectories).Length==0)
          throw new DirectoryNotFoundException("The published LibVLC payload is incomplete: "+directory);
        var cache=Path.Combine(plugins,"plugins.dat");
        // A stale file must not count as success when native cache generation cannot write its replacement.
        if(File.Exists(cache)) File.Delete(cache);
        // Generate only for this payload, without inherited external plugin directories or user VLC preferences.
        Environment.SetEnvironmentVariable("VLC_PLUGIN_PATH",null);
        Core.Initialize(directory);
        using(var lib=new LibVLC("--ignore-config","--reset-plugins-cache"))
          Console.WriteLine("Preparing plugin cache for LibVLC "+lib.Version);
        if(!File.Exists(cache) || new FileInfo(cache).Length==0)
          throw new IOException("LibVLC did not generate a nonempty plugin cache: "+cache);
        Console.WriteLine("Prepared "+cache);
        return 0;
      }
      catch(Exception error)
      {
        Console.Error.WriteLine(error.Message);
        return 1;
      }
    }
  }
}