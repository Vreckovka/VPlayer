using System;
using System.IO;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VPlayer.Player.UserControls;
using WinformsVisualization.Visualization;
using Xunit;
using Point = WinformsVisualization.Visualization.SpectrumBase.SpectrumPointData;

namespace VPlayer.Tests
{
  internal static class Sta
  {
    public static void Run(Action action)
    {
      Exception failure = null;
      var thread = new Thread(() => {try {action();} catch(Exception error) {failure=error;}});
      thread.IsBackground = true;
      thread.SetApartmentState(ApartmentState.STA);
      thread.Start();
      if(!thread.Join(TimeSpan.FromSeconds(10))) throw new TimeoutException("WPF test did not finish.");
      if(failure != null) ExceptionDispatchInfo.Capture(failure).Throw();
    }
  }

  public class SpectrumTests
  {
    private static Point[] Points(params double[] heights)
    {
      var points = new Point[heights.Length];
      for(int i=0;i<heights.Length;i++)
        points[i] = new Point {SpectrumPointIndex=i,Value=(heights[i]+1)/2};
      return points;
    }
    private static WriteableBitmap Bitmap(int width=16,int height=16)
      => new WriteableBitmap(width,height,96,96,PixelFormats.Pbgra32,null);
    private static int[] Pixels(WriteableBitmap bitmap)
    {
      var pixels = new int[bitmap.PixelWidth*bitmap.PixelHeight];
      bitmap.CopyPixels(pixels,bitmap.PixelWidth*4,0);
      return pixels;
    }
    private static void Render(LineSpectrum renderer, WriteableBitmap bitmap, Point[] points)
      => renderer.UpdateSpectrumBitmap(bitmap,points,System.Drawing.Color.Red,System.Drawing.Color.Blue);

    [Fact]
    public void GrowingShrinkingAndRepeatedBarsMatchFreshFrame()
    {
      Sta.Run(() =>
      {
        var renderer = new LineSpectrum {BarWidth=4};
        var bitmap = Bitmap();
        foreach(var heights in new[] {new double[] {3,10,5},new double[] {3,10,5},new double[] {12,2,0},new double[] {1,15,8}})
        {
          var points = Points(heights);
          Render(renderer,bitmap,points);
          var fresh = Bitmap();
          Render(new LineSpectrum {BarWidth=4},fresh,points);
          Assert.Equal(Pixels(fresh),Pixels(bitmap));
        }
      });
    }
    [Fact]
    public void ResizingBitmapDiscardsOldHeightCache()
    {
      Sta.Run(() =>
      {
        var renderer = new LineSpectrum {BarWidth=4};
        Render(renderer,Bitmap(),Points(16,12));
        var smaller = Bitmap(8,4);
        Render(renderer,smaller,Points(1,2));
        var fresh = Bitmap(8,4);
        Render(new LineSpectrum {BarWidth=4},fresh,Points(1,2));
        Assert.Equal(Pixels(fresh),Pixels(smaller));
      });
    }
    [Fact]
    public void ChangedBarLayoutClearsOldPixels()
    {
      Sta.Run(() =>
      {
        var renderer = new LineSpectrum {BarWidth=4};
        var bitmap = Bitmap();
        Render(renderer,bitmap,Points(12,12,12));
        renderer.BarWidth=2;
        Render(renderer,bitmap,Points(3));
        var fresh = Bitmap();
        Render(new LineSpectrum {BarWidth=2},fresh,Points(3));
        Assert.Equal(Pixels(fresh),Pixels(bitmap));
      });
    }
    [Fact]
    public void GradientChangesRepaintExistingHeights()
    {
      Sta.Run(() =>
      {
        var renderer = new LineSpectrum {BarWidth=4};
        var bitmap = Bitmap();
        Render(renderer,bitmap,Points(12));
        renderer.UpdateSpectrumBitmap(bitmap,Points(12),System.Drawing.Color.Green,System.Drawing.Color.White);
        var fresh = Bitmap();
        new LineSpectrum {BarWidth=4}.UpdateSpectrumBitmap(fresh,Points(12),System.Drawing.Color.Green,System.Drawing.Color.White);
        Assert.Equal(Pixels(fresh),Pixels(bitmap));
      });
    }
    [Fact]
    public void ZeroBinsInQuietSpectrumRemainFiniteAndDoNotHang()
    {
      var renderer = new LineSpectrum();
      var normalize = typeof(LineSpectrum).GetMethod("NormalizeData",BindingFlags.Instance|BindingFlags.NonPublic);
      var input = new[] {new Point {Value=0},new Point {Value=0.00000001},new Point {Value=0.00000002}};
      Point[] output = null;
      Sta.Run(()=>output=(Point[])normalize.Invoke(renderer,new object[] {input,0.0,30.0}));
      Assert.Same(input,output);
      Assert.Equal(0,output[0].Value);
      Assert.Equal(renderer.NormlizedDataMaxSilentValue,output[2].Value);
      foreach(var point in output) Assert.False(double.IsNaN(point.Value)||double.IsInfinity(point.Value));
    }
    [Fact]
    public void VisualizerCanBeConfiguredBeforeLoadingWithoutAudioInitialization()
    {
      Sta.Run(() =>
      {
        var control = new SoundVizualizer();
        control.UseAutomaticBarCountCalculation=true;
        control.MinimumBarWidth=4;
        control.MinimumBarWidth=null;
        control.MaxFrequency=16000;
        Assert.Equal("MaxFrequency",SoundVizualizer.MaxFrequencyProperty.Name);
      });
    }
    [Fact]
    public void RendersVisualSnapshot()
    {
      Sta.Run(() =>
      {
        var bitmap=Bitmap(640,240);
        var points = new Point[32];
        for(int i=0;i<points.Length;i++)
          points[i]=new Point {SpectrumPointIndex=i,Value=(30+180*Math.Abs(Math.Sin(i*0.4))+1)/2};
        Render(new LineSpectrum {BarWidth=18},bitmap,points);
        var pixels=Pixels(bitmap);
        Assert.Contains(pixels,x=>x!=0);
        var directory=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"..","..","..","..","..","..","artifacts","visual-tests"));
        Directory.CreateDirectory(directory);
        var encoder=new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var file=File.Create(Path.Combine(directory,"spectrum.png"));
        encoder.Save(file);
      });
    }
  }
}
