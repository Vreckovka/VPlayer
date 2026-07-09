using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using VPlayer.Core.SoundVizualization;
using VPlayer.Core.UserControls;

namespace VPlayer.Player.UserControls
{
  public static class LightingDirector
  {
    private static readonly Stopwatch clock = Stopwatch.StartNew();

    private static readonly AdaptiveSignalNormalizer psychedelicFluxNormalizer = new AdaptiveSignalNormalizer
    {
      AverageAdaptation = 0.08,
      PeakDecay = 0.985
    };

    private static readonly Queue<double> recentBeatIntervals = new Queue<double>();

    private static StageMacroState state = StageMacroState.Calm;
    private static StageMacroState? pendingState;
    private static TransitionQuantization pendingQuantization;

    private static BassImageVizualizer backgroundRgb;
    private static BassImageVizualizer mainRgb;
    private static BackgroundStageLights backgroundLights;
    private static FrontStageLights frontLights;

    private static double lastUpdateTime;
    private static double stateTime;
    private static double pendingStateTime;

    private static double fastEnergy;
    private static double slowEnergy;
    private static double previousEnergy;

    private static double visualFlux;
    private static float visualBass;

    private static double pulseFast;
    private static double pulseSlow;
    private static double onsetAverage = 0.02;
    private static double previousOnset;

    private static double lastBeatTime = -10.0;
    private static double beatInterval = 0.5;
    private static double beatConfidence;
    private static int beatIndex;
    private static int barIndex;

    private static double beatPulse;
    private static double accentPulse;
    private static double breakEnvelope;

    private static double lastAccentTime = -10.0;
    private static double lastBreakTime = -10.0;

    private static int liveVariation;
    private static int lastVariationBar = -1;

    private static double currentBackgroundMovement = 1.0;
    private static double currentMainMovement = 1.0;
    private static double currentBackgroundLightOpacity = 1.0;
    private static double currentFrontLightOpacity = 1.0;
    private static double currentBackgroundBeamWidth = 1.0;
    private static double currentFrontBeamWidth = 1.0;

    private static double currentFrontMinSpeedMultiplier = 1.0;
    private static double currentFrontMaxSpeedMultiplier = 1.0;

    private static double baseBackgroundMovement = 1.0;
    private static double baseMainMovement = 1.0;

    private static double baseBackgroundLightOpacity = 1.0;
    private static double baseFrontLightOpacity = 1.0;

    private static double baseFrontMinSpeed;
    private static double baseFrontMaxSpeed;

    private static bool isEnabled = true;
    private static bool applyPending;
    private static double pendingDeltaTime;

    public static float BassSensitivity { get; set; } = 1f;
    public static float BassCurve { get; set; } = 0.20f;
    public static float BassOutputGate { get; set; } = 0.06f;
    public static float BassPeakDecay { get; set; } = 0.997f;

    public static double FluxAttack { get; set; } = 0.65;
    public static double FluxRelease { get; set; } = 0.10;

    public static double EstimatedBpm => beatInterval > 0.0 ? 60.0 / beatInterval : 0.0;
    public static double BeatConfidence => beatConfidence;
    public static string CurrentState => state.ToString();

    public static event EventHandler<(double bass, double flux)> OnFftTick;

    public static bool IsEnabled
    {
      get => isEnabled;
      set
      {
        if (isEnabled == value)
          return;

        isEnabled = value;
        ResetDirectorState();
      }
    }

    static LightingDirector()
    {
      SpektrumAnalyzer.OnFFtTick += SpektrumAnalyzer_OnFFtTick;
    }

    public static void RegisterBackgroundRGB(BassImageVizualizer background)
    {
      if (ReferenceEquals(backgroundRgb, background))
        return;

      backgroundRgb = background;

      if (backgroundRgb != null)
      {
        baseBackgroundMovement = backgroundRgb.MovementMultiplier;
        currentBackgroundMovement = 1.0;
      }
    }

    public static void RegisterMainRGB(BassImageVizualizer main)
    {
      if (ReferenceEquals(mainRgb, main))
        return;

      mainRgb = main;

      if (mainRgb != null)
      {
        baseMainMovement = mainRgb.MovementMultiplier;
        currentMainMovement = 1.0;
      }
    }

