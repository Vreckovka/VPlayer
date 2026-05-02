using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using MathNet.Numerics.IntegralTransforms;
using System.Numerics;
using System.Windows;
using VCore.Standard;
using System.Threading;
using System.Threading.Tasks;

public sealed class VideoFile
{
  public List<Fingerprint> Fingerprints;

  public VideoFile(string path, string name)
  {
    Path = path;
    Name = name;
  }

  public string Path { get; set; }
  public string Name { get; set; }
  public double DurationSeconds { get; set; }
}

public sealed class Fingerprint
{
  public Fingerprint(int videoIndex, double timeSeconds, ulong hash)
  {
    VideoIndex = videoIndex;
    TimeSeconds = timeSeconds;
    Hash = hash;
  }

  public int VideoIndex { get; set; }
  public double TimeSeconds { get; set; }
  public ulong Hash { get; set; }
}

public sealed class DetectedSegment : ViewModel
{
  public double StartSeconds;
  public double EndSeconds;
  public string Type { get; set; }
  public double ConfidencePercent;
  public VideoFile VideoFile;

  public float StartPosition
  {
    get
    {
      if (VideoFile == null || VideoFile.DurationSeconds <= 0)
        return 0;

      return (float)(StartSeconds / VideoFile.DurationSeconds);
    }
  }

  public float EndPosition
  {
    get
    {
      if (VideoFile == null || VideoFile.DurationSeconds <= 0)
        return 0;

      return (float)(EndSeconds / VideoFile.DurationSeconds);
    }
  }

  public double DurationPosition
  {
    get { return Math.Max(0, EndPosition - StartPosition); }
  }

  public double RemainingPosition
  {
    get { return Math.Max(0, 1.0 - EndPosition); }
  }

  public GridLength StartGridWidth
  {
    get { return new GridLength(StartPosition, GridUnitType.Star); }
  }

  public GridLength DurationGridWidth
  {
    get { return new GridLength(DurationPosition, GridUnitType.Star); }
  }

  public GridLength RemainingGridWidth
  {
    get { return new GridLength(RemainingPosition, GridUnitType.Star); }
  }

  public void RaisePropertyChanges()
  {
    RaisePropertyChanged(nameof(StartGridWidth));
    RaisePropertyChanged(nameof(DurationGridWidth));
    RaisePropertyChanged(nameof(RemainingGridWidth));
  }

  public string Description
  {
    get
    {
      var duration = EndSeconds - StartSeconds;
      return $"{TimeSpan.FromSeconds(StartSeconds)} - {TimeSpan.FromSeconds(EndSeconds)} ({TimeSpan.FromSeconds(duration)}) | {Type} | {ConfidencePercent:F1}%";
    }
  }

  public DetectedSegment(
      VideoFile videoFile,
      double startSeconds,
      double endSeconds,
      string type,
      double confidencePercent = 0)
  {
    VideoFile = videoFile;
    StartSeconds = startSeconds;
    EndSeconds = endSeconds;
    Type = type;
    ConfidencePercent = confidencePercent;
  }
}

public static class SectionsIndentifier
{
  private const int SampleRate = 4000;
  private const int totalBits = 15;

  private static readonly int[] bandEdges =
  {
        60, 80, 100, 125, 160, 200, 250, 315,
        400, 500, 630, 800, 1000, 1250, 1600, 1900
    };

  //private const double MinRmsToFingerprint = 0.015; 
  //private const double MinPeakToFingerprint = 0.04;

  private const double MinRmsToFingerprint = 0.0035;
  private const double MinPeakToFingerprint = 0.001;

  private const double WindowSeconds = 2.0;
  private const double HopSeconds = 2.0;

  private const double MinSegmentSeconds = 25;
  private const double MaxSegmentSeconds = 140.0;
  private const double MergeGapSeconds = 6.0;

  private const double MinMatchedSeconds = 25;
  private const double MinHashSimilarity = 0.88;

  private const double IntroSearchMaxRatio = 0.45;
  private const double OutroSearchMinRatio = 0.55;

