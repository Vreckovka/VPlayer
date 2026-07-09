using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using Ninject.Activation;
using VCore.WPF;
using VCore.WPF.Controls;
using VPlayer.Core.SoundVizualization;
using Windows.UI.Xaml.Media;
using static VPlayer.Player.UserControls.LightingSystem;
using Color = System.Windows.Media.Color;

namespace VPlayer.Player.UserControls
{
  public partial class BassImageVizualizer : UserControl
  {
    private BitmapSource originalImage;

    private float visualBass;

    #region ImagePath

    public string ImagePath
    {
      get => (string)GetValue(ImagePathProperty);
      set => SetValue(ImagePathProperty, value);
    }

    public static readonly DependencyProperty ImagePathProperty =
      DependencyProperty.Register(
        nameof(ImagePath),
        typeof(string),
        typeof(BassImageVizualizer),
        new PropertyMetadata(
          null,
          ReloadImage));


    #endregion

    #region VizualizerLayerPath

    public string VizualizerLayerPath
    {
      get => (string)GetValue(VizualizerLayerPathProperty);
      set => SetValue(VizualizerLayerPathProperty, value);
    }

    public static readonly DependencyProperty VizualizerLayerPathProperty =
      DependencyProperty.Register(
        nameof(VizualizerLayerPath),
        typeof(string),
        typeof(BassImageVizualizer),
        new PropertyMetadata(
          null,
          ReloadImage));


    #endregion

    #region StableVizualizerLayerPath

    public string StableVizualizerLayerPath
    {
      get => (string)GetValue(StableVizualizerLayerPathProperty);
      set => SetValue(StableVizualizerLayerPathProperty, value);
    }

    public static readonly DependencyProperty StableVizualizerLayerPathProperty =
      DependencyProperty.Register(
        nameof(StableVizualizerLayerPath),
        typeof(string),
        typeof(BassImageVizualizer),
        new PropertyMetadata(
          null,
          ReloadImage));


    #endregion

    #region Stretch

    public System.Windows.Media.Stretch Stretch
    {
      get => (System.Windows.Media.Stretch)GetValue(StretchProperty);
      set => SetValue(StretchProperty, value);
    }

    public static readonly DependencyProperty StretchProperty =
      DependencyProperty.Register(
        nameof(Stretch),
        typeof(System.Windows.Media.Stretch),
        typeof(BassImageVizualizer),
        new PropertyMetadata(
          System.Windows.Media.Stretch.Uniform,
          ReloadImage));


    #endregion

    #region LowBassImageColor

    public Color LowBassImageColor
    {
      get => (Color)GetValue(LowBassImageColorProperty);
      set => SetValue(LowBassImageColorProperty, value);
    }

    public static readonly DependencyProperty LowBassImageColorProperty =
      DependencyProperty.Register(
        nameof(LowBassImageColor),
        typeof(Color),
        typeof(BassImageVizualizer),
        new PropertyMetadata(
          Color.FromRgb(0, 0, 255),
          ReloadImage));


    #endregion

    #region MidBassImageColor

    public Color MidBassImageColor
    {
      get => (Color)GetValue(MidBassImageColorProperty);
      set => SetValue(MidBassImageColorProperty, value);
    }

    public static readonly DependencyProperty MidBassImageColorProperty =
      DependencyProperty.Register(
        nameof(MidBassImageColor),
        typeof(Color),
        typeof(BassImageVizualizer),
        new PropertyMetadata(
          Color.FromRgb(66, 245, 72),
          ReloadImage));


    #endregion

    #region HighBassImageColor

    public Color HighBassImageColor
    {
      get => (Color)GetValue(HighBassImageColorProperty);
      set => SetValue(HighBassImageColorProperty, value);
    }

    public static readonly DependencyProperty HighBassImageColorProperty =
      DependencyProperty.Register(
        nameof(HighBassImageColor),
        typeof(Color),
        typeof(BassImageVizualizer),
        new PropertyMetadata(
           Color.FromRgb(255, 0, 0),
          ReloadImage));


    #endregion

    private static void ReloadImage(DependencyObject d,
     DependencyPropertyChangedEventArgs e)
    {
      var visualizer =
      (BassImageVizualizer)d;

      visualizer.LoadPsychedelicImage();
      visualizer.LoadStableImage();
    }

