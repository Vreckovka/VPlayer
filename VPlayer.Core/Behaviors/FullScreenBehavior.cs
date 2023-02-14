using System;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;
using VCore.WPF.Managers;
using VPlayer.Core.ViewModels;
using VVLC;
using VVLC.Controls;

namespace VPlayer.Core.Behaviors
{
  public class FullScreenBehavior : Behavior<FrameworkElement>
  {
    public VideoView VideoView { get; set; }
    public FullscreenPlayer FullscreenPlayer { get; set; }
    public FrameworkElement VideoMenu { get; set; }

    public ButtonBase HideButton { get; set; }

    #region PlayerDataContext

    public static readonly DependencyProperty PlayerDataContextProperty =
      DependencyProperty.Register(
        nameof(PlayerDataContext),
        typeof(object),
        typeof(FullScreenBehavior),
        new PropertyMetadata(null));

    public object PlayerDataContext
    {
      get { return (object)GetValue(PlayerDataContextProperty); }
      set { SetValue(PlayerDataContextProperty, value); }
    }

    #endregion

    private SerialDisposable fullScrennDisposable;

    #region OnAttached

    protected override void OnAttached()
    {
      base.OnAttached();

      fullScrennDisposable = new SerialDisposable();

      AssociatedObject.MouseLeftButtonDown += AssociatedObject_MouseLeftButtonDown;
      AssociatedObject.MouseMove += AssociatedObject_MouseMove;
      fullScrennDisposable.Disposable = FullScreenManager.OnFullScreen.Subscribe(DecideFullScreen);
    }

    #endregion

    #region AssociatedObject_MouseMove

    private void AssociatedObject_MouseMove(object sender, MouseEventArgs e)
    {
      if(FullScreenManager.IsFullscreen)
        FullScreenManager.ResetMouse();
    }

    #endregion

    #region AssociatedObject_MouseLeftButtonDown

    private void AssociatedObject_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
      if (e.ClickCount >= 2)
      {
        FullScreenManager.IsFullscreen = !FullScreenManager.IsFullscreen;
      }
    }

    #endregion

    #region DecideFullScreen

    private void DecideFullScreen(bool isFullScreen)
    {
      if (isFullScreen)
        MakeFullScreen();
      else
        ResetFullScreen();
    }

    #endregion

    #region MakeFullScreen

    private void MakeFullScreen()
    {
      if (VideoView != null)
        VideoView.MakeFullScreen();

      VFocusManager.SetFocus(AssociatedObject);
      //var res2 = AssociatedObject?.CaptureMouse();

      if (FullscreenPlayer != null)
        FullscreenPlayer.Visibility = Visibility.Visible;

      //if (VideoMenu != null)
      //  VideoMenu.Visibility = Visibility.Collapsed;

      if (HideButton != null)
        HideButton.Visibility = Visibility.Collapsed;

      if (FullscreenPlayer != null)
        FullscreenPlayer.DataContext = PlayerDataContext;

      InputManager.Current.PreProcessInput += Current_PreProcessInput;


      Application.Current.MainWindow?.Focus();
    }

    #endregion

    #region ResetFullScreen

    private void ResetFullScreen()
    {
      if (VideoView != null)
        VideoView.ResetFullScreen();

      //AssociatedObject?.ReleaseMouseCapture();

      if (FullscreenPlayer != null)
        FullscreenPlayer.Visibility = Visibility.Hidden;

      if (VideoMenu != null)
        VideoMenu.Visibility = Visibility.Visible;

      if (HideButton != null)
        HideButton.Visibility = Visibility.Visible;

      InputManager.Current.PreProcessInput -= Current_PreProcessInput;
    }

    #endregion

    #region Current_PreProcessInput

    private void Current_PreProcessInput(object sender, PreProcessInputEventArgs args)
    {
      try
      {
        if (args != null && args.StagingItem != null && args.StagingItem.Input != null)
        {
          InputEventArgs inputEvent = args.StagingItem.Input;

          if (inputEvent is KeyboardEventArgs)
          {
            KeyboardEventArgs k = inputEvent as KeyboardEventArgs;
            RoutedEvent r = k.RoutedEvent;
            KeyEventArgs keyEvent = k as KeyEventArgs;

            if (r == Keyboard.KeyUpEvent)
            {
              if (keyEvent?.Key == Key.Escape)
              {
                FullScreenManager.IsFullscreen = !FullScreenManager.IsFullscreen;
              }
            }
          }
        }
      }
      catch (Exception ex)
      {

      }
    }

    #endregion

    #region OnDetaching

    protected override void OnDetaching()
    {
      base.OnDetaching();

      AssociatedObject.MouseLeftButtonDown -= AssociatedObject_MouseLeftButtonDown;
      AssociatedObject.MouseMove -= AssociatedObject_MouseMove;
      InputManager.Current.PreProcessInput -= Current_PreProcessInput;
      fullScrennDisposable.Dispose();
    }

    #endregion
  }
}