  private const double RecoveryTimeToleranceSeconds = 35.0;

  private const int MinVideosPerIntroCluster = 2;
  private const int MinVideosPerOutroCluster = 2;

  private const double SegmentClusterSimilarity = 0.82;

  private const int MaxIntroClustersToKeep = 3;
  private const int MaxOutroClustersToKeep = 3;

  public static List<DetectedSegment> Detect(
      List<string> videoPathsToParse,
      List<VideoFile> existingVideos = null,
      CancellationToken? token = null)
  {
    var videos = new List<VideoFile>();
    var allFingerprints = new List<List<Fingerprint>>();
    var durations = new List<double>();

    if (existingVideos != null)
    {
      foreach (var video in existingVideos)
      {
        if (video == null)
          continue;

        if (video.Fingerprints == null || video.Fingerprints.Count == 0)
          continue;

        if (video.DurationSeconds <= 0)
          continue;

        videos.Add(video);
        allFingerprints.Add(video.Fingerprints);
        durations.Add(video.DurationSeconds);
      }
    }



    var tasks = new List<Task<(VideoFile Video, List<Fingerprint> Fingerprints, double Duration)>>();

    int baseVideoIndex = videos.Count;
    int parseIndex = 0;

    foreach (string path in videoPathsToParse)
    {
      if (string.IsNullOrWhiteSpace(path))
        continue;

      if (videos.Any(v => string.Equals(v.Path, path, StringComparison.OrdinalIgnoreCase)))
        continue;

      if (token != null && token.Value.IsCancellationRequested)
      {
        Console.WriteLine("Sections detections cancelled");
        return new List<DetectedSegment>();
      }

      int videoIndex = baseVideoIndex + parseIndex;
      parseIndex++;

      string localPath = path;

      tasks.Add(Task.Run(() =>
      {
        var video = new VideoFile(localPath, Path.GetFileName(localPath));

        Console.WriteLine($"Extracting audio {localPath}");
        float[] samples = ExtractAudioToMemory(localPath, out double duration);

        Console.WriteLine($"Creating fingerprints {localPath}");
        var fingerprints = CreateFingerprints(samples, videoIndex);

        video.DurationSeconds = duration;
        video.Fingerprints = fingerprints;

        return (video, fingerprints, duration);
      }));
    }

    Task.WaitAll(tasks.ToArray());

    foreach (var task in tasks)
    {
      var result = task.Result;

      videos.Add(result.Video);
      allFingerprints.Add(result.Fingerprints);
      durations.Add(result.Duration);
    }

    var rawSegments = FindRepeatedSegments(allFingerprints, videos);

    var merged = rawSegments
        .GroupBy(s => s.VideoFile.Path)
        .SelectMany(g => MergeSegments(g.ToList()))
        .Where(s =>
            s.EndSeconds - s.StartSeconds >= MinSegmentSeconds &&
            s.EndSeconds - s.StartSeconds <= MaxSegmentSeconds)
        .Select(s =>
        {
          int videoIndex = videos.FindIndex(v => v.Path == s.VideoFile.Path);
          return ClassifyIntroOutro(s, durations[videoIndex]);
        })
        .Where(IsIntroOrOutro)
        .ToList();

    merged = ValidateIntroOutroClusters(merged);

    merged = RecoverMissingIntroOutros(merged, videos);

    merged = ValidateIntroOutroClusters(merged);

    return merged
        .OrderBy(s => s.VideoFile.Path)
        .ThenBy(s => s.StartSeconds)
        .ToList();
  }

