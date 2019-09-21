using System.Collections.Generic;
using VCore.ViewModels;

namespace VPlayer.Player.ViewModels.AudioAnalyzer
{
  public class SpectrumViewModel : ViewModel
  {
    public SpectrumViewModel(int linesCount)
    {
      LinesCount = linesCount;
      Data = new byte[linesCount];
    }

    private int linesCount;
    public int LinesCount
    {
      get { return linesCount; }
      set
      {
        if (value != linesCount)
        {
          linesCount = value;
          Data = new byte[linesCount];
          RaisePropertyChanged();
        }
      }
    }

    public byte[] Data { get; set; }
    public void Set(byte[] data)
    {
      Data = data;
    }

    public void ClearData()
    {
      //for (int i = 0; i < LinesCount; i++)
      //{
      //  Data[i] = 0;
      //}
    }
  }
}