    public static void RegisterBackgroundLights(BackgroundStageLights lights)
    {
      if (ReferenceEquals(backgroundLights, lights))
        return;

      backgroundLights = lights;

      if (backgroundLights != null)
      {
        baseBackgroundLightOpacity = backgroundLights.Opacity;
        currentBackgroundLightOpacity = 1.0;
        currentBackgroundBeamWidth = 1.0;
      }
    }

    public static void RegisterFrontLights(FrontStageLights lights)
    {
      if (ReferenceEquals(frontLights, lights))
        return;

      frontLights = lights;

      if (frontLights != null)
      {
        baseFrontLightOpacity = frontLights.Opacity;
        baseFrontMinSpeed = frontLights.MinSpeed;
        baseFrontMaxSpeed = frontLights.MaxSpeed;

        currentFrontLightOpacity = 1.0;
        currentFrontBeamWidth = 1.0;
        currentFrontMinSpeedMultiplier = 1.0;
        currentFrontMaxSpeedMultiplier = 1.0;
      }
    }

    private static void SpektrumAnalyzer_OnFFtTick(object sender, float[] fftData)
    {
      if (!IsEnabled || !HasRegisteredControls())
        return;

      float bass = SpektrumAnalyzer.AnalyzeBass(fftData, BassSensitivity, BassOutputGate, BassPeakDecay, BassCurve);
      float bassSmoothing = bass > visualBass ? 0.50f : 0.14f;

      visualBass += (bass - visualBass) * bassSmoothing;

      double rawFlux = SpektrumAnalyzer.GetPositiveSpectralFlux(fftData);
      double normalizedFlux = psychedelicFluxNormalizer.Update(rawFlux);
      double fluxSmoothing = normalizedFlux > visualFlux ? FluxAttack : FluxRelease;

      visualFlux += (normalizedFlux - visualFlux) * fluxSmoothing;

      //UpdateDirector(visualBass, visualFlux);

      OnFftTick?.Invoke(sender, (visualBass, visualFlux));
    }

    private static void UpdateDirector(double bass, double flux)
    {
      double now = clock.Elapsed.TotalSeconds;

      if (lastUpdateTime <= 0.0)
        lastUpdateTime = now;

      double deltaTime = Math.Min(now - lastUpdateTime, 0.1);

      lastUpdateTime = now;
      stateTime += deltaTime;

      if (pendingState.HasValue)
        pendingStateTime += deltaTime;

      double energy = Clamp01(bass * 0.75 + flux * 0.25);
      double fastResponse = energy > fastEnergy ? 4.5 : 2.2;
      double fastSmoothing = 1.0 - Math.Exp(-fastResponse * deltaTime);
      double slowSmoothing = 1.0 - Math.Exp(-0.55 * deltaTime);

      fastEnergy += (energy - fastEnergy) * fastSmoothing;
      slowEnergy += (energy - slowEnergy) * slowSmoothing;

      double trend = fastEnergy - slowEnergy;

      UpdateBeatTracking(now, deltaTime, bass, flux);
      UpdateLiveEvents(now, deltaTime, energy, flux, trend);
      UpdateState(flux, trend);
      UpdatePendingState();
      ScheduleApply(deltaTime);

      previousEnergy = energy;
    }