  private static float[] ExtractAudioToMemory(string videoPath, out double durationSeconds)
  {
    double ScanRatio = 0.25;
    double MaxScanSeconds = 8 * 60;

    durationSeconds = GetVideoDurationSeconds(videoPath);

    if (durationSeconds <= 0)
      throw new Exception("Could not read video duration: " + videoPath);

    // bounded segment duration
    double segmentDuration = Math.Min(durationSeconds * ScanRatio, MaxScanSeconds);

    // intro
    double introStart = 0;

    // outro
    double outroStart = Math.Max(0, durationSeconds - segmentDuration);

    var samples = new List<float>(SampleRate * 60 * 25);

    // first segment
    samples.AddRange(ExtractAudioRangeToMemory(videoPath, introStart, segmentDuration));

    // silent middle (preserve timeline)
    double middleDuration = outroStart - segmentDuration;
    int middleSamples = (int)(middleDuration * SampleRate);

    if (middleSamples > 0)
      samples.AddRange(new float[middleSamples]);

    // last segment
    samples.AddRange(ExtractAudioRangeToMemory(videoPath, outroStart, segmentDuration));

    return samples.ToArray();
  }

  private static float[] ExtractAudioRangeToMemory(
    string videoPath,
    double startSeconds,
    double durationSeconds)
  {
    var psi = new ProcessStartInfo
    {
      FileName = "ffmpeg",
      Arguments =
            $"-nostdin -hide_banner -loglevel error " +
            $"-threads 1 " +
            $"-ss {startSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture)} " +
            $"-t {durationSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture)} " +
            $"-i \"{videoPath}\" " +
            $"-map 0:a:0 " +
            $"-vn -sn -dn " +
            $"-ac 1 -ar {SampleRate} " +
            $"-f s16le pipe:1",
      RedirectStandardOutput = true,
      RedirectStandardError = true,
      UseShellExecute = false,
      CreateNoWindow = true
    };

    using var process = new Process();
    process.StartInfo = psi;

    var errorLines = new List<string>();

    process.ErrorDataReceived += (s, e) =>
    {
      if (!string.IsNullOrWhiteSpace(e.Data))
        errorLines.Add(e.Data);
    };

    process.Start();
    process.BeginErrorReadLine();

    var samples = new List<float>();
    byte[] buffer = new byte[SampleRate * 2];

    using var output = process.StandardOutput.BaseStream;

    int bytesRead;
    while ((bytesRead = output.Read(buffer, 0, buffer.Length)) > 0)
    {
      int sampleCount = bytesRead / 2;

      for (int i = 0; i < sampleCount; i++)
      {
        short value = BitConverter.ToInt16(buffer, i * 2);
        samples.Add(value / 32768f);
      }
    }

    process.WaitForExit();

    if (process.ExitCode != 0)
    {
      string error = string.Join(Environment.NewLine, errorLines);
      throw new Exception("ffmpeg failed with exit code " + process.ExitCode + Environment.NewLine + error);
    }

    return samples.ToArray();
  }

  private static double GetVideoDurationSeconds(string videoPath)
  {
    var psi = new ProcessStartInfo
    {
      FileName = "ffprobe",
      Arguments =
            $"-v error -show_entries format=duration " +
            $"-of default=noprint_wrappers=1:nokey=1 \"{videoPath}\"",
      RedirectStandardOutput = true,
      RedirectStandardError = true,
      UseShellExecute = false,
      CreateNoWindow = true
    };

    using var process = Process.Start(psi);

    string output = process.StandardOutput.ReadToEnd();
    process.WaitForExit();

    if (double.TryParse(
        output.Trim(),
        System.Globalization.NumberStyles.Float,
        System.Globalization.CultureInfo.InvariantCulture,
        out double duration))
    {
      return duration;
    }

    return 0;
  }

  private static List<Fingerprint> CreateFingerprints(float[] samples, int videoIndex)
  {
    int windowSize = (int)(SampleRate * WindowSeconds);
    int hopSize = (int)(SampleRate * HopSeconds);

    var result = new List<Fingerprint>();

    for (int offset = 0; offset + windowSize < samples.Length; offset += hopSize)
    {
      if (IsQuietWindow(samples, offset, windowSize))
        continue;

      ulong hash = FingerprintWindow(samples, offset, windowSize);

      if (hash == 0)
        continue;

      double time = offset / (double)SampleRate;

      result.Add(new Fingerprint(videoIndex, time, hash));
    }

    return result;
  }

