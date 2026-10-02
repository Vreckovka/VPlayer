using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using VPlayer.TestSupport;

namespace VPlayer.Performance
{
  internal static class LyricsAnimationBenchmarks
  {
    public static void Run(string directory,string output,string commit)
    {
      if(commit.Length!=40 || commit.Any(c=>!Uri.IsHexDigit(c)))throw new ArgumentException("Exact source commit required.");
      if(File.Exists(output))throw new IOException("Use a new output.");
      Directory.CreateDirectory(directory);
      var app=new Application {ShutdownMode=ShutdownMode.OnExplicitShutdown};
      var results=new List<object>();
      foreach(var swaps in new[]{0,1000})
      {
        var first=LyricsAnimationFixture.Create(100000);
        var second=LyricsAnimationFixture.Create(100000);
        first.SetActualLine(TimeSpan.Zero);second.SetActualLine(TimeSpan.Zero);
        using var fixture=new LyricsAnimationFixture(first);
        var host=new Window {Title="VPlayer isolated lyrics animation benchmark",Width=760,Height=550,ShowActivated=false,Content=fixture.View};
        host.Show();LyricsAnimationFixture.Pump(TimeSpan.FromMilliseconds(100));
        var allocated=GC.GetAllocatedBytesForCurrentThread();
        var swapWatch=Stopwatch.StartNew();
        for(int i=0;i<swaps;i++)fixture.View.DataContext=(i%2==0?second:first);
        LyricsAnimationFixture.Pump(TimeSpan.FromMilliseconds(50));swapWatch.Stop();
        var swapAllocation=GC.GetAllocatedBytesForCurrentThread()-allocated;
        var oldObserved=LyricsAnimationFixture.Observed(second);
        fixture.Scroller.ScrollToTop();LyricsAnimationFixture.Pump(TimeSpan.FromMilliseconds(20));
        second.SetActualLine(TimeSpan.FromSeconds(90000));LyricsAnimationFixture.Pump(TimeSpan.FromMilliseconds(50));
        var oldTrackMovedScroll=fixture.Scroller.VerticalOffset>1;
        fixture.Scroller.ScrollToTop();LyricsAnimationFixture.Pump(TimeSpan.FromMilliseconds(30));
        var frameGaps=new List<double>();var lags=new List<double>();
        var watch=Stopwatch.StartNew();double lastFrame=-1;double previousTick=0;
        EventHandler frame=(sender,args)=>{var now=watch.Elapsed.TotalMilliseconds;if(lastFrame>=0)frameGaps.Add(now-lastFrame);lastFrame=now;};
        CompositionTarget.Rendering+=frame;
        int updates=0;
        var timer=new DispatcherTimer(DispatcherPriority.Normal){Interval=TimeSpan.FromMilliseconds(16)};
        timer.Tick+=(sender,args)=>{var now=watch.Elapsed.TotalMilliseconds;if(updates>0)lags.Add(Math.Max(0,now-previousTick-16));previousTick=now;updates++;first.SetActualLine(TimeSpan.FromSeconds(updates/3));};
        var process=Process.GetCurrentProcess();var cpu=process.TotalProcessorTime.TotalMilliseconds;
        allocated=GC.GetAllocatedBytesForCurrentThread();
        timer.Start();LyricsAnimationFixture.Pump(TimeSpan.FromSeconds(6));timer.Stop();CompositionTarget.Rendering-=frame;
        var elapsed=watch.Elapsed.TotalMilliseconds;
        var cpuUsed=process.TotalProcessorTime.TotalMilliseconds-cpu;
        var playAllocation=GC.GetAllocatedBytesForCurrentThread()-allocated;
        LyricsAnimationFixture.Pump(TimeSpan.FromMilliseconds(1200));
        fixture.Layout();
        var screenshot=Path.Combine(directory,"lyrics-"+swaps+".png");
        var bitmap=new RenderTargetBitmap(720,480,96,96,PixelFormats.Pbgra32);bitmap.Render(fixture.View);
        var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using(var stream=File.Create(screenshot))encoder.Save(stream);
        if(updates<10 || frameGaps.Count<2 || first.ActualLine==null)throw new InvalidOperationException("No valid rendered animation samples.");
        results.Add(new {TrackChanges=swaps,LinesPerTrack=100000,TrackChangeMilliseconds=swapWatch.Elapsed.TotalMilliseconds,TrackChangeAllocatedBytes=swapAllocation,OldTrackStillObserved=oldObserved,OldTrackMovedScroll=oldTrackMovedScroll,
          PlaybackUpdates=updates,ElapsedMilliseconds=elapsed,FrameCount=frameGaps.Count,FrameGapsMilliseconds=frameGaps.ToArray(),DispatcherDelayMilliseconds=lags.ToArray(),CpuMilliseconds=cpuUsed,UiThreadAllocatedBytes=playAllocation,
          ActiveLineIndex=first.AllLine.IndexOf(first.ActualLine),FinalVerticalOffset=fixture.Scroller.VerticalOffset,Screenshot=screenshot});
        host.Close();
      }
      app.Shutdown();
      File.WriteAllText(output,JsonSerializer.Serialize(new {Schema="lyrics-animation-v1",Commit=commit,CreatedUtc=DateTime.UtcNow,Runtime=Environment.Version.ToString(),Environment.ProcessorCount,
        Boundary="Production AutoScrollLyricsBehavior and LRCFileViewModel in a visible virtualized pixel-scroll ListView; simplified fixed-height text rows; synthetic 16ms playback ticks, no audio decoding or full player UI. First fresh case then 1000 track changes; render gaps, dispatcher delay and process CPU measured for six seconds. Context construction, snapshots and settling excluded from playback timing.",Results=results},new JsonSerializerOptions {WriteIndented=true}));
    }
  }
}