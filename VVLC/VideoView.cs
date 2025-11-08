using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using LibVLCSharp.Shared;
using MediaPlayer = LibVLCSharp.Shared.MediaPlayer;

namespace VVLC
{
  public class VideoView : ContentControl, IVideoView, IDisposable
  {
    #region Fields

    private bool IsDesignMode => (bool)DesignerProperties.IsInDesignModeProperty.GetMetadata(typeof(DependencyObject)).DefaultValue;
    private ForegroundWindow? ForegroundWindow { get; set; }
    private bool IsUpdatingContent { get; set; }
    private UIElement? ViewContent { get; set; }
    private IntPtr Hwnd { get; set; }

    #endregion

    public VideoView()
    {
      DefaultStyleKey = typeof(VideoView);

      DataContextChanged += VideoView_DataContextChanged;
      this.IsVisibleChanged += VideoView_IsVisibleChanged;

      Application.Current.Exit += Current_Exit;

    }

    private void VideoView_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
      if(ForegroundWindow != null) 
      {
        if (Visibility != Visibility.Visible)
          ForegroundWindow.Hide();
        else
          ForegroundWindow.Show();
      }
     
    }

    #region Properties

    public static readonly DependencyProperty MediaPlayerProperty = DependencyProperty.Register(nameof(MediaPlayer),
            typeof(MediaPlayer),
            typeof(VideoView),
            new PropertyMetadata(null, OnMediaPlayerChanged));


    public MediaPlayer? MediaPlayer
    {
      get { return GetValue(MediaPlayerProperty) as MediaPlayer; }
      set { SetValue(MediaPlayerProperty, value); }
    }

    #endregion

    #region Methods

    #region VideoView_DataContextChanged

    private void VideoView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
      if (ForegroundWindow != null)
        ForegroundWindow.DataContext = DataContext;
    }

    #endregion

    #region OnMediaPlayerChanged

    private static void OnMediaPlayerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      if (e.OldValue is MediaPlayer oldMediaPlayer)
      {
        //oldMediaPlayer.Hwnd = IntPtr.Zero; // Causing crash 
      }
      if (e.NewValue is MediaPlayer newMediaPlayer)
      {
        newMediaPlayer.Hwnd = ((VideoView)d).Hwnd;
      }
    }


    #endregion

    #region OnApplyTemplate

    public override void OnApplyTemplate()
    {
      base.OnApplyTemplate();

      if (!IsDesignMode)
      {
        ForegroundWindow = new ForegroundWindow(this)
        {
          OverlayContent = ViewContent,
          DataContext = DataContext
        };

        ForegroundWindow.Loaded += ForegroundWindow_Loaded;
      }
    }

    #endregion

    #region ForegroundWindow_Loaded

    private async void ForegroundWindow_Loaded(object sender, RoutedEventArgs e)
    {
      Hwnd = (new WindowInteropHelper(ForegroundWindow)).Handle;

      if (MediaPlayer == null)
      {
        Trace.Write("No MediaPlayer is set, aborting...");
        return;
      }

      MediaPlayer.Hwnd = Hwnd;
      await Task.Delay(1000);

      if (Visibility != Visibility.Visible)
        ForegroundWindow.Hide();
    }

    #endregion

    #region OnContentChanged

    private object originalContent;
    protected override void OnContentChanged(object oldContent, object newContent)
    {
      base.OnContentChanged(oldContent, newContent);

      if (originalContent == null)
        originalContent = newContent;

      if (IsDesignMode || IsUpdatingContent)
      {
        return;
      }

      IsUpdatingContent = true;
      try
      {
        Content = null;
      }
      finally
      {
        IsUpdatingContent = false;
      }

      ViewContent = newContent as UIElement;
      if (ForegroundWindow != null)
      {
        ForegroundWindow.OverlayContent = ViewContent;

      }
    }

    #endregion

    #region MakeFullScreen

    public void MakeFullScreen()
    {
      ForegroundWindow.WindowState = WindowState.Maximized;
    }

    #endregion

    #region ResetFullScreen

    public void ResetFullScreen()
    {
      ForegroundWindow.WindowState = WindowState.Normal;
    }

    #endregion

    #region Current_Exit

    private void Current_Exit(object sender, ExitEventArgs e)
    {
      Dispose();
    }

    #endregion

    #region Dispose Support

    private bool disposing;

    /// <summary>
    /// Unhook mediaplayer and dispose foreground window
    /// </summary>
    public void Dispose()
    {
      disposing = true;

      Dispose(true);
    }


    bool disposedValue;

    protected virtual void Dispose(bool disposing)
    {
      if (!disposedValue)
      {
        if (disposing)
        {
          ForegroundWindow?.Close();
        }

        ViewContent = null;
        ForegroundWindow = null;
        disposedValue = true;
      }
    }

    #endregion

    #endregion
  }
}