  private static bool IsQuietWindow(float[] samples, int offset, int size)
  {
    double sumSquares = 0;
    double peak = 0;

    for (int i = 0; i < size; i++)
    {
      double value = Math.Abs(samples[offset + i]);

      sumSquares += value * value;

      if (value > peak)
        peak = value;
    }

    double rms = Math.Sqrt(sumSquares / size);

    return rms < MinRmsToFingerprint || peak < MinPeakToFingerprint;
  }

  private static ulong FingerprintWindow(float[] samples, int offset, int size)
  {
    int fftSize = 1;

    while (fftSize < size)
      fftSize <<= 1;

    var fft = new Complex[fftSize];

    for (int i = 0; i < size; i++)
    {
      double hann = 0.5 - 0.5 * Math.Cos(2.0 * Math.PI * i / (size - 1));
      fft[i] = new Complex(samples[offset + i] * hann, 0);
    }

    Fourier.Forward(fft, FourierOptions.Matlab);

    int bandCount = bandEdges.Length - 1;
    var energies = new double[bandCount];

    for (int b = 0; b < bandCount; b++)
    {
      int startBin = FrequencyToBin(bandEdges[b], fftSize);
      int endBin = FrequencyToBin(bandEdges[b + 1], fftSize);

      double sum = 0;

      for (int k = startBin; k <= endBin && k < fft.Length / 2; k++)
        sum += fft[k].Magnitude;

      energies[b] = Math.Log10(sum + 1e-9);
    }

    double avg = energies.Average();

    ulong hash = 0;

    for (int i = 0; i < energies.Length; i++)
    {
      if (energies[i] > avg)
        hash |= 1UL << i;
    }

    return hash;
  }

  private static int FrequencyToBin(int frequency, int fftSize)
  {
    return (int)Math.Round(frequency * fftSize / (double)SampleRate);
  }

  private static List<DetectedSegment> FindRepeatedSegments(
      List<List<Fingerprint>> all,
      List<VideoFile> videos)
  {
    var detected = new List<DetectedSegment>();

    for (int a = 0; a < all.Count; a++)
    {
      for (int b = a + 1; b < all.Count; b++)
      {
        var fa = all[a];
        var fb = all[b];

        var offsetVotes = new Dictionary<int, List<MatchPoint>>();

        foreach (var fpA in fa)
        {
          foreach (var fpB in fb)
          {
            double similarity = GetHashSimilarity(fpA.Hash, fpB.Hash);

            if (similarity < MinHashSimilarity)
              continue;

            int offsetBucket = (int)Math.Round(
                (fpB.TimeSeconds - fpA.TimeSeconds) / HopSeconds);

            if (!offsetVotes.TryGetValue(offsetBucket, out var list))
            {
              list = new List<MatchPoint>();
              offsetVotes[offsetBucket] = list;
            }

            list.Add(new MatchPoint(fpA, fpB, similarity));
          }
        }

        foreach (var group in offsetVotes.Values)
        {
          double matchedSeconds =
              group.Select(x => x.A.TimeSeconds).Distinct().Count() * HopSeconds;

          if (matchedSeconds < MinMatchedSeconds)
            continue;

          double confidence = group.Average(x => x.Similarity) * 100.0;

          var timesA = group
              .Select(x => x.A.TimeSeconds)
              .Distinct()
              .OrderBy(x => x)
              .ToList();

          var timesB = group
              .Select(x => x.B.TimeSeconds)
              .Distinct()
              .OrderBy(x => x)
              .ToList();

          foreach (var segment in BuildSegments(timesA))
          {
            detected.Add(new DetectedSegment(
                videos[a],
                segment.Start,
                segment.End + WindowSeconds,
                "Repeated",
                confidence));
          }

          foreach (var segment in BuildSegments(timesB))
          {
            detected.Add(new DetectedSegment(
                videos[b],
                segment.Start,
                segment.End + WindowSeconds,
                "Repeated",
                confidence));
          }
        }
      }
    }

    return detected;
  }

