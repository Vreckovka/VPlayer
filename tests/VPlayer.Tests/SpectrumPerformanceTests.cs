using System;
using System.IO;
using System.Text.Json;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WinformsVisualization.Visualization;
using Xunit;
using Point = WinformsVisualization.Visualization.SpectrumBase.SpectrumPointData;

namespace VPlayer.Tests
{
  public partial class PerformanceTests
  {
    // Reference implementation of the original per-frame bitmap loop.
    private static unsafe void RenderOriginal(WriteableBitmap bitmap,Point[] points,int[] previous,int[] gradient,int barWidth)
    {
      int width=bitmap.PixelWidth,height=bitmap.PixelHeight;
      using(var context=bitmap.GetBitmapContext())
      {
        int* pixels=context.Pixels;
        for(int i=0;i<points.Length;i++)
        {
          int start=barWidth*points[i].SpectrumPointIndex,end=Math.Min(width,start+barWidth);
          int next=Math.Clamp((int)(points[i].Value*2-1),0,height);
          int old=previous[i];
          for(int y=height-old;y<height-next;y++)
            for(int x=start;x<end;x++) pixels[y*width+x]=0;
          for(int y=height-next;y<height;y++)
            for(int x=start;x<end;x++) pixels[y*width+x]=gradient[y];
          previous[i]=next;
        }
      }
    }
    [Fact]
    [Trait("Category","Performance")]
    public void CompareStableSpectrumFrameWithOriginalPixelLoop()
    {
      Sta.Run(() =>
      {
        const int width=576,height=240,barWidth=18;
        var points=new Point[32];
        for(int i=0;i<points.Length;i++) points[i]=new Point {SpectrumPointIndex=i,Value=(30+180*Math.Abs(Math.Sin(i*0.4))+1)/2};
        var previous=new int[points.Length];
        var gradient=new int[height];
        for(int y=0;y<height;y++)
        {
          double t=(double)y/(height-1);
          byte red=(byte)(255*t),blue=(byte)(255*(1-t));
          gradient[y]=unchecked((int)0xff000000)|(red<<16)|blue;
        }
        var oldBitmap=new WriteableBitmap(width,height,96,96,PixelFormats.Pbgra32,null);
        var newBitmap=new WriteableBitmap(width,height,96,96,PixelFormats.Pbgra32,null);
        var renderer=new LineSpectrum {BarWidth=barWidth};
        var original=Measure(()=>RenderOriginal(oldBitmap,points,previous,gradient,barWidth),1000);
        var incremental=Measure(()=>renderer.UpdateSpectrumBitmap(newBitmap,points,System.Drawing.Color.Red,System.Drawing.Color.Blue),1000);
        var oldPixels=new int[width*height];
        var newPixels=new int[width*height];
        oldBitmap.CopyPixels(oldPixels,width*4,0);
        newBitmap.CopyPixels(newPixels,width*4,0);
        Assert.Equal(oldPixels,newPixels);
        output.WriteLine($"1,000 unchanged 576x240 spectrum frames: original {original.Milliseconds:F2} ms; incremental {incremental.Milliseconds:F2} ms.");
        var artifacts=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"..","..","..","..","..","..","artifacts","performance"));
        Directory.CreateDirectory(artifacts);
        File.WriteAllText(Path.Combine(artifacts,"spectrum.json"),JsonSerializer.Serialize(new {
          Runtime=Environment.Version.ToString(),Frames=1000,Width=width,Height=height,
          Scenario="Unchanged bar heights",OriginalMilliseconds=original.Milliseconds,IncrementalMilliseconds=incremental.Milliseconds,
          OriginalAllocatedBytes=original.Allocated,IncrementalAllocatedBytes=incremental.Allocated
        },new JsonSerializerOptions {WriteIndented=true}));
      });
    }
  }
}
