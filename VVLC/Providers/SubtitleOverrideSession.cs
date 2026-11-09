using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LibVLCSharp.Shared;

namespace VVLC.Providers
{
  // A generated track is local to one playback input. Only the original ID is
  // exposed to the UI or saved to the library.
  public sealed class SubtitleOverrideSession : IDisposable
  {
    private readonly string directory = Path.Combine(Path.GetTempPath(), "VPlayer-subtitles-" + Guid.NewGuid().ToString("N"));
    private readonly Dictionary<int, int> generated = new Dictionary<int, int>();
    private readonly SemaphoreSlim addTrack = new SemaphoreSlim(1, 1);
    private CancellationTokenSource selection;
    private object owner;

    public void UseMedia(object media)
    {
      if (ReferenceEquals(owner, media)) return;
      Cancel();
      owner = media;
      generated.Clear();
    }

    public void Cancel() => selection?.Cancel();

    public bool IsGenerated(int id) => generated.ContainsValue(id);

    public int OriginalTrack(int id)
    {
      foreach (var pair in generated)
        if (pair.Value == id) return pair.Key;
      return id;
    }

    public async Task<bool> SelectAsync(MediaPlayer player, object mediaOwner, int id, string ffmpeg,
      Func<bool> isCurrentMedia, Action<Exception> onFallback)
    {
      UseMedia(mediaOwner);
      Cancel();
      using var request = new CancellationTokenSource();
      selection = request;
      var token = request.Token;
      bool IsCurrent() => !token.IsCancellationRequested && ReferenceEquals(owner, mediaOwner) && isCurrentMedia();
      try
      {
        if (!IsCurrent()) return false;
        if (generated.TryGetValue(id, out int ready))
        {
          if (player.Spu != ready) player.SetSpu(ready);
          return true;
        }

        using var media = player.Media;
        var tracks = media?.Tracks?.Where(track => track.TrackType == TrackType.Text).ToArray();
        int index = id < 0 || tracks == null ? -1 : Array.FindIndex(tracks, track => track.Id == id);
        if (index < 0 || tracks[index].Codec != PlainSubtitleConverter.AssCodec ||
            !Uri.TryCreate(media.Mrl, UriKind.Absolute, out var source) || !source.IsFile)
        {
          if (player.Spu != id) player.SetSpu(id);
          return true;
        }

        player.SetSpu(-1);
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, Guid.NewGuid().ToString("N") + ".srt");
        await PlainSubtitleConverter.ConvertAsync(ffmpeg, source.LocalPath, index, path, token);
        if (!IsCurrent()) return false;
        await addTrack.WaitAsync(token);
        try
        {
          if (!IsCurrent()) return false;
          if (!generated.TryGetValue(id, out ready))
          {
            ready = await LoadTrackAsync(player, path);
            if (ReferenceEquals(owner, mediaOwner) && isCurrentMedia()) generated[id] = ready;
          }
          if (!IsCurrent()) return false;
          if (!player.SetSpu(ready)) throw new IOException("VLC did not select the converted subtitle track.");
          return true;
        }
        finally
        {
          addTrack.Release();
        }
      }
      catch (OperationCanceledException) when (token.IsCancellationRequested)
      {
        return false;
      }
      catch (Exception error)
      {
        if (!IsCurrent()) return false;
        player.SetSpu(id);
        onFallback?.Invoke(error);
        return true;
      }
      finally
      {
        if (ReferenceEquals(selection, request)) selection = null;
      }
    }

    private static async Task<int> LoadTrackAsync(MediaPlayer player, string path)
    {
      var knownIds = new HashSet<int>(player.SpuDescription.Select(track => track.Id));
      var added = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
      void OnAdded(object sender, MediaPlayerESAddedEventArgs args)
      {
        if (args.Type == TrackType.Text && !knownIds.Contains(args.Id)) added.TrySetResult(args.Id);
      }
      player.ESAdded += OnAdded;
      try
      {
        // Don't auto-select the slave: a newer request may arrive while VLC loads it.
        if (!player.AddSlave(MediaSlaveType.Subtitle, new Uri(path).AbsoluteUri, false))
          throw new IOException("VLC could not load the converted subtitle track.");
        if (await Task.WhenAny(added.Task, Task.Delay(TimeSpan.FromSeconds(5))) != added.Task)
          throw new TimeoutException("VLC did not announce the converted subtitle track.");
        return await added.Task;
      }
      finally
      {
        player.ESAdded -= OnAdded;
      }
    }

    public void Dispose()
    {
      UseMedia(null);
      Cancel();
      try
      {
        if (Directory.Exists(directory)) Directory.Delete(directory, true);
      }
      catch (IOException) { }
      catch (UnauthorizedAccessException) { }
    }
  }
}
