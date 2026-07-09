using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
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
using VCore.WPF;
using VCore.WPF.Helpers;
using VPlayer.Core.SoundVizualization;
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

    System.Drawing.Color bottomColor = System.Drawing.Color.Green;
    System.Drawing.Color topColor = System.Drawing.Color.Red;
    System.Drawing.Color middleColor = System.Drawing.Color.Black;

    #endregion

    #region Constructors

    public SoundVizualizer()
    {
      InitializeComponent();

      this.SizeChanged += SoundVizualizer_SizeChanged;

      this.Loaded += SoundVizualizer_Loaded;

      SpektrumAnalyzer.OnFFtTick += SpektrumAnalyzer_OnFFtTick;
    }
    #endregion
    private void SoundVizualizer_Loaded(object sender, RoutedEventArgs e)
    {
      AssignSpectrum();
    }

    private int _spectrumRenderInProgress;

    private void SpektrumAnalyzer_OnFFtTick(object sender, float[] e)
    {
      if (Interlocked.CompareExchange(ref spectrumRenderInProgress, 1, 0) != 0)
        return;

      float[] fftData = (float[])e.Clone();

      VSynchronizationContext.PostOnUIThread(async () =>
      {
        try
        {
          if (!IsEnabled || lineSpectrum == null || Visibility != Visibility.Visible)
            return;

          EnsureSpectrumBitmap();

          var spectrumPoints = await Task.Run(() =>
          {
            return lineSpectrum.CalculateSpectrumLineData(fftData, new System.Drawing.Size(width, height));
          });

          if (spectrumPoints != null)
            lineSpectrum.UpdateSpectrumBitmap(spectrumBitmap, spectrumPoints, bottomColor, topColor);
        }
        finally
        {
          Volatile.Write(ref spectrumRenderInProgress, 0);
        }
      });
    }

    private WriteableBitmap spectrumBitmap;
    private int spectrumRenderInProgress;
    private void EnsureSpectrumBitmap()
    {
      if (spectrumBitmap != null && spectrumBitmap.PixelWidth == width && spectrumBitmap.PixelHeight == height)
        return;

      spectrumBitmap = BitmapFactory.New(width, height);
      spectrumBitmap.Clear(System.Windows.Media.Colors.Transparent);

      Image.Source = spectrumBitmap;
    }

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

    #region SoundVizualizer_SizeChanged

    private void SoundVizualizer_SizeChanged(object sender, SizeChangedEventArgs e)
    {
      width = (int)e.NewSize.Width;
      height = (int)e.NewSize.Height;
    }

    #endregion

    #region AssignSpectrum

    private void AssignSpectrum()
    {
      lineSpectrum = new LineSpectrum()
      {
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
