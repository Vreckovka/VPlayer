using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using VCore.WPF;
using VPlayer.Core.SoundVizualization;
using VPlayer.Player.UserControls;

namespace VPlayer.Core.UserControls
{
  /// <summary>
  /// Interaction logic for BackgroundStageLights.xaml
  /// </summary>
  public partial class BackgroundStageLights : UserControl
  {
    private readonly Stopwatch lightClock = Stopwatch.StartNew();

    private const int LightLookCount = 4;

    private double lightLookPhase;

    private double light1Intensity = 1.0;
    private double light2Intensity = 1.0;
    private double light3Intensity = 1.0;
    private double light4Intensity = 1.0;

    private double previousLightBass;
    private double lightActivityEnergy;
    private double lightActivity;
    private double lightSpeed = 0.08;

    private double lightSweepPhase;
    private double lightSpinPhase;
    private double lastLightUpdateTime;

    private static readonly Color BeamLowColor = Color.FromRgb(0x0F, 0x73, 0xFF);
    private static readonly Color BeamHighColor = Color.FromRgb(0x6F, 0x0F, 0xFF);

    public double LightActivityDecay { get; set; } = 1.8;
    public double LightActivityGain { get; set; } = 4.0;

    public double LightMinSpeed { get; set; } = 0.008;
    public double LightMaxSpeed { get; set; } = 0.65;
    public double LightSpeedSmoothing { get; set; } = 1.5;


    public BackgroundStageLights()
    {
      InitializeComponent();

      LightingDirector.RegisterBackgroundLights(this);
      LightingDirector.OnFftTick += LightingDirector_OnFftTick;

      IsEnabledChanged += OnIsEnabledChanged;
    }

    private void OnIsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
      if(IsEnabled)
      {
        Visibility = Visibility.Visible;
      }
      else
      {
        Visibility = Visibility.Collapsed;
      }
    }

    private void LightingDirector_OnFftTick(object sender, (double bass, double flux) e)
    {
      VSynchronizationContext.PostOnUIThread(() =>
      {
        if (!IsEnabled || Visibility != Visibility.Visible)
          return;
        
        UpdateConcertLight(e.bass, e.flux);
      });
    }

    public double BeamWidthMultiplier { get; set; } = 1.0;

