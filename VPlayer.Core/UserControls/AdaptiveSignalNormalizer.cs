using System;

namespace VPlayer.Player.UserControls
{
  public sealed class AdaptiveSignalNormalizer
  {
    private bool initialized;

    private double average;
    private double peak;


    public double AverageAdaptation { get; set; } =
      0.03;

    public double PeakDecay { get; set; } =
      0.997;


    public double Update(double value)
    {
      if (double.IsNaN(value) ||
          double.IsInfinity(value))
      {
        return 0.0;
      }


      value =
        Math.Max(
          0.0,
          value
        );


      if (!initialized)
      {
        average =
          Math.Max(
            value,
            0.0000001
          );


        peak =
          Math.Max(
            value,
            0.0000001
          );


        initialized = true;

        return 0.0;
      }


      peak =
        Math.Max(
          value,
          peak * PeakDecay
        );


      double peakLevel =
        value /
        Math.Max(
          peak,
          0.0000001
        );


      peakLevel =
        Clamp01(
          peakLevel
        );


      double ratio =
        value /
        Math.Max(
          average,
          0.0000001
        );


      double relativeRise =
        Clamp01(
          (ratio - 1.0) /
          1.5
        );


      double result =
        peakLevel * 0.70 +
        relativeRise * 0.30;


      average =
        average *
        (1.0 - AverageAdaptation) +
        value *
        AverageAdaptation;


      return
        Clamp01(
          result
        );
    }


    public void Reset()
    {
      initialized = false;

      average = 0.0;
      peak = 0.0;
    }


    private static double Clamp01(
      double value)
    {
      return Math.Max(
        0.0,
        Math.Min(
          1.0,
          value
        )
      );
    }
  }
}


