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
  /// Interaction logic for FrontStageLights.xaml
  /// </summary>
  public partial class FrontStageLights : UserControl
  {
    private readonly Stopwatch lightClock = Stopwatch.StartNew();

    private const int LookCount = 4;

    private double previousBass;
    private double activityEnergy;
    private double activity;
    private double speed = 0.035;
    private double phase;
    private double lookPhase;
    private double lastUpdateTime;
    private double light1Intensity = 1.0;
    private double light2Intensity = 1.0;

    private static readonly double[] MountX = { 0.12, 0.88 };
    public Color LowColor { get; set; } = Color.FromRgb(0xB7, 0xD9, 0xFF);
    public Color HighColor { get; set; } = Color.FromRgb(0xFF, 0x5E, 0xA8);

    public double ActivityDecay { get; set; } = 1.5;
    public double ActivityGain { get; set; } = 2.8;
    public double MinSpeed { get; set; } = 0.003;
    public double MaxSpeed { get; set; } = 0.18;
    public double SpeedSmoothing { get; set; } = 1.2;
    public double BeamWidthMultiplier { get; set; } = 1.0;

    public FrontStageLights()
    {
      InitializeComponent();

      LightingDirector.RegisterFrontLights(this);
      LightingDirector.OnFftTick += LightingDirector_OnFftTick;

      IsEnabledChanged += OnIsEnabledChanged;
    }

    private void OnIsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
      if (IsEnabled)
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
        
        UpdateLighting(e.bass, e.flux);
      });
    }

    public void UpdateLighting(double bass, double flux)
    {
      double now = lightClock.Elapsed.TotalSeconds;

      if (lastUpdateTime <= 0)
        lastUpdateTime = now;

      double deltaTime = Math.Min(now - lastUpdateTime, 0.1);
      lastUpdateTime = now;

      double bassChange = Math.Abs(bass - previousBass);
      previousBass = bass;

      activityEnergy *= Math.Exp(-ActivityDecay * deltaTime);
      activityEnergy += bassChange * ActivityGain;
      activity = Math.Min(1.0, activityEnergy);

      double speedDrive = Math.Pow(activity, 2.4);
      double targetSpeed = MinSpeed + speedDrive * (MaxSpeed - MinSpeed);
      double speedSmoothing = 1.0 - Math.Exp(-SpeedSmoothing * deltaTime);

      speed += (targetSpeed - speed) * speedSmoothing;

      phase += Math.PI * 2.0 * speed * deltaTime;
      lookPhase += deltaTime * (0.028 + activity * 0.018);

      double p = phase;
      double lookPosition = PositiveModulo(lookPhase, LookCount);
      int lookA = (int)Math.Floor(lookPosition);
      int lookB = (lookA + 1) % LookCount;
      double localLookPosition = lookPosition - lookA;
      double lookMix = Smooth01(Clamp01((localLookPosition - 0.60) / 0.40));
      double aspect = ActualHeight > 1.0 ? ActualWidth / ActualHeight : 16.0 / 9.0;

      FrontPose pose1 = BlendPose(GetPose(lookA, 0, p, aspect), GetPose(lookB, 0, p, aspect), lookMix);
      FrontPose pose2 = BlendPose(GetPose(lookA, 1, p, aspect), GetPose(lookB, 1, p, aspect), lookMix);

      double rawIntensity1 = Lerp(GetIntensity(lookA, 0, p), GetIntensity(lookB, 0, p), lookMix);
      double rawIntensity2 = Lerp(GetIntensity(lookA, 1, p), GetIntensity(lookB, 1, p), lookMix);

      double musicLevel = Clamp01(0.72 + bass * 0.18 + flux * 0.10);
      double fluxAccent = Smooth01(Clamp01((flux - 0.70) / 0.30));

      double targetIntensity1 = Lerp(rawIntensity1 * musicLevel, 1.0, fluxAccent);
      double targetIntensity2 = Lerp(rawIntensity2 * musicLevel, 1.0, fluxAccent);

      SmoothIntensity(ref light1Intensity, targetIntensity1, deltaTime);
      SmoothIntensity(ref light2Intensity, targetIntensity2, deltaTime);

      UpdateFrontLight(FrontLight1Rotate, FrontLight1Scale, FrontLight1ImageBeam, FrontLight1WeakGate, pose1.Angle, pose1.Depth, light1Intensity);
      UpdateFrontLight(FrontLight2Rotate, FrontLight2Scale, FrontLight2ImageBeam, FrontLight2WeakGate, pose2.Angle, pose2.Depth, light2Intensity);

      Color color = LerpColor(LowColor, HighColor, bass);

      Resources["FrontWeakBeamPeakColor"] = color;
      Resources["FrontImageBeamPeakColor"] = color;
    }

    private static FrontPose GetPose(int look, int lightIndex, double p, double aspect)
    {
      switch (look)
      {
        case 0:
          return GetCrossPose(lightIndex, p, aspect);

        case 1:
          return GetConvergencePose(lightIndex, p, aspect);

        case 2:
          return GetOpenPose(lightIndex, p, aspect);

        default:
          return GetSidePose(lightIndex, p, aspect);
      }
    }
    private const double MaxBeamAngle = 72.0;
    private static FrontPose GetCrossPose(int lightIndex, double p, double aspect)
    {
      double center = 0.50 + Math.Sin(p * 0.62) * 0.15;
      double spread = 0.10 + (0.5 + Math.Sin(p * 0.41 + 0.7) * 0.5) * 0.05;

      double targetX = lightIndex == 0 ? center + spread : center - spread;
      targetX = Clamp(targetX, 0.25, 0.75);

      double targetY = 0.44 + Math.Sin(p * 0.18 + 0.6) * 0.04;

      double angle = GetBottomBeamAngle(MountX[lightIndex], targetX, targetY, aspect);
      double depth = 0.56 + Math.Cos(p * 0.38 + lightIndex * 0.65) * 0.22;

      return new FrontPose(Clamp(angle, -MaxBeamAngle, MaxBeamAngle), Clamp01(depth));
    }

    private static FrontPose GetConvergencePose(int lightIndex, double p, double aspect)
    {
      double targetX = 0.50 + Math.Sin(p * 0.55) * 0.18 + Math.Sin(p * 0.19 + 1.0) * 0.05;
      targetX = Clamp(targetX, 0.25, 0.75);

      double targetY = 0.49 + Math.Cos(p * 0.17) * 0.04;

      double angle = GetBottomBeamAngle(MountX[lightIndex], targetX, targetY, aspect);
      double depth = 0.68 + Math.Sin(p * 0.31 + lightIndex * 0.35) * 0.12;

      return new FrontPose(Clamp(angle, -MaxBeamAngle, MaxBeamAngle), Clamp01(depth));
    }

    private static FrontPose GetOpenPose(int lightIndex, double p, double aspect)
    {
      double movement = Math.Sin(p * 0.68 + lightIndex * Math.PI) * 0.13;

      double targetX = lightIndex == 0 ? 0.36 + movement : 0.64 + movement;
      targetX = Clamp(targetX, 0.25, 0.75);

      double targetY = 0.43 + Math.Sin(p * 0.17 + lightIndex * 0.7) * 0.035;

      double angle = GetBottomBeamAngle(MountX[lightIndex], targetX, targetY, aspect);
      double depth = 0.52 + Math.Cos(p * 0.29 + lightIndex * 0.8) * 0.20;

      return new FrontPose(Clamp(angle, -MaxBeamAngle, MaxBeamAngle), Clamp01(depth));
    }

    private static FrontPose GetSidePose(int lightIndex, double p, double aspect)
    {
      double sweep = Math.Sin(p * 0.58) * 0.12;

      double targetX = lightIndex == 0 ? 0.38 + sweep : 0.62 + sweep;
      targetX = Clamp(targetX, 0.25, 0.75);

      double targetY = 0.46 + Math.Cos(p * 0.17 + lightIndex * 0.5) * 0.04;

      double angle = GetBottomBeamAngle(MountX[lightIndex], targetX, targetY, aspect);
      double depth = 0.58 + Math.Sin(p * 0.24 + lightIndex * Math.PI) * 0.24;

      return new FrontPose(Clamp(angle, -MaxBeamAngle, MaxBeamAngle), Clamp01(depth));
    }

    private static double GetIntensity(int look, int lightIndex, double p)
    {
      switch (look)
      {
        case 0:
          return 0.85 + (0.5 + Math.Sin(p * 0.42 + lightIndex * 0.3) * 0.5) * 0.15;

        case 1:
          return 1.0;

        case 2:
          return 0.72 + (0.5 + Math.Sin(p * 0.33 + lightIndex * 0.8) * 0.5) * 0.18;

        default:
          {
            double emphasis = 0.5 + Math.Sin(p * 0.38) * 0.5;

            return lightIndex == 0 ? 0.25 + emphasis * 0.75 : 0.25 + (1.0 - emphasis) * 0.75;
          }
      }
    }

    private void UpdateFrontLight(RotateTransform rotate, ScaleTransform scale, FrameworkElement imageBeam, FrameworkElement weakGate, double angle, double depth, double intensity)
    {
      rotate.Angle = angle;
      scale.ScaleX = (0.86 + depth * 0.22) * BeamWidthMultiplier;
      scale.ScaleY = 0.82 + depth * 0.22;
      imageBeam.Opacity = (0.60 + depth * 0.40) * intensity;
      weakGate.Opacity = (0.55 + depth * 0.45) * intensity;
    }

    private static double GetBottomBeamAngle(double mountX, double targetX, double targetY, double aspect)
    {
      double horizontalDistance = (targetX - mountX) * aspect;
      double verticalDistance = Math.Max(0.05, 1.0 - targetY);

      return Math.Atan2(horizontalDistance, verticalDistance) * 180.0 / Math.PI;
    }

    private static void SmoothIntensity(ref double current, double target, double deltaTime)
    {
      double response = target > current ? 6.0 : 2.8;
      double smoothing = 1.0 - Math.Exp(-response * deltaTime);

      current += (target - current) * smoothing;
    }

    private static FrontPose BlendPose(FrontPose from, FrontPose to, double t)
    {
      return new FrontPose(Lerp(from.Angle, to.Angle, t), Lerp(from.Depth, to.Depth, t));
    }

    private static Color LerpColor(Color from, Color to, double t)
    {
      t = Clamp01(t);

      return Color.FromArgb(
        (byte)(from.A + (to.A - from.A) * t),
        (byte)(from.R + (to.R - from.R) * t),
        (byte)(from.G + (to.G - from.G) * t),
        (byte)(from.B + (to.B - from.B) * t));
    }

    private static double PositiveModulo(double value, double modulus)
    {
      double result = value % modulus;

      return result < 0 ? result + modulus : result;
    }

    private static double Smooth01(double value)
    {
      value = Clamp01(value);

      return value * value * (3.0 - 2.0 * value);
    }

    private static double Clamp01(double value)
    {
      return Math.Max(0.0, Math.Min(1.0, value));
    }

    private static double Clamp(double value, double minimum, double maximum)
    {
      return Math.Max(minimum, Math.Min(maximum, value));
    }

    private static double Lerp(double from, double to, double t)
    {
      return from + (to - from) * t;
    }

    private struct FrontPose
    {
      public double Angle;
      public double Depth;

      public FrontPose(double angle, double depth)
      {
        Angle = angle;
        Depth = depth;
      }
    }
  }
}