  private static List<TimeSegment> BuildSegments(List<double> times)
  {
    var result = new List<TimeSegment>();

    if (times.Count == 0)
      return result;

    var distinct = times
        .Distinct()
        .OrderBy(x => x)
        .ToList();

    double start = distinct[0];
    double last = distinct[0];

    for (int i = 1; i < distinct.Count; i++)
    {
      double gap = distinct[i] - last;

      if (gap <= MergeGapSeconds)
      {
        last = distinct[i];
      }
      else
      {
        double length = last + WindowSeconds - start;

        if (length >= MinSegmentSeconds)
          result.Add(new TimeSegment(start, last));

        start = distinct[i];
        last = distinct[i];
      }
    }

    double finalLength = last + WindowSeconds - start;

    if (finalLength >= MinSegmentSeconds)
      result.Add(new TimeSegment(start, last));

    return result;
  }

  private static List<DetectedSegment> MergeSegments(List<DetectedSegment> segments)
  {
    var sorted = segments
        .OrderBy(s => s.StartSeconds)
        .ToList();

    var result = new List<DetectedSegment>();

    foreach (var s in sorted)
    {
      if (result.Count == 0)
      {
        result.Add(s);
        continue;
      }

      var last = result[result.Count - 1];

      if (s.StartSeconds <= last.EndSeconds + MergeGapSeconds)
      {
        last.EndSeconds = Math.Max(last.EndSeconds, s.EndSeconds);
        last.ConfidencePercent = Math.Max(last.ConfidencePercent, s.ConfidencePercent);
      }
      else
      {
        result.Add(s);
      }
    }

    return result;
  }

  private static DetectedSegment ClassifyIntroOutro(
      DetectedSegment segment,
      double videoDuration)
  {
    double midpoint = (segment.StartSeconds + segment.EndSeconds) / 2.0;
    double ratio = midpoint / videoDuration;

    if (ratio <= IntroSearchMaxRatio)
      segment.Type = "Intro / Opening";
    else if (ratio >= OutroSearchMinRatio)
      segment.Type = "Outro / Ending";
    else
      segment.Type = "Repeated Song Section";

    return segment;
  }

  private static bool IsIntroOrOutro(DetectedSegment segment)
  {
    return segment.Type == "Intro / Opening" ||
           segment.Type == "Outro / Ending";
  }

  private static List<DetectedSegment> ValidateIntroOutroClusters(List<DetectedSegment> segments)
  {
    var intros = segments
        .Where(s => s.Type == "Intro / Opening")
        .ToList();

    var outros = segments
        .Where(s => s.Type == "Outro / Ending")
        .ToList();

    var validIntros = KeepValidClustersAndNormalizeDuration(
        intros,
        MinVideosPerIntroCluster,
        MaxIntroClustersToKeep);

    var validOutros = KeepValidClustersAndNormalizeDuration(
        outros,
        MinVideosPerOutroCluster,
        MaxOutroClustersToKeep);

    return validIntros
        .Concat(validOutros)
        .ToList();
  }

  private static List<DetectedSegment> KeepValidClustersAndNormalizeDuration(
    List<DetectedSegment> candidates,
    int minVideosPerCluster,
    int maxClustersToKeep)
  {
    var clusters = BuildSegmentClusters(candidates);

    var validClusters = clusters
        .Where(c =>
            c.Select(s => s.VideoFile.Path)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count() >= minVideosPerCluster)
        .OrderByDescending(c =>
            c.Select(s => s.VideoFile.Path)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count())
        .ThenByDescending(c => c.Average(s => s.ConfidencePercent))
        .Take(maxClustersToKeep)
        .ToList();

    var result = new List<DetectedSegment>();

    foreach (var cluster in validClusters)
    {
      NormalizeClusterDurations(cluster);

      var bestPerVideo = cluster
          .GroupBy(s => s.VideoFile.Path, StringComparer.OrdinalIgnoreCase)
          .Select(g => g
              .OrderByDescending(x => x.ConfidencePercent)
              .ThenByDescending(x => x.EndSeconds - x.StartSeconds)
              .First());

      result.AddRange(bestPerVideo);
    }

    return result;
  }

