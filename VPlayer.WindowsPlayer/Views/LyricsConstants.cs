using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using VCore.Standard;

namespace VPlayer.WindowsPlayer.Views
{
  public class LyricsConstants : ViewModel
  {
    #region Instance

    private static LyricsConstants instance;

    public static LyricsConstants Instance
    {
      get
      {
        if (instance == null)
        {
          instance = new LyricsConstants();
        }

        return instance;
      }
    }

    #endregion

    #region IsCinemaMode

    private bool isCinemaMode;

    public bool IsCinemaMode
    {
      get { return isCinemaMode; }
      set
      {
        if (value != isCinemaMode)
        {
          isCinemaMode = value;
          RaisePropertyChanged();
          RaisePropertyChanged(nameof(FontSize));
          RaisePropertyChanged(nameof(ActualLineFontSize));

          RaisePropertyChanged(nameof(LineHeight));


          RaisePropertyChanged(nameof(AutoScrollStep));
        }
      }
    }

    #endregion


    public double FontSize
    {
      get { return IsCinemaMode ? 35.0 : 25.0; }
    }

    public double ActualLineFontSize
    {
      get { return IsCinemaMode ? 60.0 : 32.0; }
    }

    public double LineHeight
    {
      get { return IsCinemaMode ? 42 : 31.0; }
    }


    public double AutoScrollStep
    {
      get
      {
        return LineHeight + Margin.Top + Margin.Bottom;
      }
    }

    public Thickness Margin
    {
      get
      {
        return new Thickness(5, 5, 5, 5);
      }
    }
  }
}
