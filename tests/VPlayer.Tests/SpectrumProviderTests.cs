using System;
using System.Linq;
using Moq;
using WinformsVisualization.Visualization;
using Xunit;

namespace VPlayer.Tests
{
  public class SpectrumProviderTests
  {
    [Theory]
    [InlineData(1)]
    [InlineData(16)]
    [InlineData(80)]
    [InlineData(4096)]
    public void FrequencyConfigurationBeforeProviderIsReadyIsSafe(int bars)
    {
      var spectrum=new LineSpectrum {BarCount=bars,MaximumFrequency=15000,MinimumFrequency=0,IsXLogScale=true};
      Assert.Empty(spectrum.CalculateSpectrumLineData(new float[8192],new System.Drawing.Size(640,240)));
    }
    [Fact]
    public void LateProviderProducesFinitePointsAndRemovalStopsOutput()
    {
      var spectrum=new LineSpectrum {BarCount=16,AutomaticBarCountCalculation=false,MinimumFrequency=0};
      var provider=new Mock<ISpectrumProvider>();
      provider.Setup(x=>x.GetFftBandIndex(It.IsAny<float>())).Returns((float frequency)=>Math.Min(100,(int)(frequency/100)));
      spectrum.SpectrumProvider=provider.Object;
      var points=spectrum.CalculateSpectrumLineData(Enumerable.Repeat(.25f,8192).ToArray(),new System.Drawing.Size(640,240));
      Assert.Equal(16,points.Length);
      Assert.All(points,p=>Assert.True(double.IsFinite(p.Value)));
      provider.Verify(x=>x.GetFftBandIndex(It.IsAny<float>()),Times.AtLeastOnce);
      spectrum.SpectrumProvider=null;
      Assert.Empty(spectrum.CalculateSpectrumLineData(new float[8192],new System.Drawing.Size(640,240)));
    }
    [Fact]
    public void EmptyAndShortFftFramesNeverReadBeyondTheBuffer()
    {
      var spectrum=new LineSpectrum {BarCount=4096,AutomaticBarCountCalculation=false,MinimumFrequency=0};
      var provider=new Mock<ISpectrumProvider>();
      provider.Setup(x=>x.GetFftBandIndex(It.IsAny<float>())).Returns(4095);
      spectrum.SpectrumProvider=provider.Object;
      Assert.Empty(spectrum.CalculateSpectrumLineData(Array.Empty<float>(),new System.Drawing.Size(640,240)));
      Assert.Empty(spectrum.CalculateSpectrumLineData(new float[8],new System.Drawing.Size(640,240)));
    }
  }
}