  private static void NormalizeClusterDurations(List<DetectedSegment> cluster)
  {
    if (cluster == null || cluster.Count == 0)
      return;

    var validSegments = cluster
        .Where(s =>
            s.EndSeconds > s.StartSeconds &&
            s.EndSeconds - s.StartSeconds >= MinSegmentSeconds &&
            s.EndSeconds - s.StartSeconds <= MaxSegmentSeconds)
        .ToList();

    if (validSegments.Count == 0)
      return;

    double targetDuration = validSegments
        .Select(s => s.EndSeconds - s.StartSeconds)
        .OrderByDescending(d => d)
        .First();

    const double toleranceSeconds = 2.0;

    foreach (var segment in cluster)
    {
      double currentDuration = segment.EndSeconds - segment.StartSeconds;

      if (currentDuration >= targetDuration - toleranceSeconds)
        continue;

      double missing = targetDuration - currentDuration;

      double currentStartSupport = GetBoundarySupport(
          segment,
          segment.StartSeconds,
          searchBackward: true);

      double currentEndSupport = GetBoundarySupport(
          segment,
          segment.EndSeconds,
          searchBackward: false);

      bool startLooksCut = currentStartSupport > currentEndSupport;
      bool endLooksCut = currentEndSupport > currentStartSupport;

      if (startLooksCut && segment.Type != "Outro / Ending")
      {
        segment.StartSeconds = Math.Max(0, segment.StartSeconds - missing);
      }
      else if (endLooksCut)
      {
        segment.EndSeconds = Math.Min(segment.VideoFile.DurationSeconds, segment.EndSeconds + missing);
      }
      else
      {
        if (segment.Type == "Intro / Opening")
        {
          segment.StartSeconds = Math.Max(0, segment.StartSeconds - missing);
        }
        else if (segment.Type == "Outro / Ending")
        {
          segment.EndSeconds = Math.Min(segment.VideoFile.DurationSeconds, segment.EndSeconds + missing);
        }
      }

      segment.RaisePropertyChanges();
    }
  }

  private static double GetBoundarySupport(
      DetectedSegment segment,
      double boundarySeconds,
      bool searchBackward)
  {
    if (segment.VideoFile == null || segment.VideoFile.Fingerprints == null)
      return 0;

    double from;
    double to;

    if (searchBackward)
    {
      from = Math.Max(0, boundarySeconds - 12);
      to = boundarySeconds;
    }
    else
    {
      from = boundarySeconds;
      to = Math.Min(segment.VideoFile.DurationSeconds, boundarySeconds + 12);
    }

    return segment.VideoFile.Fingerprints
        .Where(f => f.TimeSeconds >= from && f.TimeSeconds <= to)
        .Count();
  }

  private static List<List<DetectedSegment>> BuildSegmentClusters(List<DetectedSegment> segments)
  {
    var clusters = new List<List<DetectedSegment>>();

    foreach (var segment in segments.OrderByDescending(s => s.ConfidencePercent))
    {
      List<DetectedSegment> bestCluster = null;
      double bestSimilarity = 0;

      foreach (var cluster in clusters)
      {
        double similarity = GetSegmentToClusterSimilarity(segment, cluster);

        if (similarity > bestSimilarity)
        {
          bestSimilarity = similarity;
          bestCluster = cluster;
        }
      }

      if (bestCluster != null && bestSimilarity >= SegmentClusterSimilarity)
        bestCluster.Add(segment);
      else
        clusters.Add(new List<DetectedSegment> { segment });
    }

    return clusters;
  }

  private static double GetSegmentToClusterSimilarity(
      DetectedSegment segment,
      List<DetectedSegment> cluster)
  {
    double best = 0;

    foreach (var other in cluster)
    {
      if (string.Equals(segment.VideoFile.Path, other.VideoFile.Path, StringComparison.OrdinalIgnoreCase))
        continue;

      double similarity = GetSegmentSimilarity(segment, other);

      if (similarity > best)
        best = similarity;
    }

    return best;
  }