    private void UpdateConcertLight(double bass, double flux)
    {
      double now = lightClock.Elapsed.TotalSeconds;

      if (lastLightUpdateTime <= 0)
        lastLightUpdateTime = now;

      double deltaTime = Math.Min(now - lastLightUpdateTime, 0.1);
      lastLightUpdateTime = now;

      double bassChange = Math.Abs(bass - previousLightBass);
      previousLightBass = bass;

      lightActivityEnergy *= Math.Exp(-LightActivityDecay * deltaTime);
      lightActivityEnergy += bassChange * LightActivityGain;
      lightActivity = Math.Min(1.0, lightActivityEnergy);

      double speedDrive = Math.Pow(lightActivity, 2.5);
      double targetSpeed = LightMinSpeed + speedDrive * (LightMaxSpeed - LightMinSpeed);
      double speedSmoothing = 1.0 - Math.Exp(-LightSpeedSmoothing * deltaTime);

      lightSpeed += (targetSpeed - lightSpeed) * speedSmoothing;

      lightSweepPhase += Math.PI * 2.0 * lightSpeed * deltaTime;
      lightSpinPhase += (0.10 + lightSpeed * 0.10) * deltaTime;

      double p = lightSweepPhase;

      lightLookPhase += deltaTime * (0.055 + lightActivity * 0.025);

      double lookPosition = LightingSystem.PositiveModulo(lightLookPhase, LightLookCount);
      int lookA = (int)Math.Floor(lookPosition);
      int lookB = (lookA + 1) % LightLookCount;
      double localLookPosition = lookPosition - lookA;
      double lookMix = LightingSystem.Smooth01(LightingSystem.Clamp01((localLookPosition - 0.65) / 0.35));

      double aspect = LightSweepOverlay.ActualHeight > 1.0 ? LightSweepOverlay.ActualWidth / LightSweepOverlay.ActualHeight : 16.0 / 9.0;

      LightingSystem.LightPose pose1A = LightingSystem.GetLightPose(lookA, 0, p, aspect);
      LightingSystem.LightPose pose2A = LightingSystem.GetLightPose(lookA, 1, p, aspect);
      LightingSystem.LightPose pose3A = LightingSystem.GetLightPose(lookA, 2, p, aspect);
      LightingSystem.LightPose pose4A = LightingSystem.GetLightPose(lookA, 3, p, aspect);

      LightingSystem.LightPose pose1B = LightingSystem.GetLightPose(lookB, 0, p, aspect);
      LightingSystem.LightPose pose2B = LightingSystem.GetLightPose(lookB, 1, p, aspect);
      LightingSystem.LightPose pose3B = LightingSystem.GetLightPose(lookB, 2, p, aspect);
      LightingSystem.LightPose pose4B = LightingSystem.GetLightPose(lookB, 3, p, aspect);

      LightingSystem.LightPose pose1 = LightingSystem.BlendPose(pose1A, pose1B, lookMix);
      LightingSystem.LightPose pose2 = LightingSystem.BlendPose(pose2A, pose2B, lookMix);
      LightingSystem.LightPose pose3 = LightingSystem.BlendPose(pose3A, pose3B, lookMix);
      LightingSystem.LightPose pose4 = LightingSystem.BlendPose(pose4A, pose4B, lookMix);

      double rigDrift = Math.Sin(lightSpinPhase) * 3.5 + Math.Sin(lightSpinPhase * 0.37 + 1.1) * 1.5;

      pose1.Angle += rigDrift;
      pose2.Angle += rigDrift;
      pose3.Angle += rigDrift;
      pose4.Angle += rigDrift;

      double intensity1A = LightingSystem.GetLookIntensity(lookA, 0, p);
      double intensity2A = LightingSystem.GetLookIntensity(lookA, 1, p);
      double intensity3A = LightingSystem.GetLookIntensity(lookA, 2, p);
      double intensity4A = LightingSystem.GetLookIntensity(lookA, 3, p);

      double intensity1B = LightingSystem.GetLookIntensity(lookB, 0, p);
      double intensity2B = LightingSystem.GetLookIntensity(lookB, 1, p);
      double intensity3B = LightingSystem.GetLookIntensity(lookB, 2, p);
      double intensity4B = LightingSystem.GetLookIntensity(lookB, 3, p);

      double rawIntensity1 = LightingSystem.Lerp(intensity1A, intensity1B, lookMix);
      double rawIntensity2 = LightingSystem.Lerp(intensity2A, intensity2B, lookMix);
      double rawIntensity3 = LightingSystem.Lerp(intensity3A, intensity3B, lookMix);
      double rawIntensity4 = LightingSystem.Lerp(intensity4A, intensity4B, lookMix);

      double intensityEffectMix = 0.25 + LightingSystem.Smooth01(LightingSystem.Clamp01((lightActivity - 0.20) / 0.70)) * 0.75;

      double targetIntensity1 = LightingSystem.Lerp(1.0, rawIntensity1, intensityEffectMix);
      double targetIntensity2 = LightingSystem.Lerp(1.0, rawIntensity2, intensityEffectMix);
      double targetIntensity3 = LightingSystem.Lerp(1.0, rawIntensity3, intensityEffectMix);
      double targetIntensity4 = LightingSystem.Lerp(1.0, rawIntensity4, intensityEffectMix);

      double fluxAccent = LightingSystem.Smooth01(LightingSystem.Clamp01((flux - 0.72) / 0.28));

      targetIntensity1 = LightingSystem.Lerp(targetIntensity1, 1.0, fluxAccent);
      targetIntensity2 = LightingSystem.Lerp(targetIntensity2, 1.0, fluxAccent);
      targetIntensity3 = LightingSystem.Lerp(targetIntensity3, 1.0, fluxAccent);
      targetIntensity4 = LightingSystem.Lerp(targetIntensity4, 1.0, fluxAccent);

      LightingSystem.SmoothLightIntensity(ref light1Intensity, targetIntensity1, deltaTime);
      LightingSystem.SmoothLightIntensity(ref light2Intensity, targetIntensity2, deltaTime);
      LightingSystem.SmoothLightIntensity(ref light3Intensity, targetIntensity3, deltaTime);
      LightingSystem.SmoothLightIntensity(ref light4Intensity, targetIntensity4, deltaTime);

      UpdateMountedLight(Light1Rotate, Light1Scale, Light1Beam, WeakLight1Gate, pose1.Angle, pose1.Depth, light1Intensity);
      UpdateMountedLight(Light2Rotate, Light2Scale, Light2Beam, WeakLight2Gate, pose2.Angle, pose2.Depth, light2Intensity);
      UpdateMountedLight(Light3Rotate, Light3Scale, Light3Beam, WeakLight3Gate, pose3.Angle, pose3.Depth, light3Intensity);
      UpdateMountedLight(Light4Rotate, Light4Scale, Light4Beam, WeakLight4Gate, pose4.Angle, pose4.Depth, light4Intensity);

      LightSweepOverlay.Opacity = Math.Min(0.30, 0.08 + bass * 0.14 + flux * 0.05);

      Color beamColor = LerpColor(BeamLowColor, BeamHighColor, bass);

      Resources["WeakBeamPeakColor"] = beamColor;
      Resources["ImageBeamPeakColor"] = beamColor;
    }

    private static Color LerpColor(Color from, Color to, double t)
    {
      t = Math.Max(0.0, Math.Min(1.0, t));

      return Color.FromArgb(
        (byte)(from.A + (to.A - from.A) * t),
        (byte)(from.R + (to.R - from.R) * t),
        (byte)(from.G + (to.G - from.G) * t),
        (byte)(from.B + (to.B - from.B) * t));
    }

    private void UpdateMountedLight(System.Windows.Media.RotateTransform rotate, System.Windows.Media.ScaleTransform scale, FrameworkElement imageBeam, FrameworkElement weakGate, double angle, double depth, double intensity)
    {
      rotate.Angle = angle;

      double scaleX = 0.92 + depth * 0.16;
      double scaleY = 0.78 + depth * 0.28;

      scale.ScaleX = scaleX * BeamWidthMultiplier;
      scale.ScaleY = scaleY;

      imageBeam.Opacity = (0.55 + depth * 0.45) * intensity;
      weakGate.Opacity = intensity;
    }

  }
}
