﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using CSCore;
using CSCore.CoreAudioAPI;
using CSCore.DSP;
using CSCore.SoundIn;
using CSCore.Streams;
using CSCore.Streams.Effects;
using WinformsVisualization.Visualization;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using Size = System.Windows.Size;

namespace VPlayer.Player.UserControls
{
  /// <summary>
  /// Interaction logic for SoundVizualizer.xaml
  /// </summary>
  public partial class SoundVizualizer : UserControl
  {
    #region Fields

    private LineSpectrum lineSpectrum;
    private IWaveSource waveSource;
    private int width;
    private int height;

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

    #endregion

    #region Methods

    #region SoundVizualizer_Loaded

    private void SoundVizualizer_Loaded(object sender, RoutedEventArgs e)
    {
      InitlizeSoundVizualizer();
    }

    #endregion

    #region SoundVizualizer_IsEnabledChanged

    private void SoundVizualizer_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
      if (e.NewValue is bool newValue && newValue)
      {
        InitlizeSoundVizualizer();
      }
      else
      {
        DisposeSoundVizualizer();
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

    #region InitlizeSoundVizualizer

    private Timer timer;
    private ISampleSource source;
    private WasapiLoopbackCapture _soundIn;


    private void InitlizeSoundVizualizer()
    {
      timer = new Timer(40);

      //open the default device
      _soundIn = new WasapiLoopbackCapture();

      //Our loopback capture opens the default render device by default so the following is not needed
      //_soundIn.Device = MMDeviceEnumerator.DefaultAudioEndpoint(DataFlow.Render, Role.Console);

      _soundIn.Initialize();

      var soundInSource = new SoundInSource(_soundIn);
      source = soundInSource.ToSampleSource().AppendSource(x => new PitchShifter(x), out var _pitchShifter);

      SetupSampleSource(source);

      // We need to read from our source otherwise SingleBlockRead is never called and our spectrum provider is not populated
      byte[] buffer = new byte[waveSource.WaveFormat.BytesPerSecond / 2];
      soundInSource.DataAvailable += (s, aEvent) =>
      {
        int read;
        try
        {
          while ((read = waveSource.Read(buffer, 0, buffer.Length)) > 0) ;
        }
        catch (Exception ex)
        {
        };
      };

      //play the audio
      _soundIn.Start();

      timer.Start();

      timer.Elapsed += timer2_Tick;

      Application.Current.Dispatcher.ShutdownStarted += Dispatcher_ShutdownStarted;
    }


    #endregion

    #region DisposeSoundVizualizer

    private void DisposeSoundVizualizer()
    {
      _soundIn?.Dispose();
      source?.Dispose();

      if (timer != null)
      {
        timer.Elapsed -= timer2_Tick;
        timer?.Dispose();
      }

      Application.Current.Dispatcher.ShutdownStarted -= Dispatcher_ShutdownStarted;
    }

    #endregion

    #region Dispatcher_ShutdownStarted

    private void Dispatcher_ShutdownStarted(object sender, EventArgs e)
    {
      DisposeSoundVizualizer();
    }

    #endregion

    #region SetupSampleSource

    private void SetupSampleSource(ISampleSource aSampleSource)
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

      notificationSource.SingleBlockRead += (s, a) => spectrumProvider.Add(a.Left, a.Right);

      waveSource = notificationSource.ToWaveSource(16);

    }

    #endregion

    #region timer2_Tick

    private void timer2_Tick(object sender, EventArgs e)
    {
      var newImage = lineSpectrum.CreateSpectrumLine(new System.Drawing.Size(width, height),
        bottomColor,
        topColor,
        middleColor, true);
      if (newImage != null)
      {
        newImage.MakeTransparent();

        Application.Current?.Dispatcher?.Invoke(() => Image.Source = BitmapToImageSource(newImage)
      );
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

    #endregion
  }
}