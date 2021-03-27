using System;
using System.Drawing;
using System.IO;
using System.Security.Permissions;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CSCore;
using CSCore.DSP;
using CSCore.SoundIn;
using CSCore.Streams;
using CSCore.Streams.Effects;
using WinformsVisualization.Visualization;
using Color = System.Windows.Media.Color;

namespace VPlayer.Player.UserControls
{
  /// <summary>
  /// Interaction logic for SoundVizualizer.xaml
  /// </summary>
  ///

  [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.ControlAppDomain)]
  public partial class SoundVizualizer : UserControl
  {
    #region Fields

    private LineSpectrum lineSpectrum;
    private int width;
    private int height;
    private ISampleSource source;
    private WasapiLoopbackCapture _soundIn;
    private SoundInSource soundInSource;
    private Timer timer;
    private bool isTimerDisposed = true;
    private byte[] buffer;
    private IWaveSource waveSource;
    private double normlizedDataMinValue;
    private double normlizedDataMaxValue;

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
      Unloaded += SoundVizualizer_Unloaded;

      Application.Current.Dispatcher.ShutdownStarted += Dispatcher_ShutdownStarted;

      RecreateSpectrumProvider();
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
        new PropertyMetadata(30.0, (x, y) =>
        {
          if (x is SoundVizualizer soundVizualizer)
          {
            var newValue = (double)y.NewValue;
           
            if (soundVizualizer.lineSpectrum != null)
            {
              soundVizualizer.lineSpectrum.NormlizedDataMaxValue = newValue;
            }

            soundVizualizer.normlizedDataMaxValue = newValue;
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
        new PropertyMetadata(0.0, (x, y) =>
        {
          if (x is SoundVizualizer soundVizualizer)
          {
            var newValue = (double)y.NewValue;

            if(soundVizualizer.lineSpectrum != null)
            {
              soundVizualizer.lineSpectrum.NormlizedDataMinValue = newValue;
            }

            soundVizualizer.normlizedDataMinValue = newValue;
          }
        }));


    #endregion

    #endregion

    #region Methods

    #region SoundVizualizer_Loaded

    private void SoundVizualizer_Loaded(object sender, RoutedEventArgs e)
    {
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
    }

    #endregion

    #region SoundVizualizer_IsEnabledChanged

    private void SoundVizualizer_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
      if (e.NewValue is bool isEnabled)
      {
        if (isEnabled && isTimerDisposed && IsLoaded)
        {
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

    private void ReadData(object s, DataAvailableEventArgs dataAvailableEventArgs)
    {
      int read;

      while (!isTimerDisposed && (read = waveSource.Read(buffer, 0, buffer.Length)) > 0) ;
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

    private void Dispatcher_ShutdownStarted(object sender, EventArgs e)
    {
      waveSource?.Dispose();
      source?.Dispose();
      _soundIn?.Dispose();

      if (soundInSource != null)
        soundInSource.DataAvailable -= ReadData;

      DisposeTimer();
    }

    #endregion

    #region SetupSampleSource

    private IWaveSource SetupSampleSource(ISampleSource aSampleSource)
    {
      const FftSize fftSize = FftSize.Fft4096;
      //create a spectrum provider which provides fft data based on some input
      var spectrumProvider = new BasicSpectrumProvider(aSampleSource.WaveFormat.Channels, aSampleSource.WaveFormat.SampleRate, fftSize);

      //linespectrum and voiceprint3dspectrum used for rendering some fft data
      //in oder to get some fft data, set the previously created spectrumprovider 
      lineSpectrum = new LineSpectrum(fftSize)
      {
        SpectrumProvider = spectrumProvider,
        UseAverage = true,
        BarCount = NumberOfColumns,
        BarSpacing = 2,
        IsXLogScale = true,
        ScalingStrategy = ScalingStrategy.Sqrt,
        MaximumFrequency = MaxFrequency,
        MinimumFrequency = 0
      };

      var notificationSource = new SingleBlockNotificationStream(aSampleSource);

      lineSpectrum.NormlizedDataMaxValue = normlizedDataMaxValue;
      lineSpectrum.NormlizedDataMinValue = normlizedDataMinValue;

      notificationSource.SingleBlockRead += (s, a) => spectrumProvider.Add(a.Left, a.Right);

      return notificationSource.ToWaveSource(16);

    }

    #endregion

    #region RecreateSpectrumProvider

    private void RecreateSpectrumProvider()
    {
      _soundIn = new WasapiLoopbackCapture();

      _soundIn.Initialize();

      soundInSource = new SoundInSource(_soundIn);
      source = soundInSource.ToSampleSource().AppendSource(x => new PitchShifter(x), out var _pitchShifter);

      waveSource = SetupSampleSource(source);

      buffer = new byte[waveSource.WaveFormat.BytesPerSecond / 2];

      _soundIn.Start();

      soundInSource.DataAvailable += ReadData;
    }

    #endregion

    #region timer2_Tick

    private void timer2_Tick(object sender, EventArgs e)
    {
      Application.Current?.Dispatcher?.Invoke(async () =>
      {
        if (IsEnabled)
        {
          var newImage = await Task.Run(() =>
          {
            return lineSpectrum.CreateSpectrumLine(new System.Drawing.Size(width, height),
              bottomColor,
              topColor,
              middleColor, true);
          });

          if (newImage == null)
          {
            var data = lineSpectrum.GetFttData();

            if (data == null)
            {
              RecreateSpectrumProvider();
            }
          }

          if (newImage != null)
          {
            await Task.Run(() =>
            {
              newImage.MakeTransparent();
            });

            var image = BitmapToImageSource(newImage);


            Image.Source = image;
          }
        }
      });
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

    #endregion
  }
}
