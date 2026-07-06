namespace WinformsVisualization.Visualization
{
    public interface ISpectrumProvider
    {
        bool GetFftData(float[] fftBuffer);
        int GetFftBandIndex(float frequency);
    }
}