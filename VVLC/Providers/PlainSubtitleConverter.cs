using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace VVLC.Providers
{
  public static class PlainSubtitleConverter
  {
    // LibVLC's little-endian FourCC for ASS and SSA subtitle streams.
    public const uint AssCodec = 0x20617373;

    public static async Task ConvertAsync(string ffmpeg, string source, int subtitleIndex, string destination,
      CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();
      if (subtitleIndex < 0) throw new ArgumentOutOfRangeException(nameof(subtitleIndex));
      if (!File.Exists(source)) throw new FileNotFoundException("Subtitle source was not found.", source);
      var startInfo = new ProcessStartInfo
      {
        FileName = string.IsNullOrWhiteSpace(ffmpeg) || !File.Exists(ffmpeg) ? "ffmpeg" : Path.GetFullPath(ffmpeg),
        UseShellExecute = false,
        CreateNoWindow = true,
        RedirectStandardError = true
      };
      foreach (var argument in new[] { "-hide_banner", "-loglevel", "error", "-nostdin", "-n", "-i", source,
        "-map", "0:s:" + subtitleIndex.ToString(CultureInfo.InvariantCulture), "-c:s", "text", "-f", "srt", destination })
        startInfo.ArgumentList.Add(argument);

      using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
      var exited = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
      process.Exited += (_, __) => exited.TrySetResult(true);
      using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
      timeout.CancelAfter(TimeSpan.FromSeconds(45));
      try
      {
        if (!process.Start()) throw new IOException("FFmpeg could not be started.");
        var error = process.StandardError.ReadToEndAsync();
        using (timeout.Token.Register(() => exited.TrySetCanceled()))
          await exited.Task.ConfigureAwait(false);
        timeout.Token.ThrowIfCancellationRequested();
        var diagnostics = await error.ConfigureAwait(false);
        if (process.ExitCode != 0 || !File.Exists(destination) || new FileInfo(destination).Length == 0)
          throw new IOException("FFmpeg could not produce a plain subtitle track. " + diagnostics.Trim());
      }
      catch (OperationCanceledException)
      {
        try { if (!process.HasExited) process.Kill(true); }
        catch (InvalidOperationException) { }
        catch (Win32Exception) { }
        if (!cancellationToken.IsCancellationRequested)
          throw new TimeoutException("Converting the subtitle track took longer than 45 seconds.");
        throw;
      }
    }
  }
}
