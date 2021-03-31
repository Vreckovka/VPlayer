using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;
using CSCore.DSP;

namespace WinformsVisualization.Visualization
{
  public class LineSpectrum : SpectrumBase
  {
    private int _barCount;
    private double _barSpacing;

    private Size _currentSize;

    public LineSpectrum(FftSize fftSize)
    {
      FftSize = fftSize;
    }

    public double NormlizedDataMaxValue { get; set; } = 30;
    public double NormlizedDataMinValue { get; set; } = 0;

    public bool UseSkew { get; set; }

    #region AutomaticBarCountCalculation

    private bool automaticBarCountCalculation;

    public bool AutomaticBarCountCalculation
    {
      get { return automaticBarCountCalculation; }
      set
      {
        if (value != automaticBarCountCalculation)
        {
          automaticBarCountCalculation = value;
          RaisePropertyChanged(nameof(CurrentSize));
        }
      }
    }

    #endregion

    #region MinimumBarWidth

    private double? minimumBarWidth;

    public double? MinimumBarWidth
    {
      get { return minimumBarWidth; }
      set
      {
        if (value != minimumBarWidth)
        {
          minimumBarWidth = value;

          //if (AutomaticBarCountCalculation)
          //{
          //  BarCount = (int)(_currentSize.Width / BarWidth);
          //}

          UpdateFrequencyMapping();
        }
      }
    }

    #endregion

    #region BarWidth

    private double barWidth = 1;
    public double BarWidth
    {
      get
      {
        return barWidth;
      }
      set
      {
        if (MinimumBarWidth != null && value < MinimumBarWidth)
        {
          value = MinimumBarWidth.Value;
        }

        barWidth = value;
      }
    }

    #endregion

    #region BarSpacing

    public double BarSpacing
    {
      get { return _barSpacing; }
      set
      {
        if (value < 0)
          throw new ArgumentOutOfRangeException("value");
        _barSpacing = value;
        UpdateFrequencyMapping();

        RaisePropertyChanged(nameof(BarSpacing));
        RaisePropertyChanged(nameof(BarWidth));
      }
    }

    #endregion

    #region BarCount

    public int BarCount
    {
      get { return _barCount; }
      set
      {
        if (value <= 0)
          throw new ArgumentOutOfRangeException("value");

        _barCount = value;
        SpectrumResolution = value;
        UpdateFrequencyMapping();

        RaisePropertyChanged("BarCount");
        RaisePropertyChanged("BarWidth");
      }
    }

    #endregion

    #region CurrentSize

    public Size CurrentSize
    {
      get { return _currentSize; }
      protected set
      {
        _currentSize = value;

        RaisePropertyChanged("CurrentSize");
      }
    }

    #endregion

    #region CreateSpectrumLine

    public Bitmap CreateSpectrumLine(Size size, Brush brush, Color background, bool highQuality)
    {
      if (!UpdateFrequencyMappingIfNessesary(size))
        return null;

      var fftBuffer = new float[(int)FftSize];

      //get the fft result from the spectrum provider
      if (SpectrumProvider.GetFftData(fftBuffer, this))
      {
        using (var pen = new Pen(brush, (float)BarWidth))
        {
          var bitmap = new Bitmap(size.Width, size.Height);
          using (Graphics graphics = Graphics.FromImage(bitmap))
          {
            PrepareGraphics(graphics, highQuality);
            graphics.Clear(background);

            CreateSpectrumLineInternal(graphics, pen, fftBuffer, size);
          }

          return bitmap;
        }
      }
      else
      {

      }

      return null;
    }

    #endregion

    #region CreateSpectrumLine

    public Bitmap CreateSpectrumLine(Size size, Color color1, Color color2, Color background, bool highQuality)
    {
      if (!UpdateFrequencyMappingIfNessesary(size))
        return null;

      using (Brush brush = new LinearGradientBrush(new RectangleF(0, 0, (float)BarWidth, size.Height), color2, color1, LinearGradientMode.Vertical))
      {
        return CreateSpectrumLine(size, brush, background, highQuality);
      }
    }