    private static void UpdateBeatTracking(double now, double deltaTime, double bass, double flux)
    {
      double pulseSignal = Clamp01(bass * 0.80 + flux * 0.20);
      double fastSmoothing = 1.0 - Math.Exp(-14.0 * deltaTime);
      double slowSmoothing = 1.0 - Math.Exp(-2.2 * deltaTime);

      pulseFast += (pulseSignal - pulseFast) * fastSmoothing;
      pulseSlow += (pulseSignal - pulseSlow) * slowSmoothing;

      double onset = Math.Max(0.0, pulseFast - pulseSlow);
      double onsetSmoothing = 1.0 - Math.Exp(-1.1 * deltaTime);

      onsetAverage += (onset - onsetAverage) * onsetSmoothing;

      double threshold = Math.Max(0.018, onsetAverage * 2.2);
      double minimumBeatDistance = Math.Max(0.22, beatInterval * 0.42);
      bool risingEdge = onset > threshold && previousOnset <= threshold;
      bool refractoryPassed = now - lastBeatTime > minimumBeatDistance;

      if (risingEdge && refractoryPassed)
        RegisterBeat(now, onset, threshold);

      if (now - lastBeatTime > beatInterval * 2.5)
        beatConfidence = Math.Max(0.0, beatConfidence - deltaTime * 0.20);

      beatPulse *= Math.Exp(-9.0 * deltaTime);
      accentPulse *= Math.Exp(-11.0 * deltaTime);
      breakEnvelope *= Math.Exp(-3.0 * deltaTime);

      previousOnset = onset;
    }

    private static void RegisterBeat(double now, double onset, double threshold)
    {
      double detectedInterval = now - lastBeatTime;

      if (lastBeatTime > 0.0 && detectedInterval >= 0.24 && detectedInterval <= 1.20)
      {
        detectedInterval = NormalizeBeatInterval(detectedInterval);

        recentBeatIntervals.Enqueue(detectedInterval);

        while (recentBeatIntervals.Count > 8)
          recentBeatIntervals.Dequeue();

        double medianInterval = GetMedian(recentBeatIntervals);

        beatInterval += (medianInterval - beatInterval) * 0.28;
        beatConfidence = Math.Min(1.0, beatConfidence + 0.12);
      }

      lastBeatTime = now;
      beatPulse = 1.0;
      beatIndex++;

      bool barBoundary = beatIndex % 4 == 0;

      if (barBoundary)
      {
        barIndex++;

        if (barIndex != lastVariationBar && barIndex % 2 == 0)
        {
          liveVariation = (liveVariation + 1) % 3;
          lastVariationBar = barIndex;
        }
      }

      CommitPendingState(barBoundary);

      if (onset > threshold * 1.8 && now - lastAccentTime > 0.30)
      {
        accentPulse = 1.0;
        lastAccentTime = now;
      }
    }

    private static double NormalizeBeatInterval(double interval)
    {
      if (beatConfidence < 0.20)
        return interval;

      while (interval < beatInterval * 0.62)
        interval *= 2.0;

      while (interval > beatInterval * 1.65)
        interval *= 0.5;

      return Math.Max(0.24, Math.Min(1.20, interval));
    }

    private static void UpdateLiveEvents(double now, double deltaTime, double energy, double flux, double trend)
    {
      bool strongAccent = flux > 0.86 && energy > 0.45;

      if (strongAccent && now - lastAccentTime > 0.28)
      {
        accentPulse = 1.0;
        lastAccentTime = now;
      }

      bool activeSection = state == StageMacroState.Drive || state == StageMacroState.Build || state == StageMacroState.Peak;
      bool sharpDrop = previousEnergy - energy > 0.20;
      bool fallingHard = trend < -0.14 && slowEnergy > 0.42;

      if (activeSection && (sharpDrop || fallingHard) && now - lastBreakTime > 3.0)
      {
        breakEnvelope = 1.0;
        lastBreakTime = now;
      }
    }