    public bool Stablized { get; set; } = false;
    public float BassSensitivity { get; set; } = 1.35f;
    public float BassCurve { get; set; } = 0.20f;
    public float BassOutputGate { get; set; } = 0.06f;
    public float BassPeakDecay { get; set; } = 0.997f;
    public float ZoomStrength { get; set; } = 0.05f;
    public float ShakeStrength { get; set; } = 8f;

    public float OpacityModifier { get; set; } = 1;

    public BassImageVizualizer()
    {
      InitializeComponent();


      SpektrumAnalyzer.OnFFtTick += SpektrumAnalyzer_OnFFtTick;
    }

    private void SpektrumAnalyzer_OnFFtTick(object sender, float[] fftData)
    {

      VSynchronizationContext.PostOnUIThread(async () =>
      {
        if (!IsEnabled) return;
        if (Visibility != Visibility.Visible) return;

        float bass = SpektrumAnalyzer.AnalyzeBass(fftData, BassSensitivity, BassOutputGate, BassPeakDecay, BassCurve);

        float smoothing = bass > visualBass ? 0.50f : 0.14f;

        visualBass += (bass - visualBass) * smoothing;


        ApplyPsychedelicImageEffect(visualBass, fftData);
      }
       );
    }

    #region LoadStableImage

    private void LoadStableImage()
    {
      if (string.IsNullOrWhiteSpace(StableVizualizerLayerPath))
      {
        StableImage.Source = null;
        return;
      }

      var bitmap = new BitmapImage();

      bitmap.BeginInit();
      bitmap.UriSource = new Uri(StableVizualizerLayerPath, UriKind.RelativeOrAbsolute);
      bitmap.CacheOption = BitmapCacheOption.OnLoad;
      bitmap.EndInit();
      bitmap.Freeze();

      StableImage.Source = bitmap;
      StableImage.Stretch = Stretch;
    }

    #endregion

    #region LoadPsychedelicImage

    private void LoadPsychedelicImage()
    {
      string imageSource = VizualizerLayerPath;

      if (string.IsNullOrEmpty(imageSource))
        imageSource = ImagePath;

      if (string.IsNullOrWhiteSpace(imageSource))
      {
        originalImage = null;

        RedImage.Source = null;
        GreenImage.Source = null;
        BlueImage.Source = null;
        OriginalImage.Source = null;

        return;
      }

      try
      {
        var bitmap = new BitmapImage();

        bitmap.BeginInit();
        bitmap.UriSource = new Uri(imageSource, UriKind.RelativeOrAbsolute);
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.EndInit();
        bitmap.Freeze();

        originalImage = bitmap;

        OriginalImage.Source = originalImage;

        RedImage.Source = CreateTintedBitmap(originalImage, HighBassImageColor);
        GreenImage.Source = CreateTintedBitmap(originalImage, MidBassImageColor);
        BlueImage.Source = CreateTintedBitmap(originalImage, LowBassImageColor);


        OriginalImage.Stretch = Stretch;
        GreenImage.Stretch = Stretch;
        BlueImage.Stretch = Stretch;
        RedImage.Stretch = Stretch;
      }
      catch (Exception ex)
      {
        Debug.WriteLine(
          $"Could not load visualizer image: {ex}");

        originalImage = null;

        RedImage.Source = null;
        GreenImage.Source = null;
        BlueImage.Source = null;
        OriginalImage.Source = null;
      }
    }

    #endregion

    #region CreateTintedBitmap

    private static BitmapSource CreateTintedBitmap(BitmapSource source, Color tintColor)
    {
      BitmapSource formattedSource = new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);

      int width = formattedSource.PixelWidth;
      int height = formattedSource.PixelHeight;
      int stride = width * 4;
      byte[] pixels = new byte[height * stride];

      formattedSource.CopyPixels(pixels, stride, 0);

      for (int i = 0; i < pixels.Length; i += 4)
      {
        byte b = pixels[i];
        byte g = pixels[i + 1];
        byte r = pixels[i + 2];
        byte a = pixels[i + 3];
        byte brightness = (byte)(r * 0.299 + g * 0.587 + b * 0.114);
        pixels[i] = (byte)(brightness * tintColor.B / 255);
        pixels[i + 1] = (byte)(brightness * tintColor.G / 255);
        pixels[i + 2] = (byte)(brightness * tintColor.R / 255);
        pixels[i + 3] = a;
      }

      var tintedBitmap = new WriteableBitmap(width, height, formattedSource.DpiX, formattedSource.DpiY, PixelFormats.Bgra32, null);
      tintedBitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, stride, 0);
      tintedBitmap.Freeze();

