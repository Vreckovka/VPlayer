using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CSCore;
using CSCore.DSP;
using CSCore.SoundIn;
using CSCore.Streams;
using SoundManagement;
using VCore.WPF.Helpers;
using WinformsVisualization.Visualization;
using Timer = System.Timers.Timer;
using WasapiLoopbackCapture = CSCore.SoundIn.WasapiLoopbackCapture;

namespace VPlayer.Core.SoundVizualization
{
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
    private static float bassEnergyPeak = InitialBassEnergyPeak;

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
      if (spectrumProvider.GetFftData(fftBuffer))
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

    private const float InitialBassEnergyPeak = 0.01f;

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
        bassEnergyPeak = Math.Max(bassEnergy, InitialBassEnergyPeak);
        detectorInitialized = true;
        return 0f;
      }

      float averageEnergy = Math.Max(bassEnergyAverage, 0.000001f);
      float averageFlux = Math.Max(bassFluxAverage, 0.000001f);
      bassEnergyPeak = Math.Max(bassEnergy, bassEnergyPeak * bassPeakDecay);
      float level = bassEnergy / Math.Max(bassEnergyPeak, 0.000001f);
      level = Clamp01((level - 0.10f) / 0.90f);

      float energyRatio = bassEnergy / averageEnergy;
      float energyRise = Clamp01((energyRatio - 0.95f) / 0.85f);
      float fluxRatio = bassFlux / averageFlux;
      float fluxRise = Clamp01((fluxRatio - 1.00f) / 2.50f);
      float transient = energyRise * 0.45f + fluxRise * 0.55f;
      float bass = level * 0.40f + level * transient * 0.60f;

      bassEnergyAverage = bassEnergyAverage * 0.97f + bassEnergy * 0.03f;
      bassFluxAverage = bassFluxAverage * 0.90f + bassFlux * 0.10f;

      bass *= bassSensitivity;
      bass = Clamp01(bass);

      if (bass <= bassOutputGate)
        return 0f;

      bass = (bass - bassOutputGate) / (1.0f - bassOutputGate);
      bass = Clamp01(bass);
      bass = MathF.Pow(bass, bassCurve);

      return Clamp01(bass);
    }

    #endregion

    #region GetPositiveSpectralFlux

    private static float[] psychedelicPreviousSpectrum;

    public static double GetPositiveSpectralFlux(float[] fftData)
    {
      if (fftData == null || fftData.Length < 4)
      {
        return 0.0;
      }

      int maxBin = fftData.Length / 2;

      if (psychedelicPreviousSpectrum == null ||
          psychedelicPreviousSpectrum.Length != fftData.Length)
      {
        psychedelicPreviousSpectrum = new float[fftData.Length];

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
        float currentMagnitude = fftData[i];

        if (float.IsNaN(currentMagnitude) || float.IsInfinity(currentMagnitude))
        {
          continue;
        }

        currentMagnitude = Math.Max(0f, currentMagnitude);

        var previousMagnitude = Math.Max(0f, psychedelicPreviousSpectrum[i]);

        double current = Math.Sqrt(currentMagnitude);
        double previous = Math.Sqrt(previousMagnitude);
        double difference = current - previous;

        if (difference > 0.0)
        {
          positiveFlux += difference;
        }

        currentEnergy += current;

        psychedelicPreviousSpectrum[i] = currentMagnitude;
      }

      if (currentEnergy <= 0.0000001)
      {
        return 0.0;
      }

      return positiveFlux / currentEnergy;
    }

    #endregion

    #region Clamp01

    public static float Clamp01(float value)
    {
      if (value < 0f) return 0f;
      if (value > 1f) return 1f;
      return value;
    }

    public static double Clamp01(double value)
    {
      if (value < 0f) return 0f;
      if (value > 1f) return 1f;
      return value;
    }

    #endregion
  }
}
