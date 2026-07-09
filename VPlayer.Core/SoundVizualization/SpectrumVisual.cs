using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace VPlayer.Core.SoundVizualization
{
  public class SpectrumVisual : FrameworkElement
  {
    private double[] values = Array.Empty<double>();
    private readonly Brush brush;
    private readonly Pen pen;

    public double BarWidth { get; set; } = 4;
    public double BarSpacing { get; set; } = 2;

    public SpectrumVisual(Color bottomColor, Color middleColor, Color topColor, double barWidth)
    {
      BarWidth = barWidth;

      var gradient = new LinearGradientBrush();
      gradient.StartPoint = new Point(0, 1);
      gradient.EndPoint = new Point(0, 0);
      gradient.GradientStops.Add(new GradientStop(bottomColor, 0));
      gradient.GradientStops.Add(new GradientStop(middleColor, 0.5));
      gradient.GradientStops.Add(new GradientStop(topColor, 1));
      gradient.Freeze();

      brush = gradient;
      pen = new Pen(brush, BarWidth);
      pen.Freeze();
    }

    public void SetValues(double[] newValues)
    {
      values = newValues;
      InvalidateVisual();
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
      base.OnRender(drawingContext);

      double height = ActualHeight;

      for (int i = 0; i < values.Length; i++)
      {
        double x = BarSpacing * (i + 1) + BarWidth * i;
        double barHeight = Math.Max(0, Math.Min(height, values[i]));

        drawingContext.DrawRectangle(brush, null, new Rect(x, height - barHeight, BarWidth, barHeight));
      }
    }
  }
}
