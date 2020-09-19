using System;
using System.Collections.Generic;
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
using CSCore.DSP;
using CSCore.SoundIn;
using CSCore.Streams;
using CSCore.Streams.Effects;
using WinformsVisualization.Visualization;

namespace VPlayer.Player.UserControls
{
  /// <summary>
  /// Interaction logic for SoundVizualizer.xaml
  /// </summary>
  public partial class SoundVizualizer : UserControl
  {
    public LineSpectrum lineSpectrum;
    private IWaveSource waveSource;

    #region Constructors

    public SoundVizualizer()
    {
      InitializeComponent();

      this.Loaded += SoundVizualizer_Loaded;


    }



    #endregion

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
                audioVizualizer.CreateStackPanel();
              }
            }
          }
        }));


    #endregion

    #region ColumnBrush

    public SolidColorBrush ColumnBrush
    {
      get { return (SolidColorBrush)GetValue(ColumnBrushProperty); }
      set { SetValue(ColumnBrushProperty, value); }
    }

    public static readonly DependencyProperty ColumnBrushProperty =
      DependencyProperty.Register(
        nameof(ColumnBrush),
        typeof(SolidColorBrush),
        typeof(SoundVizualizer),
        new PropertyMetadata(Brushes.Black, (x, y) =>
        {
          if (x is SoundVizualizer audioVizualizer)
          {
            audioVizualizer.CreateStackPanel();
          }
        }));


    #endregion

    private void SoundVizualizer_Loaded(object sender, RoutedEventArgs e)
    {
      var timer = new Timer(40);

      //open the default device 
      var _soundIn = new WasapiLoopbackCapture();
      //Our loopback capture opens the default render device by default so the following is not needed
      //_soundIn.Device = MMDeviceEnumerator.DefaultAudioEndpoint(DataFlow.Render, Role.Console);
      _soundIn.Initialize();

      var soundInSource = new SoundInSource(_soundIn);
      ISampleSource source = soundInSource.ToSampleSource().AppendSource(x => new PitchShifter(x), out var _pitchShifter);

      SetupSampleSource(source);

      // We need to read from our source otherwise SingleBlockRead is never called and our spectrum provider is not populated
      byte[] buffer = new byte[waveSource.WaveFormat.BytesPerSecond / 2];
      soundInSource.DataAvailable += (s, aEvent) =>
      {
        int read;
        while ((read = waveSource.Read(buffer, 0, buffer.Length)) > 0) ;
      };

      //play the audio
      _soundIn.Start();

      timer.Start();

      timer.Elapsed += timer1_Tick;

      //propertyGridTop.SelectedObject = _lineSpectrum;
      //propertyGridBottom.SelectedObject = _voicePrint3DSpectrum;
      CreateStackPanel();
    }

    private void SetupSampleSource(ISampleSource aSampleSource)
    {
      const FftSize fftSize = FftSize.Fft4096;
      //create a spectrum provider which provides fft data based on some input
      var spectrumProvider = new BasicSpectrumProvider(aSampleSource.WaveFormat.Channels,
        aSampleSource.WaveFormat.SampleRate, fftSize);

      //linespectrum and voiceprint3dspectrum used for rendering some fft data
      //in oder to get some fft data, set the previously created spectrumprovider 
      lineSpectrum = new LineSpectrum(fftSize)
      {
        SpectrumProvider = spectrumProvider,
        UseAverage = true,
        BarCount = NumberOfColumns,
        BarSpacing = 2,
        IsXLogScale = true,
        ScalingStrategy = ScalingStrategy.Sqrt
      };

      var notificationSource = new SingleBlockNotificationStream(aSampleSource);

      notificationSource.SingleBlockRead += (s, a) => spectrumProvider.Add(a.Left, a.Right);

      waveSource = notificationSource.ToWaveSource(16);

    }


    private void timer1_Tick(object sender, EventArgs e)
    {
      Application.Current.Dispatcher.Invoke(() =>
      {
        var data = lineSpectrum.fttData();

        if (data != null)
          _t_Tick(data);
      });
    }

    private void CreateStackPanel()
    {
      mainStackPanel.Children.Clear();

      for (int i = 0; i < NumberOfColumns; i++)
      {
        var newprogres = new Grid();
        newprogres.Background = ColumnBrush;
        newprogres.Height = 5;
        newprogres.Width = 1;
        newprogres.Margin = new Thickness(1);


        mainStackPanel.Children.Add(newprogres);
      }
    }

    private void _t_Tick(float[] _fft)
    {
      int x, y;
      int b0 = 0;
      List<byte> _spectrumdata = new List<byte>();

      //computes the spectrum data, the code is taken from a bass_wasapi sample.
      for (x = 0; x < NumberOfColumns; x++)
      {
        float peak = 0;
        int b1 = (int)Math.Pow(2, x * 10.0 / (NumberOfColumns - 1));
        if (b1 > 1023) b1 = 1023;
        if (b1 <= b0) b1 = b0 + 1;
        for (; b0 < b1; b0++)
        {
          if (peak < _fft[1 + b0]) peak = _fft[1 + b0];
        }
        y = (int)(Math.Sqrt(peak) * 3 * 255 - 4);
        if (y > 255) y = 255;
        if (y < 0) y = 0;

        _spectrumdata.Add((byte)y);

        //Console.Write("{0, 3} ", y);
      }

      Set(_spectrumdata);

      _spectrumdata.Clear();
    }

    private async void Set(List<byte> data)
    {
      if (data.Count > 1)
        for (int i = 0; i < NumberOfColumns; i++)
        {
          var gridd = ((Grid)mainStackPanel.Children[i]);

          var asdPrec = (data[i] * 100) / 255;
          var asd = (asdPrec * ActualHeight) / 100;

          ScaleTransform trans = new ScaleTransform();
          gridd.RenderTransform = trans;
          // if you use the same animation for X & Y you don't need anim1, anim2 
          DoubleAnimation anim = new DoubleAnimation(asd, TimeSpan.FromMilliseconds(0));
          anim.AutoReverse = false;

          //trans.BeginAnimation(ScaleTransform.ScaleXProperty, anim);
          trans.BeginAnimation(ScaleTransform.ScaleXProperty, anim);

          //gridd.Width = data[i];
        }
    }
  }
}
