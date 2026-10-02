using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using LibVLCSharp.Shared;
using VVLC.Providers;

namespace VPlayer.Performance
{
  // One process per sample: native libraries and module banks cannot be reset inside a process.
  internal static class VlcStartupBenchmarks
  {
    public static void Run(string nativeDirectory, string output, string commit)
    {
      nativeDirectory=Path.GetFullPath(nativeDirectory);
      output=Path.GetFullPath(output);
      if(File.Exists(output)) throw new IOException("Use a new result path.");
      if(commit.Length!=40 || commit.Any(c=>!Uri.IsHexDigit(c))) throw new ArgumentException("An exact source commit is required.");
      var pluginDirectory=Path.Combine(nativeDirectory,"plugins");
      var cache=Path.Combine(pluginDirectory,"plugins.dat");
      var cacheBefore=File.Exists(cache)?Hash(cache):null;
      var clock=Stopwatch.StartNew();
      LibVLCSharp.Shared.Core.Initialize(nativeDirectory);
      var nativeLoad=clock.Elapsed.TotalMilliseconds;
      clock.Restart();
      using var lib=new VlcProvider().InitlizeVlc();
      var construction=clock.Elapsed.TotalMilliseconds;
      var files=Directory.GetFiles(nativeDirectory,"*.dll",SearchOption.AllDirectories)
        .OrderBy(x=>Path.GetRelativePath(nativeDirectory,x),StringComparer.Ordinal).ToArray();
      if(files.Length<2 || !File.Exists(Path.Combine(nativeDirectory,"libvlc.dll"))) throw new IOException("Incomplete native payload.");
      using var digest=SHA256.Create();
      var payload=string.Join("\n",files.Select(x=>Path.GetRelativePath(nativeDirectory,x)+"|"+Hash(x)));
      var payloadHash=BitConverter.ToString(digest.ComputeHash(Encoding.UTF8.GetBytes(payload))).Replace("-","");
      var audioFilters=lib.AudioFilters.Select(x=>x.Name).OrderBy(x=>x,StringComparer.Ordinal).ToArray();
      var videoFilters=lib.VideoFilters.Select(x=>x.Name).OrderBy(x=>x,StringComparer.Ordinal).ToArray();
      if(audioFilters.Length==0 || videoFilters.Length==0) throw new InvalidOperationException("Missing native modules.");
      // Decoded frames are captured, never sent to the user's audio device.
      var wave=Path.ChangeExtension(output,".wav");
      WriteWave(wave);
      long frames=0;
      long failures=0;
      using(var ended=new ManualResetEventSlim())
      using(var media=new Media(lib,wave,FromType.FromPath))
      using(var player=new MediaPlayer(lib))
      {
        player.SetAudioFormat("S16N",48000,1);
        player.SetAudioCallbacks((data,samples,count,pts)=>Interlocked.Add(ref frames,count),null,null,null,null);
        player.EndReached+=(sender,args)=>ended.Set();
        player.EncounteredError+=(sender,args)=>{Interlocked.Increment(ref failures);ended.Set();};
        if(!player.Play(media) || !ended.Wait(TimeSpan.FromSeconds(15))) throw new InvalidOperationException("Local audio decode did not complete.");
        player.Stop();
        if(failures!=0 || frames!=96000 || media.Duration!=2000) throw new InvalidOperationException($"Changed local decode: {frames} frames, {media.Duration} ms, {failures} failures.");
      }
      var cacheAfter=File.Exists(cache)?Hash(cache):null;
      if(cacheBefore!=cacheAfter) throw new InvalidOperationException("Measurement changed the native cache.");
      File.WriteAllText(output,JsonSerializer.Serialize(new {
        Schema="vlc-startup-v1",Commit=commit,Runtime=Environment.Version.ToString(),Configuration="Release",Architecture="x64",
        NativeVersion=lib.Version,NativeDirectory=nativeDirectory,NativePayloadSha256=payloadHash,NativeDlls=files.Length,
        CachePresent=cacheBefore!=null,CacheSha256=cacheBefore,CacheBytes=File.Exists(cache)?new FileInfo(cache).Length:0,
        NativeLoadMilliseconds=nativeLoad,ProviderInitializeMilliseconds=construction,TotalInitializationMilliseconds=nativeLoad+construction,
        AudioFilters=audioFilters,VideoFilters=videoFilters,DecodedFrames=frames,DurationMilliseconds=2000,
        DecodeFailures=failures,FreshProcess=true,ColdDiskCache=false,CreatedUtc=DateTime.UtcNow
      },new JsonSerializerOptions{WriteIndented=true}));
    }
    private static string Hash(string path)
    {
      using var stream=File.OpenRead(path);
      using var hash=SHA256.Create();
      return BitConverter.ToString(hash.ComputeHash(stream)).Replace("-","");
    }
    private static void WriteWave(string path)
    {
      using var stream=new FileStream(path,FileMode.CreateNew,FileAccess.Write);
      using var writer=new BinaryWriter(stream);
      writer.Write(Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+192000);
      writer.Write(Encoding.ASCII.GetBytes("WAVEfmt "));writer.Write(16);
      writer.Write((short)1);writer.Write((short)1);writer.Write(48000);writer.Write(96000);
      writer.Write((short)2);writer.Write((short)16);
      writer.Write(Encoding.ASCII.GetBytes("data"));writer.Write(192000);writer.Write(new byte[192000]);
    }
  }
}