    private static void UpdateState(double flux, double trend)
    {
      switch (state)
      {
        case StageMacroState.Calm:
          if (stateTime > 0.8 && fastEnergy > 0.30)
            RequestState(StageMacroState.Drive, TransitionQuantization.Beat, false);
          break;

        case StageMacroState.Drive:
          if (stateTime > 2.0 && ((fastEnergy > 0.58 && trend > 0.03) || (slowEnergy > 0.62 && stateTime > 6.0)))
            RequestState(StageMacroState.Build, TransitionQuantization.Bar, false);
          else if (stateTime > 4.0 && slowEnergy < 0.22)
            RequestState(StageMacroState.Calm, TransitionQuantization.Bar, false);
          break;

        case StageMacroState.Build:
          if (stateTime > 1.5 && (fastEnergy > 0.78 || slowEnergy > 0.72 || flux > 0.88))
            RequestState(StageMacroState.Peak, TransitionQuantization.Beat, flux > 0.94);
          else if (stateTime > 3.0 && trend < -0.06 && fastEnergy < 0.55)
            RequestState(StageMacroState.Drive, TransitionQuantization.Beat, false);
          break;

        case StageMacroState.Peak:
          if (stateTime > 2.5 && fastEnergy < 0.58)
            RequestState(StageMacroState.Release, TransitionQuantization.Bar, false);
          break;

        case StageMacroState.Release:
          if (fastEnergy > 0.82 || flux > 0.92)
            RequestState(StageMacroState.Peak, TransitionQuantization.Beat, flux > 0.96);
          else if (stateTime > 2.5 && fastEnergy > 0.40 && trend > 0.03)
            RequestState(StageMacroState.Drive, TransitionQuantization.Beat, false);
          else if (stateTime > 3.0 && slowEnergy < 0.25)
            RequestState(StageMacroState.Calm, TransitionQuantization.Bar, false);
          break;
      }
    }

    private static void RequestState(StageMacroState newState, TransitionQuantization quantization, bool immediate)
    {
      if (state == newState)
      {
        pendingState = null;
        pendingStateTime = 0.0;
        return;
      }

      if (immediate || beatConfidence < 0.25)
      {
        SetState(newState);
        return;
      }

      if (pendingState == newState)
        return;

      pendingState = newState;
      pendingQuantization = quantization;
      pendingStateTime = 0.0;
    }

    private static void CommitPendingState(bool barBoundary)
    {
      if (!pendingState.HasValue)
        return;

      if (pendingQuantization == TransitionQuantization.Beat || barBoundary)
        SetState(pendingState.Value);
    }

    private static void UpdatePendingState()
    {
      if (!pendingState.HasValue)
        return;

      double maximumWait = pendingQuantization == TransitionQuantization.Beat ? 0.85 : 2.20;

      if (pendingStateTime >= maximumWait)
        SetState(pendingState.Value);
    }

