using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CSCore;
using CSCore.DSP;
using CSCore.SoundIn;
using CSCore.Streams;
using Ninject.Activation;
using SoundManagement;
using VCore.WPF;
using VCore.WPF.Controls;
using VCore.WPF.Helpers;
using Windows.UI.Xaml.Media;
using WinformsVisualization.Visualization;
using Color = System.Windows.Media.Color;
using Timer = System.Timers.Timer;
using WasapiLoopbackCapture = CSCore.SoundIn.WasapiLoopbackCapture;

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

    private void SpektrumAnalyzer_OnFFtTick(object sender, float[] e)
    {
     
      VSynchronizationContext.PostOnUIThread(async () =>
      {
        if (!IsEnabled) return;
        if (Visibility != Visibility.Visible) return;

        float bass = SpektrumAnalyzer.AnalyzeBass(e, BassSensitivity, BassOutputGate, BassPeakDecay, BassCurve);

        float smoothing = bass > visualBass ? 0.50f : 0.14f;

        visualBass += (bass - visualBass) * smoothing;


        ApplyPsychedelicImageEffect(visualBass);
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

    #region ApplyPsychedelicImageEffect

    private void ApplyPsychedelicImageEffect(float bass)
    {
      bass = SpektrumAnalyzer.Clamp(bass);

      double redOpacity;
      double greenOpacity;
      double blueOpacity;
      if (bass <= 0.5f)
      {
        float t = bass / 0.5f;
        blueOpacity = 1.0 - t;
        greenOpacity = t;
        redOpacity = 0.0;
      }
      else
      {
        float t = (bass - 0.5f) / 0.5f;
        blueOpacity = 0.0;
        greenOpacity = 1.0 - t;
        redOpacity = t;
      }

      RedImage.Opacity = redOpacity * OpacityModifier;
      GreenImage.Opacity = greenOpacity * OpacityModifier;
      BlueImage.Opacity = blueOpacity * OpacityModifier;

      if (!Stablized)
      {
        double offset = 4.0 + bass * ShakeStrength;
        RedImage.Margin = new Thickness(offset, 0, -offset, 0);
        GreenImage.Margin = new Thickness(0, -offset * 0.5, 0, offset * 0.5);
        BlueImage.Margin = new Thickness(-offset, 0, offset, 0);

        double scale = 1.0 + bass * ZoomStrength;
        BassImageScale.ScaleX = scale;
        BassImageScale.ScaleY = scale;
      }
    }

    #endregion
  }

  public static class SpektrumAnalyzer
  {
    public const FftSize fftSize = FftSize.Fft8192;
    public static BasicSpectrumProvider spectrumProvider;

    private static ISampleSource source;
    private static WasapiLoopbackCapture soundIn;
    private static SoundInSource soundInSource;
    private static IWaveSource waveSource;
   

    private static byte[] readBuffer;
    private static bool soundSourceInitialized;
    private static readonly SemaphoreSlim initializeSemaphore = new SemaphoreSlim(1, 1);
    private static readonly SemaphoreSlim recreateSemaphore = new SemaphoreSlim(1, 1);
    private static readonly object disposeLock = new object();
    private static bool disposedFromShutdown;
    private static readonly float[] fftBuffer = new float[(int)fftSize];
    private static readonly float[] previousSpectrum = new float[(int)fftSize];
    private static Timer timer;

    private const int BassLowHz = 35;
    private const int BassHighHz = 140;

    private static bool detectorInitialized;
    private static float bassEnergyAverage;
    private static float bassFluxAverage;
    private static float bassEnergyPeak = 0.000001f;

    public static event EventHandler<float[]> OnFFtTick;

    static SpektrumAnalyzer()
    {
      InitializeSoundSource();

      Application.Current.Dispatcher.ShutdownStarted += Dispatcher_ShutdownStarted;
    }

    public static void Start()
    {
      StartTimer();
    }

    public static void Stop()
    {
      StopTimer();
    }

    #region RecreateSpectrumProvider

    private static async Task RecreateSpectrumProvider()
    {
      await recreateSemaphore.WaitAsync();

      try
      {
        await Task.Run(() =>
        {
          DisposeAudioSource();

          soundIn = new WasapiLoopbackCapture();
          soundIn.Initialize();
          soundInSource = new SoundInSource(soundIn);
          source = soundInSource.ToSampleSource();

          waveSource = SetupSampleSource(source);

          readBuffer = new byte[waveSource.WaveFormat.BytesPerSecond / 2];

          soundInSource.DataAvailable += ReadData; soundIn.Start();
        }
        );
      }
      catch (Exception ex)
      {
        Debug.WriteLine(ex);
      }
      finally
      {
        recreateSemaphore.Release();
      }
    }

    #endregion

    private static void Dispatcher_ShutdownStarted(object sender, EventArgs e)
    {
      lock (disposeLock)
      {
        if (disposedFromShutdown) return;
        disposedFromShutdown = true;
        StopTimer();
        DisposeAudioSource();
      }
    }

    private static void DisposeAudioSource()
    {
      if (soundInSource != null)
      {
        soundInSource.DataAvailable -= ReadData;
      }
      try
      {
        soundIn?.Stop();
      }
      catch (Exception ex)
      {
        Debug.WriteLine(ex);
      }

      waveSource?.Dispose();
      source?.Dispose();
      soundInSource?.Dispose();
      soundIn?.Dispose();
      waveSource = null;
      source = null;
      soundInSource = null;
      soundIn = null;
      readBuffer = null;
      spectrumProvider = null;
    }

    private static void StartTimer()
    {
      if (timer == null)
      {
        timer = new Timer(40);
        timer.Elapsed += OnTimerTick;
      }
      timer.Start();
    }

    private static void StopTimer()
    {
      if (timer == null) return;
      timer.Stop();
      timer.Elapsed -= OnTimerTick;
      timer.Dispose();
      timer = null;
    }

    public static void OnTimerTick(object sender, System.Timers.ElapsedEventArgs e)
    {
      if (spectrumProvider.GetFftData(fftBuffer, spectrumProvider))
      {
        OnFFtTick.Invoke(spectrumProvider, fftBuffer);
      }
    }

    #region ReadData

    private static void ReadData(object sender, DataAvailableEventArgs e)
    {
      if (waveSource == null || readBuffer == null)
      {
        return;
      }
      try
      {
        while (waveSource.Read(readBuffer, 0, readBuffer.Length) > 0)
        {
        }
      }
      catch (ObjectDisposedException)
      {
      }
      catch (Exception ex)
      {
        Debug.WriteLine(ex);
      }
    }

    #endregion

    #region SetupSampleSource

    private static IWaveSource SetupSampleSource(ISampleSource sampleSource)
    {
      spectrumProvider = new BasicSpectrumProvider(sampleSource.WaveFormat.Channels, sampleSource.WaveFormat.SampleRate, fftSize);

      var notificationSource = new SingleBlockNotificationStream(sampleSource);
      notificationSource.SingleBlockRead += (sender, args) =>
      {
        spectrumProvider.Add(args.Left, args.Right);
      }
      ;
      return notificationSource.ToWaveSource(16);
    }

    #endregion

    #region InitializeSoundSource

    private static async Task InitializeSoundSource()
    {
      await initializeSemaphore.WaitAsync();

      try
      {
        if (soundSourceInitialized) return;

        await RecreateSpectrumProvider();

        soundSourceInitialized = true;

        AudioDeviceManager.Instance.ObservePropertyChange(x => x.SelectedSoundDevice).Subscribe(async _ =>
        {
          try
          {
            await RecreateSpectrumProvider();
          }
          catch (Exception ex)
          {
            Debug.WriteLine(ex);
          }
        }
        );
      }
      catch (Exception ex)
      {
        Debug.WriteLine(ex);
      }
      finally
      {
        initializeSemaphore.Release();
      }
    }

    #endregion

    #region AnalyzeBass

    public static float AnalyzeBass(
      float[] spectrum,
      float bassSensitivity = 1,
      float bassOutputGate = 0.06f,
      float bassPeakDecay = 0.997f,
      float bassCurve = 0.20f)
    {
      int lowBin = spectrumProvider.GetFftBandIndex(BassLowHz);
      int highBin = spectrumProvider.GetFftBandIndex(BassHighHz);
      int maxBin = spectrum.Length / 2 - 1;
      lowBin = Math.Max(1, Math.Min(lowBin, maxBin));
      highBin = Math.Max(lowBin, Math.Min(highBin, maxBin));
      double energySquaredSum = 0.0;
      double positiveFluxSum = 0.0;
      int binCount = 0;

      for (int i = lowBin; i <= highBin; i++)
      {
        float magnitude = spectrum[i];
        if (float.IsNaN(magnitude) || float.IsInfinity(magnitude))
        {
          continue;
        }
        magnitude = Math.Max(0f, magnitude);
        energySquaredSum += magnitude * magnitude;
        float delta = magnitude - previousSpectrum[i];
        if (delta > 0f)
        {
          positiveFluxSum += delta;
        }
        previousSpectrum[i] = magnitude;
        binCount++;
      }

      if (binCount == 0) return 0f;

      float bassEnergy = (float)Math.Sqrt(energySquaredSum / binCount);
      float bassFlux = (float)(positiveFluxSum / binCount);

      if (!detectorInitialized)
      {
        bassEnergyAverage = Math.Max(bassEnergy, 0.000001f);
        bassFluxAverage = Math.Max(bassFlux, 0.000001f);
        bassEnergyPeak = Math.Max(bassEnergy, 0.000001f);
        detectorInitialized = true;
        return 0f;
      }

      float averageEnergy = Math.Max(bassEnergyAverage, 0.000001f);
      float averageFlux = Math.Max(bassFluxAverage, 0.000001f);
      bassEnergyPeak = Math.Max(bassEnergy, bassEnergyPeak * bassPeakDecay);
      float level = bassEnergy / Math.Max(bassEnergyPeak, 0.000001f);
      level = Clamp((level - 0.10f) / 0.90f);

      float energyRatio = bassEnergy / averageEnergy;
      float energyRise = Clamp((energyRatio - 0.95f) / 0.85f);
      float fluxRatio = bassFlux / averageFlux;
      float fluxRise = Clamp((fluxRatio - 1.00f) / 2.50f);
      float transient = energyRise * 0.45f + fluxRise * 0.55f;
      float bass = level * 0.40f + level * transient * 0.60f;

      bassEnergyAverage = bassEnergyAverage * 0.97f + bassEnergy * 0.03f;
      bassFluxAverage = bassFluxAverage * 0.90f + bassFlux * 0.10f;

      bass *= bassSensitivity;
      bass = Clamp(bass);

      if (bass <= bassOutputGate)
        return 0f;

      bass = (bass - bassOutputGate) / (1.0f - bassOutputGate);
      bass = Clamp(bass);
      bass = MathF.Pow(bass, bassCurve);

      return Clamp(bass);
    }

    #endregion

    #region Clamp

    public static float Clamp(float value)
    {
      if (value < 0f) return 0f;
      if (value > 1f) return 1f;
      return value;
    }

    #endregion
  }
}
