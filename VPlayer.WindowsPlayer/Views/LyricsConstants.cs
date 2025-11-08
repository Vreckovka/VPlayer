using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Input;
using VCore.Standard;
using VCore.WPF.Misc;

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
          RaisePropertyChanged(nameof(VizualizerBottomColor));
          RaisePropertyChanged(nameof(VizualizerTopColor));
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
      get {  var originalHeight = IsCinemaMode ? 42 : 31.0;

        var final = originalHeight;
        if (IsVideo)
          final *= 1.15;

        return final;
      }
    }

    #region ShowHUD

    private bool showHUD = true;

    public bool ShowHUD
    {
      get { return showHUD; }
      set
      {
        if (value != showHUD)
        {
          showHUD = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region ShowLyrics

    private bool showLyrics = true;

    public bool ShowLyrics
    {
      get { return showLyrics; }
      set
      {
        if (value != showLyrics)
        {
          showLyrics = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region IsVideo

    private bool isVideo = false;

    public bool IsVideo
    {
      get { return isVideo; }
      set
      {
        if (value != isVideo)
        {
          isVideo = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region VideoPath

    private string videoPath;

    public string VideoPath
    {
      get { return videoPath; }
      set
      {
        if (value != videoPath)
        {
          videoPath = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion


    #region PlainImage

    private bool plainImage = false;

    public bool PlainImage
    {
      get { return plainImage; }
      set
      {
        if (value != plainImage)
        {
          plainImage = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region HideEQ

    private bool hideEQ = false;

    public bool HideEQ
    {
      get { return hideEQ; }
      set
      {
        if (value != hideEQ)
        {
          hideEQ = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region VizualizerTopColor

    private string vizualizerTopColor = "#FFFF0000";

    public string VizualizerTopColor
    {
      get { return vizualizerTopColor; }
      set
      {
        if (value != vizualizerTopColor)
        {
          vizualizerTopColor = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region VizualizerMiddleColor

    private string vizualizerMiddleColor = "#FFFF0000";

    public string VizualizerMiddleColor
    {
      get { return vizualizerMiddleColor; }
      set
      {
        if (value != vizualizerMiddleColor)
        {
          vizualizerMiddleColor = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region BarNumber

    private int barNumber = 250;

    public int BarNumber
    {
      get { return barNumber; }
      set
      {
        if (value != barNumber)
        {
          barNumber = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region BarWidth

    private int barWidth = 5;

    public int BarWidth
    {
      get { return barWidth; }
      set
      {
        if (value != barWidth)
        {
          barWidth = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region VizualizerBottomColor

    private string vizualizerBottomColor = "#FF45FF00";

    public string VizualizerBottomColor
    {
      get { return vizualizerBottomColor; }
      set
      {
        if (value != vizualizerBottomColor)
        {
          vizualizerBottomColor = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

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

    #region SetToFullHD

    protected ActionCommand setToFullHD;

    public ICommand SetToFullHD
    {
      get
      {
        return setToFullHD ??= new ActionCommand(OnSetToFullHD);
      }
    }

    protected virtual void OnSetToFullHD()
    {
      Application.Current.MainWindow.Width = 1980;
      Application.Current.MainWindow.Height = 1037;
    }

    #endregion

  
  }
}
