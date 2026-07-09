using System;
using System.Diagnostics;
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

    private static StageMacroState state = StageMacroState.Calm;

    private static BassImageVizualizer backgroundRgb;
    private static BassImageVizualizer mainRgb;
    private static BackgroundStageLights backgroundLights;
    private static FrontStageLights frontLights;

    private static double lastUpdateTime;
    private static double stateTime;
    private static double fastEnergy;
    private static double slowEnergy;
    private static double visualFlux;
    private static float visualBass;

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

    public static event EventHandler<(double bass, double flux)> OnFftTick;

    public static bool IsEnabled
    {
      get => isEnabled;
      set
      {
        if (isEnabled == value)
          return;

        isEnabled = value;

        if (!isEnabled)
        {
          ResetDirectorState();
        }
        else
        {
          ResetDirectorState();
        }
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

      UpdateDirector(visualBass, visualFlux);

      OnFftTick?.Invoke(sender, (visualBass, visualFlux));
    }

    private static void UpdateDirector(double bass, double flux)
    {
      double now = clock.Elapsed.TotalSeconds;

      if (lastUpdateTime <= 0)
        lastUpdateTime = now;

      double deltaTime = Math.Min(now - lastUpdateTime, 0.1);

      lastUpdateTime = now;
      stateTime += deltaTime;

      double energy = Clamp01(bass * 0.75 + flux * 0.25);
      double fastResponse = energy > fastEnergy ? 4.5 : 2.2;
      double fastSmoothing = 1.0 - Math.Exp(-fastResponse * deltaTime);
      double slowSmoothing = 1.0 - Math.Exp(-0.55 * deltaTime);

      fastEnergy += (energy - fastEnergy) * fastSmoothing;
      slowEnergy += (energy - slowEnergy) * slowSmoothing;

      double trend = fastEnergy - slowEnergy;

      UpdateState(flux, trend);
      ScheduleApply(deltaTime);
    }

    private static void UpdateState(double flux, double trend)
    {
      switch (state)
      {
        case StageMacroState.Calm:
          if (stateTime > 0.8 && fastEnergy > 0.30)
            SetState(StageMacroState.Drive);
          break;

        case StageMacroState.Drive:
          if (stateTime > 2.0 && ((fastEnergy > 0.58 && trend > 0.03) || (slowEnergy > 0.62 && stateTime > 6.0)))
            SetState(StageMacroState.Build);
          else if (stateTime > 4.0 && slowEnergy < 0.22)
            SetState(StageMacroState.Calm);
          break;

        case StageMacroState.Build:
          if (stateTime > 1.5 && (fastEnergy > 0.78 || slowEnergy > 0.72 || flux > 0.88))
            SetState(StageMacroState.Peak);
          else if (stateTime > 3.0 && trend < -0.06 && fastEnergy < 0.55)
            SetState(StageMacroState.Drive);
          break;

        case StageMacroState.Peak:
          if (stateTime > 2.5 && fastEnergy < 0.58)
            SetState(StageMacroState.Release);
          break;

        case StageMacroState.Release:
          if (fastEnergy > 0.82 || flux > 0.92)
            SetState(StageMacroState.Peak);
          else if (stateTime > 2.5 && fastEnergy > 0.40 && trend > 0.03)
            SetState(StageMacroState.Drive);
          else if (stateTime > 3.0 && slowEnergy < 0.25)
            SetState(StageMacroState.Calm);
          break;
      }
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

      dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() =>
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

      SmoothValue(ref currentBackgroundMovement, look.BackgroundMovement, deltaTime, 2.0);
      SmoothValue(ref currentMainMovement, look.MainMovement, deltaTime, 2.0);
      SmoothValue(ref currentBackgroundLightOpacity, look.BackgroundLightOpacity, deltaTime, 3.0);
      SmoothValue(ref currentFrontLightOpacity, look.FrontLightOpacity, deltaTime, 3.0);
      SmoothValue(ref currentBackgroundBeamWidth, look.BackgroundBeamWidth, deltaTime, 2.5);
      SmoothValue(ref currentFrontBeamWidth, look.FrontBeamWidth, deltaTime, 2.5);
      SmoothValue(ref currentFrontMinSpeedMultiplier, look.FrontMinSpeedMultiplier, deltaTime, 2.0);
      SmoothValue(ref currentFrontMaxSpeedMultiplier, look.FrontMaxSpeedMultiplier, deltaTime, 2.0);

      if (backgroundRgb != null)
        backgroundRgb.MovementMultiplier = baseBackgroundMovement * currentBackgroundMovement;

      if (mainRgb != null)
        mainRgb.MovementMultiplier = baseMainMovement * currentMainMovement;

      if (backgroundLights != null)
      {
        backgroundLights.Opacity = baseBackgroundLightOpacity * currentBackgroundLightOpacity;
        backgroundLights.BeamWidthMultiplier = currentBackgroundBeamWidth;
      }

      if (frontLights != null)
      {
        frontLights.Opacity = baseFrontLightOpacity * currentFrontLightOpacity;
        frontLights.MinSpeed = baseFrontMinSpeed * currentFrontMinSpeedMultiplier;
        frontLights.MaxSpeed = baseFrontMaxSpeed * currentFrontMaxSpeedMultiplier;
        frontLights.BeamWidthMultiplier = currentFrontBeamWidth;
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
    }

    private static void ResetDirectorState()
    {
      state = StageMacroState.Calm;
      stateTime = 0.0;
      fastEnergy = 0.0;
      slowEnergy = 0.0;
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

    private static void SmoothValue(ref double current, double target, double deltaTime, double response)
    {
      double smoothing = 1.0 - Math.Exp(-response * deltaTime);

      current += (target - current) * smoothing;
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