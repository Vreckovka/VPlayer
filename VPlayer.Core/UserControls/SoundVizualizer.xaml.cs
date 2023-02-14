using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Security.Permissions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CSCore;
using CSCore.CoreAudioAPI;
using CSCore.DSP;
using CSCore.SoundIn;
using CSCore.Streams;
using CSCore.Streams.Effects;
using CSCore.Win32;
using SoundManagement;
using VCore.WPF.Helpers;
using WinformsVisualization.Visualization;
using Color = System.Windows.Media.Color;
using Timer = System.Timers.Timer;

namespace VPlayer.Player.UserControls
{
  /// <summary>
  /// Interaction logic for SoundVizualizer.xaml
  /// </summary>
  ///

  public partial class SoundVizualizer : UserControl
  {
    #region Fields

    private LineSpectrum lineSpectrum;
    private int width;
    private int height;


    private static ISampleSource source;
    private static WasapiLoopbackCapture _soundIn;
    private static SoundInSource soundInSource;
    private static byte[] buffer;
    private static string registredOutputDevice;
    private static List<SoundVizualizer> soundVizualizers = new List<SoundVizualizer>();
    private static bool wasSoundSourceInitilized = false;
    private static BasicSpectrumProvider basicSpectrumProvider;
    private const FftSize fftSize = FftSize.Fft8192;

    private Timer timer;
    private bool isTimerDisposed = true;

    private static IWaveSource waveSource;


    System.Drawing.Color bottomColor = System.Drawing.Color.Green;
    System.Drawing.Color topColor = System.Drawing.Color.Red;
    System.Drawing.Color middleColor = System.Drawing.Color.Black;

    #endregion

    #region Constructors

    public SoundVizualizer()
    {
      InitializeComponent();

      this.Loaded += SoundVizualizer_Loaded;
      this.SizeChanged += SoundVizualizer_SizeChanged;
      this.IsEnabledChanged += SoundVizualizer_IsEnabledChanged;
      this.DataContextChanged += SoundVizualizer_DataContextChanged;
      Unloaded += SoundVizualizer_Unloaded;

      Application.Current.Dispatcher.ShutdownStarted += Dispatcher_ShutdownStarted;

      soundVizualizers.Add(this);

      InitilizeSoundSource();

      if (soundInSource != null)
        AssignSpectrum();
    }

    private async void SoundVizualizer_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
      if (IsEnabled && isTimerDisposed && IsLoaded)
      {
        AssignSpectrum();

        InitlizeTimer();
      }
     
    }

    private void SoundVizualizer_Unloaded(object sender, RoutedEventArgs e)
    {
      timer?.Stop();
    }


    #endregion

    #region Properties

    #region NumberOfColumns

    public int NumberOfColumns
    {
      get { return (int)GetValue(NumberOfColumnsProperty); }
      set { SetValue(NumberOfColumnsProperty, value); }
    }

    public static readonly DependencyProperty NumberOfColumnsProperty =
      DependencyProperty.Register(
        nameof(NumberOfColumns),
        typeof(int),
        typeof(SoundVizualizer),
        new PropertyMetadata(16, (x, y) =>
        {
          if (x is SoundVizualizer audioVizualizer)
          {
            if (y.NewValue is int number)
            {
              if (audioVizualizer.lineSpectrum != null)
              {
                audioVizualizer.lineSpectrum.BarCount = number;
              }
            }
          }
        }));


    #endregion

    #region TopColor

    public Color TopColor
    {
      get { return (Color)GetValue(TopColorProperty); }
      set { SetValue(TopColorProperty, value); }
    }

    public static readonly DependencyProperty TopColorProperty =
      DependencyProperty.Register(
        nameof(TopColor),
        typeof(Color),
        typeof(SoundVizualizer),
        new PropertyMetadata(Colors.Black, (x, y) =>
        {
          if (x is SoundVizualizer soundVizualizer)
          {
            var windowsCOlor = (Color)y.NewValue;
            soundVizualizer.topColor = System.Drawing.Color.FromArgb(windowsCOlor.A, windowsCOlor.R, windowsCOlor.G, windowsCOlor.B);
          }
        }));


    #endregion

    #region BottomColor

    public Color BottomColor
    {
      get { return (Color)GetValue(BottomColorProperty); }
      set { SetValue(BottomColorProperty, value); }
    }

    public static readonly DependencyProperty BottomColorProperty =
      DependencyProperty.Register(
        nameof(BottomColor),
        typeof(Color),
        typeof(SoundVizualizer),
        new PropertyMetadata(Colors.Black, (x, y) =>
        {
          if (x is SoundVizualizer soundVizualizer)
          {
            var windowsCOlor = (Color)y.NewValue;
            soundVizualizer.bottomColor = System.Drawing.Color.FromArgb(windowsCOlor.A, windowsCOlor.R, windowsCOlor.G, windowsCOlor.B);
          }
        }));


