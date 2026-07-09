using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using CSCore.DSP;
using VPlayer.Core.SoundVizualization;
using Size = System.Drawing.Size;

namespace WinformsVisualization.Visualization
{
  public class LineSpectrum : SpectrumBase
  {
    private int _barCount;
    private double _barSpacing;

    private Size _currentSize;

    public LineSpectrum()
    {
    }

    public double NormlizedDataMaxValue { get; set; } = 30;
    public double NormlizedDataMinValue { get; set; } = 0;
    public double NormlizedDataMaxSilentValue { get; set; } = 5;
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

    public Bitmap CreateSpectrumLine(float[] fftData, Size size, Brush brush, Color background, bool highQuality)
    {
      if (!UpdateFrequencyMappingIfNessesary(size))
        return null;




      using (var pen = new Pen(brush, (float)BarWidth))
      {
        var bitmap = new Bitmap(size.Width, size.Height);
        using (Graphics graphics = Graphics.FromImage(bitmap))
        {
          PrepareGraphics(graphics, highQuality);
          graphics.Clear(background);

          CreateSpectrumLineInternal(graphics, pen, fftData, size);
        }

        return bitmap;
      }
    }

    #endregion
    private int[] spectrumGradient;
    private int spectrumGradientHeight;
    private Color spectrumGradientBottomColor;
    private Color spectrumGradientTopColor;
    public SpectrumPointData[] CalculateSpectrumLineData(float[] fftBuffer, Size size)
    {
      if (!UpdateFrequencyMappingIfNessesary(size))
        return null;

      var spectrumPoints = CalculateSpectrumPoints(size.Height, fftBuffer);

      if (UseSkew)
      {
        int count = spectrumPoints.Length;
        int p05 = (int)(count * 0.05);
        int p10 = (int)(count * 0.10);
        int p15 = (int)(count * 0.15);
        int p20 = (int)(count * 0.20);

        for (int i = 0; i < count; i++)
        {
          double value = spectrumPoints[i].Value;

          if (i < p05)
          {
          }
          else if (i < p10)
            value = Math.Pow(value, 1.2);
          else if (i < p15)
            value = Math.Pow(value, 1.4);
          else if (i < p20)
            value = Math.Pow(value, 1.6);
          else
            value *= value;

          spectrumPoints[i].Value = value;
        }
      }

      return NormalizeData(spectrumPoints, NormlizedDataMinValue, NormlizedDataMaxValue);
    }

    private int[] previousBarHeights;

    public unsafe void UpdateSpectrumBitmap(WriteableBitmap bitmap, SpectrumPointData[] spectrumPoints, System.Drawing.Color bottomColor, System.Drawing.Color topColor)
    {
      int width = bitmap.PixelWidth;
      int height = bitmap.PixelHeight;

      UpdateSpectrumGradient(height, bottomColor, topColor);

      if (previousBarHeights == null || previousBarHeights.Length != spectrumPoints.Length)
        previousBarHeights = new int[spectrumPoints.Length];

      using (var context = bitmap.GetBitmapContext())
      {
        int* pixels = context.Pixels;

        for (int i = 0; i < spectrumPoints.Length; i++)
        {
          SpectrumPointData p = spectrumPoints[i];

          int barIndex = p.SpectrumPointIndex;
          int xStart = (int)(BarSpacing * (barIndex + 1) + BarWidth * barIndex);
          int xEnd = Math.Min(width, xStart + (int)Math.Ceiling(BarWidth));

          if (xStart < 0 || xStart >= width || xEnd <= xStart)
            continue;

          int newHeight = Math.Clamp((int)(p.Value * 2 - 1), 0, height);
          int oldHeight = previousBarHeights[i];

          if (oldHeight > newHeight)
          {
            int clearStart = height - oldHeight;
            int clearEnd = height - newHeight;

            for (int y = clearStart; y < clearEnd; y++)
              for (int x = xStart; x < xEnd; x++)
                pixels[y * width + x] = 0;
          }

          int drawStart = height - newHeight;

          for (int y = drawStart; y < height; y++)
          {
            int offset = y * width + xStart;
            int color = spectrumGradient[y];

            for (int x = xStart; x < xEnd; x++)
              pixels[offset++] = color;
          }

          previousBarHeights[i] = newHeight;
        }
      }
    }

    private void UpdateSpectrumGradient(int height, System.Drawing.Color bottomColor, System.Drawing.Color topColor)
{
	if (spectrumGradient != null &&
		spectrumGradientHeight == height &&
		spectrumGradientBottomColor == bottomColor &&
		spectrumGradientTopColor == topColor)
		return;

	spectrumGradientHeight = height;
	spectrumGradientBottomColor = bottomColor;
	spectrumGradientTopColor = topColor;

	spectrumGradient = new int[height];

	for (int y = 0; y < height; y++)
	{
		double t = height <= 1 ? 0 : (double)y / (height - 1);

		byte a = (byte)(topColor.A + (bottomColor.A - topColor.A) * t);
		byte r = (byte)(topColor.R + (bottomColor.R - topColor.R) * t);
		byte g = (byte)(topColor.G + (bottomColor.G - topColor.G) * t);
		byte b = (byte)(topColor.B + (bottomColor.B - topColor.B) * t);

		r = (byte)(r * a / 255);
		g = (byte)(g * a / 255);
		b = (byte)(b * a / 255);

		spectrumGradient[y] = (a << 24 | r << 16 | g << 8 | b);
	}
}

    public Bitmap CreateSpectrumLine(float[] fftData, Size size, Brush brush, bool highQuality)
    {
      if (!UpdateFrequencyMappingIfNessesary(size))
        return null;

      using (var pen = new Pen(brush, (float)BarWidth))
      {
        var bitmap = new Bitmap(size.Width, size.Height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);

        using (Graphics graphics = Graphics.FromImage(bitmap))
        {
          PrepareGraphics(graphics, highQuality);
          graphics.Clear(Color.Transparent);
          CreateSpectrumLineInternal(graphics, pen, fftData, size);
        }

        return bitmap;
      }
    }

    #region CreateSpectrumLineInternal

    private void CreateSpectrumLineInternal(Graphics graphics, Pen pen, float[] fftBuffer, Size size)
    {
      int height = size.Height;

      var spectrumPoints = CalculateSpectrumPoints(height, fftBuffer);

      if (UseSkew)
      {
        int count = spectrumPoints.Length;

        int p05 = (int)(count * 0.05);
        int p10 = (int)(count * 0.10);
        int p15 = (int)(count * 0.15);
        int p20 = (int)(count * 0.20);

        for (int i = 0; i < count; i++)
        {
          double value = spectrumPoints[i].Value;

          if (i < p05)
          {
            // Power 1.0: no operation required.
          }
          else if (i < p10)
          {
            value = Math.Pow(value, 1.2);
          }
          else if (i < p15)
          {
            value = Math.Pow(value, 1.4);
          }
          else if (i < p20)
          {
            value = Math.Pow(value, 1.6);
          }
          else
          {
            value *= value;
          }

          spectrumPoints[i].Value = value;
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

      if (range != 0)
      {
        if (range < 0.2)
        {
          for (int i = 0; i < data.Length; i++)
          {
            data[i].Value = data[i].Value * 500000;

            while (data[i].Value < 0.1)
            {
              data[i].Value *= 10;
            }
          }

          max = NormlizedDataMaxSilentValue;
          dataMax = data.Max(x => x.Value);
          dataMin = data.Min(x => x.Value);
          range = dataMax - dataMin;
        }

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

      return data;
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