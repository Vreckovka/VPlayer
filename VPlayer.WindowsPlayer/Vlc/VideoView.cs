using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms.Integration;
using System.Windows.Interop;
using System.Windows.Media;
using LibVLCSharp.Shared;
using VPlayer.WindowsPlayer.Vlc.Controls;
using MediaPlayer = LibVLCSharp.Shared.MediaPlayer;

namespace VPlayer.WindowsPlayer.Vlc
{
  /// <summary>
  /// WPF VideoView with databinding for use with LibVLCSharp
  /// </summary>
  [TemplatePart(Name = PART_PlayerHost, Type = typeof(WindowsFormsHost))]
  [TemplatePart(Name = PART_PlayerView, Type = typeof(System.Windows.Forms.Panel))]
  public class VideoView : ContentControl, IVideoView, IDisposable
  {
    private const string PART_PlayerHost = "PART_PlayerHost";
    private const string PART_PlayerView = "PART_PlayerView";

    private WindowsFormsHost? WindowsFormsHost => Template.FindName(PART_PlayerHost, this) as WindowsFormsHost;

    /// <summary>
    /// WPF VideoView constructor
    /// </summary>
    public VideoView()
    {
      DefaultStyleKey = typeof(VideoView);

      Application.Current.MainWindow.Closing += MainWindow_Closing;
    }

    private void MainWindow_Closing(object sender, CancelEventArgs e)
    {
      Application.Current.MainWindow.Closing -= MainWindow_Closing;
      Dispose();

    }



    /// <summary>
    /// MediaPlayer WPF databinding property
    /// </summary>
    public static readonly DependencyProperty MediaPlayerProperty = DependencyProperty.Register(nameof(MediaPlayer),
            typeof(MediaPlayer),
            typeof(VideoView),
            new PropertyMetadata(null, OnMediaPlayerChanged));

    /// <summary>
    /// MediaPlayer property for this VideoView
    /// </summary>
    public MediaPlayer? MediaPlayer
    {
      get { return GetValue(MediaPlayerProperty) as MediaPlayer; }
      set { SetValue(MediaPlayerProperty, value); }
    }

    private static void OnMediaPlayerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      if (e.OldValue is MediaPlayer oldMediaPlayer)
      {
        oldMediaPlayer.Hwnd = IntPtr.Zero;
      }
      if (e.NewValue is MediaPlayer newMediaPlayer)
      {
        newMediaPlayer.Hwnd = ((VideoView)d).Hwnd;
      }
    }

    private bool IsDesignMode => (bool)DesignerProperties.IsInDesignModeProperty.GetMetadata(typeof(DependencyObject)).DefaultValue;
    private ForegroundWindow? ForegroundWindow { get; set; }
    private bool IsUpdatingContent { get; set; }
    private UIElement? ViewContent { get; set; }
    private IntPtr Hwnd { get; set; }

    public object PlayerDataContext { get; set; }

    /// <summary>
    /// ForegroundWindow management and MediaPlayer setup.
    /// </summary>
    public override void OnApplyTemplate()
    {
      base.OnApplyTemplate();

      if (!IsDesignMode)
      {
        var windowsFormsHost = WindowsFormsHost;
        if (windowsFormsHost != null)
        {
          ForegroundWindow = new ForegroundWindow(windowsFormsHost)
          {
            OverlayContent = ViewContent
          };
        }

        Hwnd = (Template.FindName(PART_PlayerView, this) as System.Windows.Forms.Panel)?.Handle ?? IntPtr.Zero;
        if (Hwnd == null)
        {
          Trace.WriteLine("HWND is NULL, aborting...");
          return;
        }

        if (MediaPlayer == null)
        {
          Trace.Write("No MediaPlayer is set, aborting...");
          return;
        }

        MediaPlayer.Hwnd = Hwnd;
      }
    }

    /// <summary>
    /// Override to update the foreground window content
    /// </summary>
    /// <param name="oldContent">old content</param>
    /// <param name="newContent">new content</param>
    ///


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

    #region IDisposable Support

    bool disposedValue;
    /// <summary>
    /// Unhook mediaplayer and dispose foreground window
    /// </summary>
    /// <param name="disposing"></param>
    protected virtual void Dispose(bool disposing)
    {
      if (!disposedValue)
      {
        if (disposing)
        {


          WindowsFormsHost?.Dispose();
          ForegroundWindow?.Close();
        }

        ViewContent = null;
        ForegroundWindow = null;
        disposedValue = true;
      }
    }

    private Window fullScreenWindow;
    public void MakeFullScreen()
    {
      var seek = MediaPlayer.Position;

      MediaPlayer.Stop();

      fullScreenWindow = new Window();
      fullScreenWindow.Loaded += Window_Loaded; ;


      fullScreenWindow.WindowStyle = WindowStyle.None;
      fullScreenWindow.Background = Brushes.Transparent;
      fullScreenWindow.ResizeMode = ResizeMode.NoResize;
      fullScreenWindow.AllowsTransparency = true;
      fullScreenWindow.ShowInTaskbar = false;
      fullScreenWindow.Topmost = true;
      fullScreenWindow.Style = null;

      var maoin = Application.Current.MainWindow;

      fullScreenWindow.Left = maoin.Left;
      fullScreenWindow.Top = maoin.Top;

      fullScreenWindow.Closed += FullScreenWindow_Closed;

      var grid = new VideoView()
      {
        Background = Brushes.Black,

        Content = new FullscreenPlayer()
        {
          DataContext = PlayerDataContext,
          OnDoubleClick = OnDoubleClick
        }
      };


      grid.Unloaded += Grid_Unloaded;
      fullScreenWindow.Content = grid;

      fullScreenWindow.Show();

      MediaPlayer.Play();

      MediaPlayer.Position = seek;
    }

    private void Grid_Unloaded(object sender, RoutedEventArgs e)
    {
      ((VideoView)sender).Dispose();
    }

    private void FullScreenWindow_Closed(object sender, EventArgs e)
    {
      ResetFullScreen();
    }

    private void OnDoubleClick()
    {
      fullScreenWindow.Close();
    }

    private void ResetFullScreen()
    {
      if (!disposing)
      {
        var seek = MediaPlayer.Position;

        MediaPlayer.Stop();

        MediaPlayer.Hwnd = Hwnd;

        MediaPlayer.Play();

        MediaPlayer.Position = seek;
      }
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
      var wn = (Window)sender;
      fullScreenWindow.WindowState = WindowState.Maximized;
      MediaPlayer.Hwnd = (new WindowInteropHelper(wn)).Handle;
    }

    private bool disposing;

    /// <summary>
    /// Unhook mediaplayer and dispose foreground window
    /// </summary>
    public void Dispose()
    {
      disposing = true;

      Dispose(true);

      fullScreenWindow?.Close();
    }

    #endregion
  }
}



