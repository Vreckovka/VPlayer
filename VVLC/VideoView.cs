﻿using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms.Integration;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using LibVLCSharp.Shared;
using VPlayer.WindowsPlayer.Vlc.Controls;
using MediaPlayer = LibVLCSharp.Shared.MediaPlayer;

namespace VPlayer.WindowsPlayer.Vlc
{

  public class VideoView : ContentControl, IVideoView, IDisposable
  {
    /// <summary>
    /// WPF VideoView constructor
    /// </summary>
    public VideoView()
    {
      DefaultStyleKey = typeof(VideoView);

      Application.Current.Exit += Current_Exit;
    }

    private void Current_Exit(object sender, ExitEventArgs e)
    {
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
        ForegroundWindow = new ForegroundWindow(this)
        {
          OverlayContent = ViewContent
        };

        ForegroundWindow.Loaded += ForegroundWindow_Loaded;

        ForegroundWindow.Show();
      }
    }

    private void ForegroundWindow_Loaded(object sender, RoutedEventArgs e)
    {
      Hwnd = (new WindowInteropHelper(ForegroundWindow)).Handle;

      if (MediaPlayer == null)
      {
        Trace.Write("No MediaPlayer is set, aborting...");
        return;
      }

      MediaPlayer.Hwnd = Hwnd;
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

  }
}