    #endregion

    #region CreateSpectrumLineInternal

    private void CreateSpectrumLineInternal(Graphics graphics, Pen pen, float[] fftBuffer, Size size)
    {
      int height = size.Height;

      var spectrumPoints = CalculateSpectrumPoints(height, fftBuffer);

      if (UseSkew)
      {
        for (int i = 0; i < spectrumPoints.Length; i++)
        {
          if (i < spectrumPoints.Length * 0.05)
            spectrumPoints[i].Value = Math.Pow(spectrumPoints[i].Value, 1);
          if (i < spectrumPoints.Length * 0.1)
            spectrumPoints[i].Value = Math.Pow(spectrumPoints[i].Value, 1.2);
          else if (i < spectrumPoints.Length * 0.15)
            spectrumPoints[i].Value = Math.Pow(spectrumPoints[i].Value, 1.4);
          else if (i < spectrumPoints.Length * 0.20)
            spectrumPoints[i].Value = Math.Pow(spectrumPoints[i].Value, 1.6);
          else
            spectrumPoints[i].Value = Math.Pow(spectrumPoints[i].Value, 2.0);
        }
      }

      SpectrumPointData[] spectrumPointsNormalized = NormalizeData(spectrumPoints, NormlizedDataMinValue, NormlizedDataMaxValue);

      //connect the calculated points with lines
      for (int i = 0; i < spectrumPointsNormalized.Length; i++)
      {
        SpectrumPointData p = spectrumPointsNormalized[i];
        int barIndex = p.SpectrumPointIndex;
        double xCoord = BarSpacing * (barIndex + 1) + (BarWidth * barIndex) + BarWidth / 2;

        var p1 = new PointF((float)xCoord, height);
        var p2 = new PointF((float)xCoord, height - ((float)(p.Value * 2) - 1));

        graphics.DrawLine(pen, p1, p2);
      }
    }

    #endregion

    #region NormalizeData

    private SpectrumPointData[] NormalizeData(SpectrumPointData[] data, double min, double max)
    {
      double dataMax = data.Max(x => x.Value);
      double dataMin = data.Min(x => x.Value);
      double range = dataMax - dataMin;

      var normalized =
         data.Select(d => (d.Value - dataMin) / range)
        .Select(n => (double)((1 - n) * min + n * max))
        .ToArray();

      var normalizeSpectrum = new SpectrumPointData[data.Length];

      for (int i = 0; i < data.Length; i++)
      {
        normalizeSpectrum[i] = new SpectrumPointData()
        {
          SpectrumPointIndex = data[i].SpectrumPointIndex,
          Value = normalized[i]
        };
      }

      return normalizeSpectrum;
    }

    #endregion

    #region UpdateFrequencyMapping

    protected override void UpdateFrequencyMapping()
    {
      BarWidth = Math.Max(((_currentSize.Width - (BarSpacing * (BarCount + 1))) / BarCount), 0.00001);

      base.UpdateFrequencyMapping();
    }

    #endregion

    #region UpdateFrequencyMappingIfNessesary

    private bool UpdateFrequencyMappingIfNessesary(Size newSize)
    {
      if (newSize != CurrentSize)
      {
        CurrentSize = newSize;
        UpdateFrequencyMapping();
      }

      return newSize.Width > 0 && newSize.Height > 0;
    }

    #endregion

    #region PrepareGraphics

    private void PrepareGraphics(Graphics graphics, bool highQuality)
    {
      if (highQuality)
      {
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.CompositingQuality = CompositingQuality.AssumeLinear;
        graphics.PixelOffsetMode = PixelOffsetMode.Default;
        graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
      }
      else
      {
        graphics.SmoothingMode = SmoothingMode.HighSpeed;
        graphics.CompositingQuality = CompositingQuality.HighSpeed;
        graphics.PixelOffsetMode = PixelOffsetMode.None;
        graphics.TextRenderingHint = TextRenderingHint.SingleBitPerPixelGridFit;
      }
    }

    #endregion
  }
}