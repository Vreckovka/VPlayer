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

    private double previousLightBass;
    private double lightActivityEnergy;
    private double lightActivity;
    private double lightSpeed = 0.08;

    private double lightSweepPhase;
    private double lightSpinPhase;
    private double lastLightUpdateTime;

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

      double targetSpeed =
        LightMinSpeed +
        speedDrive * (LightMaxSpeed - LightMinSpeed);

      double speedSmoothing = 1.0 - Math.Exp(-LightSpeedSmoothing * deltaTime);

      lightSpeed += (targetSpeed - lightSpeed) * speedSmoothing;


      lightSweepPhase += Math.PI * 2.0 * lightSpeed * deltaTime;

      // Much slower phase for the entire rig's gentle drift.
      lightSpinPhase += (0.10 + lightSpeed * 0.10) * deltaTime;

      double p = lightSweepPhase;

      // Entire rig slowly leans left/right.
      double rigSwing = Math.Sin(lightSpinPhase) * 7.0;

      // Outer lights have wider movement.
      double outerSweep = Math.Sin(p) * 38.0;

      // Inner lights follow the same choreography with a small delay.
      double innerSweep = Math.Sin(p - 0.35) * 25.0;
      double outerDepth = 0.5 + Math.Sin(p * 0.5) * 0.5;
      double innerDepth = 0.5 + Math.Sin(p * 0.5 - 0.7) * 0.5;


      UpdateMountedLight(
        Light1Rotate,
       Light1Scale,
        Light1Beam,
        rigSwing + outerSweep,
        outerDepth
      );

      UpdateMountedLight(
        Light2Rotate,
        Light2Scale,
        Light2Beam,
        rigSwing + innerSweep,
        innerDepth
      );

      UpdateMountedLight(
        Light3Rotate,
        Light3Scale,
        Light3Beam,
        rigSwing - innerSweep,
        innerDepth
      );

      UpdateMountedLight(
        Light4Rotate,
        Light4Scale,
        Light4Beam,
        rigSwing - outerSweep,
        outerDepth
      );

      LightSweepOverlay.Opacity = Math.Min(
        0.30,
        0.08 + bass * 0.14 + flux * 0.05
      );

      Color beamColor = LerpColor(
        (Color)ColorConverter.ConvertFromString("#0f73ff"), 
        (Color)ColorConverter.ConvertFromString("#6f0fff"), bass);


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


    private void UpdateMountedLight(
      System.Windows.Media.RotateTransform rotate,
      System.Windows.Media.ScaleTransform scale,
      FrameworkElement beam,
      double angle,
      double depth)
    {
      rotate.Angle = angle;

      // Fake tilt toward / away from camera.
      double scaleY = 0.78 + depth * 0.28;

      // Tiny width change helps the 3D illusion.
      double scaleX = 0.92 + depth * 0.16;

      scale.ScaleX = scaleX;
     scale.ScaleY = scaleY;

      // Nearer beam is stronger.
      beam.Opacity = 0.55 + depth * 0.45;
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
  public sealed class AdaptiveSignalNormalizer
  {
    private bool initialized;

    private double average;
    private double peak;


    public double AverageAdaptation { get; set; } =
      0.03;

    public double PeakDecay { get; set; } =
      0.997;


    public double Update(double value)
    {
      if (double.IsNaN(value) ||
          double.IsInfinity(value))
      {
        return 0.0;
      }


      value =
        Math.Max(
          0.0,
          value
        );


      if (!initialized)
      {
        average =
          Math.Max(
            value,
            0.0000001
          );


        peak =
          Math.Max(
            value,
            0.0000001
          );


        initialized = true;

        return 0.0;
      }


      peak =
        Math.Max(
          value,
          peak * PeakDecay
        );


      double peakLevel =
        value /
        Math.Max(
          peak,
          0.0000001
        );


      peakLevel =
        Clamp01(
          peakLevel
        );


      double ratio =
        value /
        Math.Max(
          average,
          0.0000001
        );


      double relativeRise =
        Clamp01(
          (ratio - 1.0) /
          1.5
        );


      double result =
        peakLevel * 0.70 +
        relativeRise * 0.30;


      average =
        average *
        (1.0 - AverageAdaptation) +
        value *
        AverageAdaptation;


      return
        Clamp01(
          result
        );
    }


    public void Reset()
    {
      initialized = false;

      average = 0.0;
      peak = 0.0;
    }


    private static double Clamp01(
      double value)
    {
      return Math.Max(
        0.0,
        Math.Min(
          1.0,
          value
        )
      );
    }
  }
}