    private static void ScheduleApply(double deltaTime)
    {
      pendingDeltaTime = Math.Min(0.25, pendingDeltaTime + deltaTime);

      if (applyPending)
        return;

      Dispatcher dispatcher = GetDispatcher();

      if (dispatcher == null)
        return;

      applyPending = true;

      dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() =>
      {
        double accumulatedDeltaTime = pendingDeltaTime;

        pendingDeltaTime = 0.0;
        applyPending = false;

        if (IsEnabled)
          ApplyState(accumulatedDeltaTime);
      }));
    }

    private static void ApplyState(double deltaTime)
    {
      StageLook look = GetStageLook(state);

      double backgroundMovementTarget = look.BackgroundMovement;
      double mainMovementTarget = look.MainMovement;
      double backgroundLightTarget = look.BackgroundLightOpacity;
      double frontLightTarget = look.FrontLightOpacity;
      double backgroundBeamTarget = look.BackgroundBeamWidth;
      double frontBeamTarget = look.FrontBeamWidth;

      ApplyLiveVariation(ref backgroundLightTarget, ref frontLightTarget);

      if (breakEnvelope > 0.001)
      {
        backgroundMovementTarget *= Lerp(1.0, 0.60, breakEnvelope);
        mainMovementTarget *= Lerp(1.0, 0.70, breakEnvelope);
        backgroundLightTarget *= Lerp(1.0, 0.05, breakEnvelope);
        frontLightTarget = Lerp(frontLightTarget, Math.Max(frontLightTarget, 0.85), breakEnvelope);
        backgroundBeamTarget = Lerp(backgroundBeamTarget, 1.40, breakEnvelope);
        frontBeamTarget = Lerp(frontBeamTarget, 1.20, breakEnvelope);
      }

      SmoothValue(ref currentBackgroundMovement, backgroundMovementTarget, deltaTime, 2.0);
      SmoothValue(ref currentMainMovement, mainMovementTarget, deltaTime, 2.0);
      SmoothValue(ref currentBackgroundLightOpacity, backgroundLightTarget, deltaTime, 3.0);
      SmoothValue(ref currentFrontLightOpacity, frontLightTarget, deltaTime, 3.0);
      SmoothValue(ref currentBackgroundBeamWidth, backgroundBeamTarget, deltaTime, 2.5);
      SmoothValue(ref currentFrontBeamWidth, frontBeamTarget, deltaTime, 2.5);
      SmoothValue(ref currentFrontMinSpeedMultiplier, look.FrontMinSpeedMultiplier, deltaTime, 2.0);
      SmoothValue(ref currentFrontMaxSpeedMultiplier, look.FrontMaxSpeedMultiplier, deltaTime, 2.0);

      double backgroundBeatStrength = GetBackgroundBeatStrength(state);
      double frontBeatStrength = GetFrontBeatStrength(state);

      double backgroundPulse = Clamp01(beatPulse * backgroundBeatStrength + accentPulse * 0.35);
      double frontPulse = Clamp01(beatPulse * frontBeatStrength + accentPulse * 0.45);

      double finalBackgroundLightOpacity = Lerp(currentBackgroundLightOpacity, 1.0, backgroundPulse);
      double finalFrontLightOpacity = Lerp(currentFrontLightOpacity, 1.0, frontPulse);

      if (backgroundRgb != null)
        backgroundRgb.MovementMultiplier = baseBackgroundMovement * currentBackgroundMovement;

      if (mainRgb != null)
        mainRgb.MovementMultiplier = baseMainMovement * currentMainMovement;

      if (backgroundLights != null)
      {
        backgroundLights.Opacity = baseBackgroundLightOpacity * finalBackgroundLightOpacity;
        backgroundLights.BeamWidthMultiplier = currentBackgroundBeamWidth;
      }

      if (frontLights != null)
      {
        frontLights.Opacity = baseFrontLightOpacity * finalFrontLightOpacity;
        frontLights.MinSpeed = baseFrontMinSpeed * currentFrontMinSpeedMultiplier;
        frontLights.MaxSpeed = baseFrontMaxSpeed * currentFrontMaxSpeedMultiplier;
        frontLights.BeamWidthMultiplier = currentFrontBeamWidth;
      }
    }

    private static void ApplyLiveVariation(ref double backgroundLightTarget, ref double frontLightTarget)
    {
      if (state != StageMacroState.Drive && state != StageMacroState.Build)
        return;

      switch (liveVariation)
      {
        case 1:
          frontLightTarget *= 0.78;
          break;

        case 2:
          backgroundLightTarget *= 0.82;
          break;
      }
    }

    private static double GetBackgroundBeatStrength(StageMacroState currentState)
    {
      switch (currentState)
      {
        case StageMacroState.Calm:
          return 0.03;

        case StageMacroState.Drive:
          return 0.10;

        case StageMacroState.Build:
          return 0.14;

        case StageMacroState.Peak:
          return 0.18;

        default:
          return 0.04;
      }
    }

    private static double GetFrontBeatStrength(StageMacroState currentState)
    {
      switch (currentState)
      {
        case StageMacroState.Calm:
          return 0.08;

        case StageMacroState.Drive:
          return 0.04;

        case StageMacroState.Build:
          return 0.09;

        case StageMacroState.Peak:
          return 0.15;

        default:
          return 0.10;
      }
    }

    private static StageLook GetStageLook(StageMacroState currentState)
    {
      switch (currentState)
      {
        case StageMacroState.Calm:
          return new StageLook(0.65, 0.55, 0.15, 0.70, 1.25, 1.25, 12.0, 1.35);

        case StageMacroState.Drive:
          return new StageLook(0.90, 1.00, 0.85, 0.25, 1.00, 0.95, 14.0, 1.65);

        case StageMacroState.Build:
          return new StageLook(1.00, 1.15, 1.00, 0.70, 0.88, 0.85, 16.0, 1.90);

        case StageMacroState.Peak:
          return new StageLook(1.10, 1.35, 1.00, 1.00, 0.72, 0.72, 18.0, 2.30);

        default:
          return new StageLook(0.55, 0.65, 0.15, 0.85, 1.35, 1.20, 12.0, 1.40);
      }
    }

    private static Dispatcher GetDispatcher()
    {
      if (backgroundLights != null)
        return backgroundLights.Dispatcher;

      if (frontLights != null)
        return frontLights.Dispatcher;

      if (mainRgb != null)
        return mainRgb.Dispatcher;

      if (backgroundRgb != null)
        return backgroundRgb.Dispatcher;

      return Application.Current?.Dispatcher;
    }

    private static bool HasRegisteredControls()
    {
      return backgroundRgb != null || mainRgb != null || backgroundLights != null || frontLights != null;
    }

    private static void SetState(StageMacroState newState)
    {
      if (state == newState)
        return;

      state = newState;
      stateTime = 0.0;
      pendingState = null;
      pendingStateTime = 0.0;
    }

    private static void ResetDirectorState()
    {
      state = StageMacroState.Calm;
      pendingState = null;

      stateTime = 0.0;
      pendingStateTime = 0.0;

      fastEnergy = 0.0;
      slowEnergy = 0.0;
      previousEnergy = 0.0;

      pulseFast = 0.0;
      pulseSlow = 0.0;
      onsetAverage = 0.02;
      previousOnset = 0.0;

      lastBeatTime = -10.0;
      beatInterval = 0.5;
      beatConfidence = 0.0;
      beatIndex = 0;
      barIndex = 0;

      beatPulse = 0.0;
      accentPulse = 0.0;
      breakEnvelope = 0.0;

      lastAccentTime = -10.0;
      lastBreakTime = -10.0;

      liveVariation = 0;
      lastVariationBar = -1;

      recentBeatIntervals.Clear();

      lastUpdateTime = 0.0;
      pendingDeltaTime = 0.0;

      currentBackgroundMovement = 1.0;
      currentMainMovement = 1.0;
      currentBackgroundLightOpacity = 1.0;
      currentFrontLightOpacity = 1.0;
      currentBackgroundBeamWidth = 1.0;
      currentFrontBeamWidth = 1.0;

      currentFrontMinSpeedMultiplier = 1.0;
      currentFrontMaxSpeedMultiplier = 1.0;
    }

    private static double GetMedian(IEnumerable<double> values)
    {
      double[] sorted = values.OrderBy(x => x).ToArray();

      if (sorted.Length == 0)
        return beatInterval;

      int middle = sorted.Length / 2;

      if (sorted.Length % 2 == 0)
        return (sorted[middle - 1] + sorted[middle]) * 0.5;

      return sorted[middle];
    }

    private static void SmoothValue(ref double current, double target, double deltaTime, double response)
    {
      double smoothing = 1.0 - Math.Exp(-response * deltaTime);

      current += (target - current) * smoothing;
    }

    private static double Lerp(double from, double to, double t)
    {
      return from + (to - from) * Clamp01(t);
    }

    private static double Clamp01(double value)
    {
      return Math.Max(0.0, Math.Min(1.0, value));
    }

    private enum StageMacroState
    {
      Calm,
      Drive,
      Build,
      Peak,
      Release
    }

    private enum TransitionQuantization
    {
      Beat,
      Bar
    }

    private struct StageLook
    {
      public double BackgroundMovement;
      public double MainMovement;
      public double BackgroundLightOpacity;
      public double FrontLightOpacity;
      public double BackgroundBeamWidth;
      public double FrontBeamWidth;
      public double FrontMinSpeedMultiplier;
      public double FrontMaxSpeedMultiplier;

      public StageLook(double backgroundMovement, double mainMovement, double backgroundLightOpacity, double frontLightOpacity, double backgroundBeamWidth, double frontBeamWidth, double frontMinSpeedMultiplier, double frontMaxSpeedMultiplier)
      {
        BackgroundMovement = backgroundMovement;
        MainMovement = mainMovement;
        BackgroundLightOpacity = backgroundLightOpacity;
        FrontLightOpacity = frontLightOpacity;
        BackgroundBeamWidth = backgroundBeamWidth;
        FrontBeamWidth = frontBeamWidth;
        FrontMinSpeedMultiplier = frontMinSpeedMultiplier;
        FrontMaxSpeedMultiplier = frontMaxSpeedMultiplier;
      }
    }
  }
}