    #endregion

    #region MiddleColor

    public Color MiddleColor
    {
      get { return (Color)GetValue(MiddleColorProperty); }
      set { SetValue(MiddleColorProperty, value); }
    }

    public static readonly DependencyProperty MiddleColorProperty =
      DependencyProperty.Register(
        nameof(MiddleColor),
        typeof(Color),
        typeof(SoundVizualizer),
        new PropertyMetadata(Colors.Black, (x, y) =>
        {
          if (x is SoundVizualizer soundVizualizer)
          {
            var windowsCOlor = (Color)y.NewValue;
            soundVizualizer.middleColor = System.Drawing.Color.FromArgb(windowsCOlor.A, windowsCOlor.R, windowsCOlor.G, windowsCOlor.B);
          }
        }));


    #endregion

    #region MinimumBarWidth

    public double? MinimumBarWidth
    {
      get { return (double?)GetValue(MinimumBarWidthProperty); }
      set { SetValue(MinimumBarWidthProperty, value); }
    }

    public static readonly DependencyProperty MinimumBarWidthProperty =
      DependencyProperty.Register(
        nameof(MinimumBarWidth),
        typeof(double?),
        typeof(SoundVizualizer),
        new PropertyMetadata(null, (x, y) =>
        {
          if (x is SoundVizualizer soundVizualizer)
          {
            var barWidth = (double)y.NewValue;

            if (soundVizualizer?.lineSpectrum != null)
              soundVizualizer.lineSpectrum.MinimumBarWidth = barWidth;
          }
        }));

    #endregion

    #region UseAutomaticBarCountCalculation

    public bool UseAutomaticBarCountCalculation
    {
      get { return (bool)GetValue(UseAutomaticBarCountCalculationProperty); }
      set { SetValue(UseAutomaticBarCountCalculationProperty, value); }
    }

    public static readonly DependencyProperty UseAutomaticBarCountCalculationProperty =
      DependencyProperty.Register(
        nameof(UseAutomaticBarCountCalculation),
        typeof(bool),
        typeof(SoundVizualizer),
        new PropertyMetadata(false, (x, y) =>
        {
          if (x is SoundVizualizer soundVizualizer)
          {
            var use = (bool)y.NewValue;
            soundVizualizer.lineSpectrum.AutomaticBarCountCalculation = use;
          }
        }));


    #endregion

    #region MaxFrequency

    public int MaxFrequency
    {
      get { return (int)GetValue(MaxFrequencyProperty); }
      set { SetValue(MaxFrequencyProperty, value); }
    }

    public static readonly DependencyProperty MaxFrequencyProperty =
      DependencyProperty.Register(
        nameof(MaxFrequencyProperty),
        typeof(int),
        typeof(SoundVizualizer),
        new PropertyMetadata(20000, (x, y) =>
        {
          if (x is SoundVizualizer soundVizualizer && y.NewValue is int number && soundVizualizer.lineSpectrum != null)
          {
            soundVizualizer.lineSpectrum.MaximumFrequency = number;
          }
        }));


    #endregion

    #region NormlizedDataMaxValue

    public double NormlizedDataMaxValue
    {
      get { return (double)GetValue(NormlizedDataMaxValueProperty); }
      set { SetValue(NormlizedDataMaxValueProperty, value); }
    }

    public static readonly DependencyProperty NormlizedDataMaxValueProperty =
      DependencyProperty.Register(
        nameof(NormlizedDataMaxValue),
        typeof(double),
        typeof(SoundVizualizer),
        new PropertyMetadata(20.0, (x, y) =>
        {
          if (x is SoundVizualizer soundVizualizer)
          {
            var newValue = (double)y.NewValue;

            if (soundVizualizer.lineSpectrum != null)
            {
              soundVizualizer.lineSpectrum.NormlizedDataMaxValue = newValue;
            }
          }
        }));


    #endregion

    #region NormlizedDataMinValue

    public double NormlizedDataMinValue
    {
      get { return (double)GetValue(NormlizedDataMinValueProperty); }
      set { SetValue(NormlizedDataMinValueProperty, value); }
    }

    public static readonly DependencyProperty NormlizedDataMinValueProperty =
      DependencyProperty.Register(
        nameof(NormlizedDataMinValue),
        typeof(double),
        typeof(SoundVizualizer),
        new PropertyMetadata(3.0, (x, y) =>
        {
          if (x is SoundVizualizer soundVizualizer)
          {
            var newValue = (double)y.NewValue;

            if (soundVizualizer.lineSpectrum != null)
            {
              soundVizualizer.lineSpectrum.NormlizedDataMinValue = newValue;
            }
          }
        }));