  private static double GetSegmentSimilarity(
      DetectedSegment a,
      DetectedSegment b)
  {
    var aFps = a.VideoFile.Fingerprints
        .Where(f => f.TimeSeconds >= a.StartSeconds && f.TimeSeconds <= a.EndSeconds)
        .ToList();

    var bFps = b.VideoFile.Fingerprints
        .Where(f => f.TimeSeconds >= b.StartSeconds && f.TimeSeconds <= b.EndSeconds)
        .ToList();

    if (aFps.Count == 0 || bFps.Count == 0)
      return 0;

    var offsetVotes = new Dictionary<int, List<double>>();

    foreach (var fa in aFps)
    {
      foreach (var fb in bFps)
      {
        double similarity = GetHashSimilarity(fa.Hash, fb.Hash);

        if (similarity < MinHashSimilarity)
          continue;

        int offsetBucket = (int)Math.Round(
            (fb.TimeSeconds - fa.TimeSeconds) / HopSeconds);

        if (!offsetVotes.TryGetValue(offsetBucket, out var list))
        {
          list = new List<double>();
          offsetVotes[offsetBucket] = list;
        }

        list.Add(similarity);
      }
    }

    if (offsetVotes.Count == 0)
      return 0;

    var bestGroup = offsetVotes.Values
        .OrderByDescending(g => g.Count)
        .ThenByDescending(g => g.Average())
        .First();

    double matchedSeconds = bestGroup.Count * HopSeconds;

    double shorterDuration = Math.Min(
        a.EndSeconds - a.StartSeconds,
        b.EndSeconds - b.StartSeconds);

    double coverage = matchedSeconds / Math.Max(1.0, shorterDuration);
    double avgSimilarity = bestGroup.Average();

    return Math.Min(1.0, coverage) * avgSimilarity;
  }

  private static List<DetectedSegment> RecoverMissingIntroOutros(
      List<DetectedSegment> detected,
      List<VideoFile> videos)
  {
    var result = detected.ToList();

    foreach (var video in videos)
    {
      bool hasIntro = result.Any(s =>
          s.VideoFile.Path == video.Path &&
          s.Type == "Intro / Opening");

      bool hasOutro = result.Any(s =>
          s.VideoFile.Path == video.Path &&
          s.Type == "Outro / Ending");

      if (!hasIntro)
      {
        var recoveredIntro = TryRecoverSegment(
            video,
            result,
            "Intro / Opening");

        if (recoveredIntro != null)
          result.Add(recoveredIntro);
      }

      if (!hasOutro)
      {
        var recoveredOutro = TryRecoverSegment(
            video,
            result,
            "Outro / Ending");

        if (recoveredOutro != null)
          result.Add(recoveredOutro);
      }
    }

    return result;
  }

  private static DetectedSegment TryRecoverSegment(
      VideoFile targetVideo,
      List<DetectedSegment> knownSegments,
      string type)
  {
    if (targetVideo.Fingerprints == null || targetVideo.Fingerprints.Count == 0)
      return null;

    var candidates = knownSegments
        .Where(s => s.Type == type)
        .Where(s => s.VideoFile.Path != targetVideo.Path)
        .OrderByDescending(s => s.ConfidencePercent)
        .ToList();

    foreach (var candidate in candidates)
    {
      var recovered = TryFindSimilarSegmentInVideo(
          targetVideo,
          candidate,
          type);

      if (recovered != null)
        return recovered;
    }

    return null;
  }

