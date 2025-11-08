using FFMpegCore;
using LibVLCSharp.Shared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using VCore.Standard.Modularity.Interfaces;
using VFfmpeg;

namespace VPlayer.WindowsPlayer.Views.WindowsPlayer
{
  /// <summary>
  /// Interaction logic for SongPlayerView.xaml
  /// </summary>
  public partial class SongPlayerView : UserControl, IView
  {
    private Process ffmpeg;
    private BinaryWriter ffmpegIn;
    private bool recording;
    private int width;
    private int height;
    private readonly IVFfmpegProvider vFfmpegProvider;

    public SongPlayerView(IVFfmpegProvider vFfmpegProvider)
    {
      InitializeComponent();
      this.vFfmpegProvider = vFfmpegProvider;
      FullScreenBehavior.VideoView = VideoView;
    }


    private double lastFrameTime = 0;
    private double targetFrameInterval;

    private async void StartRecording_Click(object sender, RoutedEventArgs e)
    {
      framesProduced = 0;

      width = (int)ActualWidth;
      height = (int)ActualHeight;

      StartFFmpeg(width, height);

      recording = true;
      await Task.Run(async () => await RecordLoop(30)); // 30 FPS
    }

    private async void StopRecording_Click(object sender, RoutedEventArgs e)
    {
      recording = false;
    }

    private void StartFFmpeg(int w, int h)
    {
      string fileName = DateTime.Now.ToString("dd_MM_yyyy_HH_mm_ss");
      ffmpeg = new Process();
      ffmpeg.StartInfo.FileName = "C:\\ffmpeg\\bin\\ffmpeg.exe";
      ffmpeg.StartInfo.Arguments =
      $"-y -f rawvideo -pixel_format bgra -video_size {width}x{height} -framerate 30 -i pipe:0 " +
      "-vf \"format=rgba,scale=1920:1080:flags=lanczos\" " +
      "-c:v png -pix_fmt rgba -r 30 " +
      $"{fileName}.mov";

      ffmpeg.StartInfo.UseShellExecute = false;
      ffmpeg.StartInfo.RedirectStandardInput = true;
      ffmpeg.StartInfo.RedirectStandardError = true;
      ffmpeg.StartInfo.RedirectStandardOutput = true;
      ffmpeg.StartInfo.CreateNoWindow = true;

      ffmpeg.ErrorDataReceived += (s, e) =>
      {
        if (!string.IsNullOrWhiteSpace(e.Data))
          Console.WriteLine("FFmpeg: " + e.Data);
      };



      ffmpeg.Start();
      ffmpeg.BeginErrorReadLine();

      ffmpegIn = new BinaryWriter(ffmpeg.StandardInput.BaseStream);
    }

    private int framesProduced = 0;
    private async Task RecordLoop(int targetFps)
    {
      double frameInterval = 1000.0 / targetFps;
      var sw = Stopwatch.StartNew();
      framesProduced = 0;

      while (recording)
      {
        // Wait until it's time for next frame
        double targetTime = framesProduced * frameInterval;

        double now = sw.Elapsed.TotalMilliseconds;
        if (now < targetTime)
        {
          // Sleep until next frame time
          await Task.Delay((int)Math.Max(1, targetTime - now));
          continue;
        }

        framesProduced++;

        // Capture on UI thread, async so UI stays smooth
        await Dispatcher.InvokeAsync(() =>
        {
          if (recording)
            WriteFrame();
        }, System.Windows.Threading.DispatcherPriority.Background);
      }

      ffmpegIn.Close();
      ffmpeg.WaitForExit();
    }

    private void WriteFrame()
    {
      var rtb = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);

      var dv = new DrawingVisual();
      using (var dc = dv.RenderOpen())
      {
        // Fill with transparent first!
        dc.DrawRectangle(Brushes.Transparent, null, new Rect(new Point(), new Size(width, height)));

        // Render your control into the transparent surface
        var vb = new VisualBrush(this);
        dc.DrawRectangle(vb, null, new Rect(new Point(), new Size(width, height)));
      }

      rtb.Render(dv);

      var pixels = new byte[width * height * 4];
      rtb.CopyPixels(pixels, width * 4, 0);

      ffmpegIn.Write(pixels);
    }
  }
}