    #endregion

    #region NormlizedDataMaxSilentValue

    public double NormlizedDataMaxSilentValue
    {
      get { return (double)GetValue(NormlizedDataMaxSilentValueProperty); }
      set { SetValue(NormlizedDataMaxSilentValueProperty, value); }
    }

    public static readonly DependencyProperty NormlizedDataMaxSilentValueProperty =
      DependencyProperty.Register(
        nameof(NormlizedDataMaxSilentValue),
        typeof(double),
        typeof(SoundVizualizer),
        new PropertyMetadata(5.0, (x, y) =>
        {
          if (x is SoundVizualizer soundVizualizer)
          {
            var newValue = (double)y.NewValue;

            if (soundVizualizer.lineSpectrum != null)
            {
              soundVizualizer.lineSpectrum.NormlizedDataMaxSilentValue = newValue;
            }
          }
        }));


    #endregion

    #region UseSkew

    public bool UseSkew
    {
      get { return (bool)GetValue(UseSkewProperty); }
      set { SetValue(UseSkewProperty, value); }
    }

    public static readonly DependencyProperty UseSkewProperty =
      DependencyProperty.Register(
        nameof(UseSkew),
        typeof(bool),
        typeof(SoundVizualizer),
        new PropertyMetadata(false, (x, y) =>
        {
          if (x is SoundVizualizer soundVizualizer)
          {
            var newValue = (bool)y.NewValue;

            if (soundVizualizer.lineSpectrum != null)
            {
              soundVizualizer.lineSpectrum.UseSkew = newValue;
            }
          }
        }));


    #endregion

    #endregion

    #region Methods

    #region InitilizeSoundSource

    private static SemaphoreSlim initilizeSempahore = new SemaphoreSlim(1, 1);
    private static async void InitilizeSoundSource()
    {
      try
      {
        await initilizeSempahore.WaitAsync();

        if (!wasSoundSourceInitilized)
        {
          await RecreateSpectrumProvider();

          wasSoundSourceInitilized = true;

          AudioDeviceManager.Instance.ObservePropertyChange(x => x.SelectedSoundDevice).Subscribe(async x =>
          {
            await RecreateSpectrumProvider();

            foreach (var soundVizualizer in soundVizualizers)
            {
              soundVizualizer.AssignSpectrum();
            }
          });
        }
      }
      finally
      {
        initilizeSempahore.Release();
      }
    }

    #endregion

    #region SoundVizualizer_Loaded

    private void SoundVizualizer_Loaded(object sender, RoutedEventArgs e)
    {
      if (soundInSource != null && lineSpectrum == null)
        AssignSpectrum();


      if (isTimerDisposed && IsEnabled)
      {
        if (timer == null)
        {
          InitlizeTimer();
          return;
        }
      }

      if (!isTimerDisposed && IsEnabled)
      {
        timer.Start();
      }


      if (IsEnabled && isTimerDisposed && IsLoaded)
      {
        AssignSpectrum();

        InitlizeTimer();
      }
      else if (!isTimerDisposed && !IsEnabled)
      {
        DisposeTimer();
      }
    }

    #endregion

    #region SoundVizualizer_IsEnabledChanged