      return tintedBitmap;
    }

    #endregion

    #region PsychedelicEffectState

    private float[] psychedelicPreviousSpectrum;
    private double visualFlux = 0.0;

    #endregion


    #region PsychedelicEffectSettings

    // Maximum extra RGB separation caused by a sudden spectral attack.
    public double FluxSeparationStrength { get; set; } = 2.0;

    // Fast response to attacks.
    public double FluxAttack { get; set; } = 0.65;

    // Slower return after attack.
    public double FluxRelease { get; set; } = 0.10;


    // Adaptive normalization of spectral flux.
    private readonly AdaptiveSignalNormalizer psychedelicFluxNormalizer =
      new AdaptiveSignalNormalizer
      {
        AverageAdaptation = 0.08,
        PeakDecay = 0.985
      };

    // Stable layer temporarily reveals more RGB on attacks.
    public double StableBaseOpacity { get; set; } = 1.0;

    public double StableFluxRevealStrength { get; set; } = 0.06;


    // Soft RGB shadow visibility.
    public double ShadowOpacity { get; set; } = 0.16;

    // Shadow sits further away than the sharp RGB layer.
    public double ShadowDistanceMultiplier { get; set; } = 1.8;


    // Allows different BassImageVizualizer instances
    // to move different amounts.
    public double MovementMultiplier { get; set; } = 1.0;

    #endregion


    #region ApplyPsychedelicImageEffect

    private void ApplyPsychedelicImageEffect(float bass, float[] fftData)
    {
      bass = SpektrumAnalyzer.Clamp(bass);

      double redOpacity;
      double greenOpacity;
      double blueOpacity;

      if (bass <= 0.5f)
      {
        double t =
          bass / 0.5;

        blueOpacity = 1.0 - t;
        greenOpacity = t;
        redOpacity = 0.0;
      }
      else
      {
        double t = (bass - 0.5) / 0.5;

        blueOpacity = 0.0;
        greenOpacity = 1.0 - t;
        redOpacity = t;
      }

      if (bass <= 0.5f)
      {
        BoostOpacityPair(ref blueOpacity, ref greenOpacity);
      }
      else
      {
        BoostOpacityPair(ref greenOpacity, ref redOpacity);
      }

      RedImage.Opacity = redOpacity * OpacityModifier;
      GreenImage.Opacity = greenOpacity * OpacityModifier;
      BlueImage.Opacity = blueOpacity * OpacityModifier;

      double opacitySum = RedImage.Opacity + GreenImage.Opacity + BlueImage.Opacity;

      if (opacitySum > 0.0 && Math.Abs(opacitySum - 1.0) > 0.000001)
      {
        RedImage.Opacity /= opacitySum;
        GreenImage.Opacity /= opacitySum;
        BlueImage.Opacity /= opacitySum;
      }

      RedImage.Opacity = redOpacity * OpacityModifier;
      GreenImage.Opacity = greenOpacity * OpacityModifier;
      BlueImage.Opacity = blueOpacity * OpacityModifier;

      RedShadow.Opacity = redOpacity * ShadowOpacity;
      GreenShadow.Opacity = greenOpacity * ShadowOpacity;
      BlueShadow.Opacity = blueOpacity * ShadowOpacity;


      double rawFlux = GetPositiveSpectralFlux(fftData);
      double normalizedFlux = psychedelicFluxNormalizer.Update(rawFlux);
      double fluxSmoothing = normalizedFlux > visualFlux ? FluxAttack: FluxRelease;
      
      visualFlux += ( normalizedFlux - visualFlux) * fluxSmoothing;


      if (!Stablized)
      {
        StableImage.Opacity = StableBaseOpacity - visualFlux * StableFluxRevealStrength;
      }
      else
      {
        StableImage.Opacity = StableBaseOpacity;
      }


      if (Stablized)
      {
        return;
      }


      double bassOffset = bass * ShakeStrength;
      double fluxOffset = visualFlux *FluxSeparationStrength;
      double offset =(4.0 + bassOffset + fluxOffset) * MovementMultiplier;

      RedImageTranslate.X = offset;
      RedImageTranslate.Y = 0.0;
      GreenImageTranslate.X = 0.0;
      GreenImageTranslate.Y = -offset * 0.5;


      double shadowOffset = offset * ShadowDistanceMultiplier;

      RedShadowTranslate.X = -shadowOffset;
      RedShadowTranslate.Y = shadowOffset * 0.08;
      GreenShadowTranslate.X = 0.0;
      GreenShadowTranslate.Y = shadowOffset * 0.65;
      BlueShadowTranslate.X = shadowOffset;
      BlueShadowTranslate.Y = shadowOffset * 0.08;

      double scale = 1.0 + bass * ZoomStrength;
      
      BassImageScale.ScaleX = scale;
      BassImageScale.ScaleY =scale;

      UpdateConcertLight(bass, visualFlux);
    }

    #endregion

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

      scale.ScaleX = scaleX;
      scale.ScaleY = scaleY;

      imageBeam.Opacity = (0.55 + depth * 0.45) * intensity;
      weakGate.Opacity = intensity;
    }

    private static void BoostOpacityPair(ref double a, ref double b, double targetOpacity = 0.9)
    {
      double combined = 1.0 - (1.0 - a) * (1.0 - b);

      if (combined >= targetOpacity || combined <= 0.000001)
        return;

      double product = a * b;

      if (product <= 0.000001)
        return;

      double discriminant = Math.Max(0.0, 1.0 - 4.0 * product * targetOpacity);
      double scale = (1.0 - Math.Sqrt(discriminant)) / (2.0 * product);

      a = Math.Min(1.0, a * scale);
      b = Math.Min(1.0, b * scale);
    }



    #region GetPositiveSpectralFlux

    private double GetPositiveSpectralFlux(
      float[] fftData)
    {
      if (fftData == null ||
          fftData.Length < 4)
      {
        return 0.0;
      }


      int maxBin =
        fftData.Length / 2;


      if (psychedelicPreviousSpectrum == null ||
          psychedelicPreviousSpectrum.Length != fftData.Length)
      {
        psychedelicPreviousSpectrum =
          new float[fftData.Length];


        Array.Copy(
          fftData,
          psychedelicPreviousSpectrum,
          fftData.Length
        );


        return 0.0;
      }


      double positiveFlux = 0.0;
      double currentEnergy = 0.0;


      for (int i = 1; i < maxBin; i++)
      {
        float currentMagnitude =
          fftData[i];


        if (float.IsNaN(currentMagnitude) ||
            float.IsInfinity(currentMagnitude))
        {
          continue;
        }


        currentMagnitude =
          Math.Max(
            0f,
            currentMagnitude
          );


        float previousMagnitude =
          psychedelicPreviousSpectrum[i];


        previousMagnitude =
          Math.Max(
            0f,
            previousMagnitude
          );


        double current =
          Math.Sqrt(
            currentMagnitude
          );


        double previous =
          Math.Sqrt(
            previousMagnitude
          );


        double difference =
          current -
          previous;


        if (difference > 0.0)
        {
          positiveFlux +=
            difference;
        }


        currentEnergy +=
          current;


        psychedelicPreviousSpectrum[i] =
          currentMagnitude;
      }


      if (currentEnergy <= 0.0000001)
      {
        return 0.0;
      }


      return
        positiveFlux /
        currentEnergy;
    }

    #endregion

  }
  public class LightingSystem
  {
    private static readonly double[] LightMountX = { 0.15, 0.30, 0.70, 0.85 };
    private static readonly double[] FanPosition = { -1.5, -0.5, 0.5, 1.5 };

    public struct LightPose
    {
      public double Angle;
      public double Depth;

      public LightPose(double angle, double depth)
      {
        Angle = angle;
        Depth = depth;
      }
    }

    public static LightPose GetLightPose(int look, int lightIndex, double p, double aspect)
    {
      switch (look)
      {
        case 0:
          return GetFanPose(lightIndex, p);

        case 1:
          return GetCrossPose(lightIndex, p);

        case 2:
          return GetConvergencePose(lightIndex, p, aspect);

        default:
          return GetOrbitPose(lightIndex, p);
      }
    }

    private static LightPose GetFanPose(int lightIndex, double p)
    {
      double commonPan = Math.Sin(p * 0.72) * 16.0 + Math.Sin(p * 0.19 + 0.7) * 5.0;
      double spread = 6.0 + (0.5 + Math.Sin(p * 0.33) * 0.5) * 10.0;
      double angle = commonPan + FanPosition[lightIndex] * spread;
      double depth = 0.5 + Math.Sin(p * 0.47 + lightIndex * 0.35) * 0.5;

      return new LightPose(Clamp(angle, -55.0, 55.0), Clamp01(depth));
    }

    private static LightPose GetCrossPose(int lightIndex, double p)
    {
      double sweep = Math.Sin(p * 0.88) * 34.0;
      double drift = Math.Sin(p * 0.41 + 1.2) * 6.0;
      double angle;

      switch (lightIndex)
      {
        case 0:
          angle = sweep + drift;
          break;

        case 1:
          angle = -sweep * 0.65 + drift;
          break;

        case 2:
          angle = sweep * 0.65 + drift;
          break;

        default:
          angle = -sweep + drift;
          break;
      }

      double depth = 0.5 + Math.Cos(p * 0.72 + (lightIndex % 2) * Math.PI) * 0.5;

      return new LightPose(Clamp(angle, -55.0, 55.0), Clamp01(depth));
    }

    private static LightPose GetConvergencePose(int lightIndex, double p, double aspect)
    {
      double targetX = 0.50 + Math.Sin(p * 0.43) * 0.23 + Math.Sin(p * 0.17 + 1.1) * 0.06;
      double targetY = 0.57 + Math.Cos(p * 0.31) * 0.14 + Math.Sin(p * 0.73) * 0.03;

      targetX = Clamp(targetX, 0.20, 0.80);
      targetY = Clamp(targetY, 0.32, 0.86);

      double angle = GetMountedBeamAngle(LightMountX[lightIndex], targetX, targetY, aspect);
      double depth = 0.62 + Math.Sin(p * 0.52 + lightIndex * 0.18) * 0.12;

      return new LightPose(Clamp(angle, -55.0, 55.0), Clamp01(depth));
    }

    private static LightPose GetOrbitPose(int lightIndex, double p)
    {
      double phase = p * 0.92 + lightIndex * 0.78;
      double commonDrift = Math.Sin(p * 0.27) * 9.0;
      double angle = commonDrift + Math.Sin(phase) * 29.0;
      double depth = 0.5 + Math.Cos(phase) * 0.5;

      return new LightPose(Clamp(angle, -55.0, 55.0), Clamp01(depth));
    }

    public static double GetLookIntensity(int look, int lightIndex, double p)
    {
      switch (look)
      {
        case 0:
          {
            double breathing = 0.5 + Math.Sin(p * 0.70 + lightIndex * 0.15) * 0.5;

            return 0.82 + breathing * 0.18;
          }

        case 1:
          {
            double pairOffset = lightIndex < 2 ? 0.0 : Math.PI;
            double wave = 0.5 + Math.Sin(p * 1.15 + pairOffset) * 0.5;

            return Smooth01(Clamp01((wave - 0.18) / 0.32));
          }

        case 2:
          return 1.0;

        default:
          {
            double offset = lightIndex * Math.PI * 0.5;
            double wave = 0.5 + Math.Sin(p * 1.35 + offset) * 0.5;

            return Smooth01(Clamp01((wave - 0.20) / 0.25));
          }
      }
    }

    public static LightPose BlendPose(LightPose from, LightPose to, double t)
    {
      return new LightPose(Lerp(from.Angle, to.Angle, t), Lerp(from.Depth, to.Depth, t));
    }

    public static void SmoothLightIntensity(ref double current, double target, double deltaTime)
    {
      double response = target > current ? 10.0 : 5.0;
      double smoothing = 1.0 - Math.Exp(-response * deltaTime);

      current += (target - current) * smoothing;
    }

    public static double GetMountedBeamAngle(double mountX, double targetX, double targetY, double aspect)
    {
      double horizontalDistance = (targetX - mountX) * aspect;
      double verticalDistance = Math.Max(0.05, targetY);

      return Math.Atan2(horizontalDistance, verticalDistance) * 180.0 / Math.PI;
    }

    public static double PositiveModulo(double value, double modulus)
    {
      double result = value % modulus;

      return result < 0 ? result + modulus : result;
    }

    public static double Clamp01(double value)
    {
      return Math.Max(0.0, Math.Min(1.0, value));
    }

    public static double Clamp(double value, double minimum, double maximum)
    {
      return Math.Max(minimum, Math.Min(maximum, value));
    }

    public static double Smooth01(double value)
    {
      value = Clamp01(value);

      return value * value * (3.0 - 2.0 * value);
    }

    public static double Lerp(double from, double to, double t)
    {
      return from + (to - from) * t;
    }
  }
}