  private static DetectedSegment TryFindSimilarSegmentInVideo(
      VideoFile targetVideo,
      DetectedSegment referenceSegment,
      string type)
  {
    var referenceFingerprints = referenceSegment.VideoFile.Fingerprints
        .Where(f =>
            f.TimeSeconds >= referenceSegment.StartSeconds &&
            f.TimeSeconds <= referenceSegment.EndSeconds)
        .ToList();

    if (referenceFingerprints.Count == 0)
      return null;

    double expectedStart = referenceSegment.StartSeconds;
    double expectedEnd = referenceSegment.EndSeconds;

    double searchStart;
    double searchEnd;

    if (type == "Intro / Opening")
    {
      searchStart = Math.Max(0, expectedStart - RecoveryTimeToleranceSeconds);

      searchEnd = Math.Min(
          targetVideo.DurationSeconds * IntroSearchMaxRatio,
          expectedEnd + RecoveryTimeToleranceSeconds);
    }
    else
    {
      searchStart = Math.Max(
          targetVideo.DurationSeconds * OutroSearchMinRatio,
          expectedStart - RecoveryTimeToleranceSeconds);

      searchEnd = Math.Min(
          targetVideo.DurationSeconds,
          expectedEnd + RecoveryTimeToleranceSeconds);
    }

    var targetCandidates = targetVideo.Fingerprints
        .Where(f => f.TimeSeconds >= searchStart && f.TimeSeconds <= searchEnd)
        .ToList();

    if (targetCandidates.Count == 0)
      return null;

    var offsetVotes = new Dictionary<int, List<MatchPoint>>();

    foreach (var refFp in referenceFingerprints)
    {
      foreach (var targetFp in targetCandidates)
      {
        double similarity = GetHashSimilarity(refFp.Hash, targetFp.Hash);

        if (similarity < MinHashSimilarity)
          continue;

        int offsetBucket = (int)Math.Round(
            (targetFp.TimeSeconds - refFp.TimeSeconds) / HopSeconds);

        if (!offsetVotes.TryGetValue(offsetBucket, out var list))
        {
          list = new List<MatchPoint>();
          offsetVotes[offsetBucket] = list;
        }

        list.Add(new MatchPoint(refFp, targetFp, similarity));
      }
    }

    var bestGroup = offsetVotes.Values
        .Select(g =>
        {
          var distinctTimes = g
          .Select(x => x.B.TimeSeconds)
          .Distinct()
          .OrderBy(x => x)
          .ToList();

          double matchedSeconds = distinctTimes.Count * HopSeconds;
          double avgSimilarity = g.Average(x => x.Similarity);

          double score =
          matchedSeconds * 0.70 +
          (avgSimilarity * 100.0) * 0.30;

          return new
          {
            Group = g,
            MatchedSeconds = matchedSeconds,
            AvgSimilarity = avgSimilarity,
            Score = score
          };
        })
        .Where(x => x.MatchedSeconds >= MinMatchedSeconds * 0.6)
        .OrderByDescending(x => x.Score)
        .ThenByDescending(x => x.MatchedSeconds)
        .ThenByDescending(x => x.AvgSimilarity)
        .Select(x => x.Group)
        .FirstOrDefault();

    if (bestGroup == null)
      return null;

    var times = bestGroup
        .Select(x => x.B.TimeSeconds)
        .Distinct()
        .OrderBy(x => x)
        .ToList();

    var segments = BuildSegments(times);

    if (segments.Count == 0)
      return null;

    var bestSegment = segments
        .OrderByDescending(s => s.End - s.Start)
        .First();

    double start = bestSegment.Start;
    double end = bestSegment.End + WindowSeconds;
    double duration = end - start;

    if (duration < MinSegmentSeconds || duration > MaxSegmentSeconds)
      return null;

    double confidence = bestGroup.Average(x => x.Similarity) * 100.0;

    return new DetectedSegment(
        targetVideo,
        start,
        end,
        type,
        confidence);
  }

  private static double GetHashSimilarity(ulong a, ulong b)
  {
    ulong diff = a ^ b;
    int differentBits = CountBits(diff);

    return 1.0 - differentBits / (double)totalBits;
  }

  private static int CountBits(ulong value)
  {
    int count = 0;

    while (value != 0)
    {
      value &= value - 1;
      count++;
    }

    return count;
  }

  private sealed class MatchPoint
  {
    public MatchPoint(Fingerprint a, Fingerprint b, double similarity)
    {
      A = a;
      B = b;
      Similarity = similarity;
    }

    public Fingerprint A { get; }
    public Fingerprint B { get; }
    public double Similarity { get; }
  }

  private sealed class TimeSegment
  {
    public TimeSegment(double start, double end)
    {
      Start = start;
      End = end;
    }

    public double Start { get; }
    public double End { get; }
  }
}