    private void SoundVizualizer_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
      if (e.NewValue is bool isEnabled)
      {
        if (isEnabled && isTimerDisposed && IsLoaded)
        {
          AssignSpectrum();

          InitlizeTimer();
        }
        else if (!isTimerDisposed && !isEnabled)
        {
          DisposeTimer();
        }
      }

    }

    #endregion

    #region SoundVizualizer_SizeChanged

    private void SoundVizualizer_SizeChanged(object sender, SizeChangedEventArgs e)
    {
      width = (int)e.NewSize.Width;
      height = (int)e.NewSize.Height;
    }

    #endregion

    #region InitlizeTimer

    private void InitlizeTimer()
    {
      isTimerDisposed = false;

      timer = new Timer(40);

      timer.Start();

      timer.Elapsed += timer2_Tick;
    }


    #endregion

    #region ReadData

    private static void ReadData(object s, DataAvailableEventArgs dataAvailableEventArgs)
    {
      int read;

      while ((read = waveSource.Read(buffer, 0, buffer.Length)) > 0) ;
    }

    #endregion

    #region SetupSampleSource

    private static IWaveSource SetupSampleSource(ISampleSource aSampleSource)
    {
      basicSpectrumProvider = new BasicSpectrumProvider(aSampleSource.WaveFormat.Channels, aSampleSource.WaveFormat.SampleRate, fftSize);

      var notificationSource = new SingleBlockNotificationStream(aSampleSource);

      notificationSource.SingleBlockRead += (s, a) => basicSpectrumProvider.Add(a.Left, a.Right);

      return notificationSource.ToWaveSource(16);

    }

    #endregion

    #region AssignSpectrum

    private void AssignSpectrum()
    {
      lineSpectrum = new LineSpectrum(fftSize)
      {
        SpectrumProvider = basicSpectrumProvider,
        UseAverage = true,
        BarCount = NumberOfColumns,
        BarSpacing = 2,
        IsXLogScale = true,
        ScalingStrategy = ScalingStrategy.Sqrt,
        MaximumFrequency = MaxFrequency,
        MinimumFrequency = 0,
        MinimumBarWidth = MinimumBarWidth,
        UseSkew = UseSkew,
        NormlizedDataMaxValue = NormlizedDataMaxValue,
        NormlizedDataMinValue = NormlizedDataMinValue,
        NormlizedDataMaxSilentValue = NormlizedDataMaxSilentValue
      };
    }

    #endregion

    #region RecreateSpectrumProvider

    private static SemaphoreSlim reacreateBatton = new SemaphoreSlim(1, 1);

    private static async Task RecreateSpectrumProvider()
    {
      try
      {
        await reacreateBatton.WaitAsync();

        await Task.Run(() =>
        {
          DisposeEqualizer();

          _soundIn = new WasapiLoopbackCapture();
          _soundIn.Initialize();

          soundInSource = new SoundInSource(_soundIn);

          source = soundInSource.ToSampleSource().AppendSource(x => new PitchShifter(x), out var _pitchShifter);

          var _dummyCapture = new WasapiCapture(true, AudioClientShareMode.Shared, 250);

          waveSource = SetupSampleSource(source);

          buffer = new byte[waveSource.WaveFormat.BytesPerSecond / 2];

          _soundIn.Start();

          soundInSource.DataAvailable += ReadData;
        });
      }
      finally
      {
        reacreateBatton.Release();
      }
    }

    #endregion

    #region DisposeEqualizer

    private static void DisposeEqualizer()
    {
      if (soundInSource != null)
      {
        soundInSource.DataAvailable -= ReadData;
      }

      soundInSource?.Dispose();
      soundInSource = null;

      source?.Dispose();
      source = null;

      waveSource?.Dispose();
      waveSource = null;

      _soundIn?.Stop();
      _soundIn?.Dispose();
      _soundIn = null;

    }

    #endregion

    #region timer2_Tick

    private void timer2_Tick(object sender, EventArgs e)
    {
      try
      {
        Application.Current?.Dispatcher?.Invoke(async () =>
         {
           if (IsEnabled && lineSpectrum != null)
           {
             var newImage = await Task.Run(() =>
             {
               return lineSpectrum.CreateSpectrumLine(new System.Drawing.Size(width, height),
                 bottomColor,
                 topColor,
                 middleColor, true);
             });


             if (newImage != null)
             {
               await Task.Run(() =>
               {
                 newImage.MakeTransparent();
               });
              
               var image = BitmapToImageSource(newImage);

               Image.Source = image;
               newImage.Dispose();
             }
           }
         });
      }
      catch (Exception ex)
      {
      }
    }


    #endregion

    #region BitmapToImageSource

    BitmapImage BitmapToImageSource(Bitmap bitmap)
    {
      using (MemoryStream memory = new MemoryStream())
      {
        bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
        memory.Position = 0;
        BitmapImage bitmapimage = new BitmapImage();
        bitmapimage.BeginInit();
        bitmapimage.StreamSource = memory;
        bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
        bitmapimage.EndInit();

        return bitmapimage;
      }
    }

    #endregion

    #region DisposeTimer

    private void DisposeTimer()
    {
      isTimerDisposed = true;

      if (timer != null)
      {
        timer.Elapsed -= timer2_Tick;
        timer?.Dispose();
      }

    }

    #endregion

    #region Dispatcher_ShutdownStarted

    private static object disposeBatton = new object();
    private static bool disposedFromShutDown = false;
    private void Dispatcher_ShutdownStarted(object sender, EventArgs e)
    {
      lock (disposeBatton)
      {
        if (!disposedFromShutDown)
        {
          disposedFromShutDown = true;
          if (soundInSource != null)
            soundInSource.DataAvailable -= ReadData;

          DisposeTimer();

          DisposeEqualizer();
        }
      }
    }

    #endregion

    #endregion
  }
}
