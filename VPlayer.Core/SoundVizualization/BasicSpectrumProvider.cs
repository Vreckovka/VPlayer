using System;
using CSCore.DSP;

namespace WinformsVisualization.Visualization
{
  public class BasicSpectrumProvider : FftProvider, ISpectrumProvider
  {
    private readonly int _sampleRate;

    public BasicSpectrumProvider(int channels, int sampleRate, FftSize fftSize)
        : base(channels, fftSize)
    {
      if (sampleRate <= 0)
        throw new ArgumentOutOfRangeException("sampleRate");
      _sampleRate = sampleRate;
    }

    public int GetFftBandIndex(float frequency)
    {
      int fftSize = (int)FftSize;
      double f = _sampleRate / 2.0;

      return (int)((frequency / f) * (fftSize / 2));
    }
  }
}