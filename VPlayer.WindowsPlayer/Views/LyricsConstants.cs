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
  public class LightsConstants : ViewModel
  {
    #region Instance

    private static LightsConstants instance;

    public static LightsConstants Instance
    {
      get
      {
        if (instance == null)
        {
          instance = new LightsConstants();
        }

        return instance;
      }
    }

    #endregion


    #region ShowBackgroundTopLights

    private bool showBackgroundTopLights = true;

    public bool ShowBackgroundTopLights
    {
      get { return showBackgroundTopLights; }
      set
      {
        if (value != showBackgroundTopLights)
        {
          showBackgroundTopLights = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region ShowBackgroundBottomLights

    private bool showBackgroundBottomLights = true;

    public bool ShowBackgroundBottomLights
    {
      get { return showBackgroundBottomLights; }
      set
      {
        if (value != showBackgroundBottomLights)
        {
          showBackgroundBottomLights = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region ShowBackgroundSupportLights

    private bool showBackgroundSupportLights = true;

    public bool ShowBackgroundSupportLights
    {
      get { return showBackgroundSupportLights; }
      set
      {
        if (value != showBackgroundSupportLights)
        {
          showBackgroundSupportLights = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region EnableBackgroundLighting

    private bool enableBackgroundLighting = true;

    public bool EnableBackgroundLighting
    {
      get { return enableBackgroundLighting; }
      set
      {
        if (value != enableBackgroundLighting)
        {
          enableBackgroundLighting = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region EnableLighting

    private bool enableLighting = true;

    public bool EnableLighting
    {
      get { return enableLighting; }
      set
      {
        if (value != enableLighting)
        {
          enableLighting = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion
  }

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
          RaiseConstants();
        }
      }
    }

    #endregion

    public void RaiseConstants()
    {
      RaisePropertyChanged(nameof(FontSize));
      RaisePropertyChanged(nameof(ActualLineFontSize));
      RaisePropertyChanged(nameof(LineHeight));
      RaisePropertyChanged(nameof(AutoScrollStep));
      RaisePropertyChanged(nameof(VizualizerBottomColor));
      RaisePropertyChanged(nameof(VizualizerTopColor));
      RaisePropertyChanged(nameof(Margin));
    }

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
      get
      {
        var originalHeight = IsCinemaMode ? 42 : 31.0;

        var final = originalHeight;
        if (IsVideo)
          final *= 1.15;

        return final;
      }
    }

    #region LyricsBackground

    private string lyricsBackground = "#00000000";

    public string LyricsBackground
    {
      get { return lyricsBackground; }
      set
      {
        if (value != lyricsBackground)
        {
          lyricsBackground = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region LyricsHeight

    private double lyricsHeight = 390;

    public double LyricsHeight
    {
      get { return lyricsHeight; }
      set
      {
        if (value != lyricsHeight)
        {
          lyricsHeight = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region ShowFrontVizualizer

    private bool showFrontVizualizer = true;

    public bool ShowFrontVizualizer
    {
      get { return showFrontVizualizer; }
      set
      {
        if (value != showFrontVizualizer)
        {
          showFrontVizualizer = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region ShowBackgroundVizualizer

    private bool showBackgroundVizualizer = false;

    public bool ShowBackgroundVizualizer
    {
      get { return showBackgroundVizualizer; }
      set
      {
        if (value != showBackgroundVizualizer)
        {
          showBackgroundVizualizer = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region BackgroundVizualizerMargin

    private string backgroundVizualizerMargin = "-450,0,0,-135";

    public string BackgroundVizualizerMargin
    {
      get { return backgroundVizualizerMargin; }
      set
      {
        if (value != backgroundVizualizerMargin)
        {
          backgroundVizualizerMargin = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region BackgroundShadowOpacity

    private double backgroundShadowOpacity = 1;

    public double BackgroundShadowOpacity
    {
      get { return backgroundShadowOpacity; }
      set
      {
        if (value != backgroundShadowOpacity)
        {
          backgroundShadowOpacity = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion


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


    #region VizualizerBottomColor

    private string vizualizerBottomColor = "#FFFFE900";

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

    #region VizualizerMiddleColor

    private string vizualizerMiddleColor = "#FFFF8700";

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

    #region BarNumber

    private int barNumber = 300;

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
