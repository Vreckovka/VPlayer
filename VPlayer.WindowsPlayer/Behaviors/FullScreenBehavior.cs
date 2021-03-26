using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System;
using System.Reactive.Disposables;
using Microsoft.Xaml.Behaviors;
using Ninject;
using Prism.Events;
using VCore.Helpers;
using VPlayer.Core.Events;
using VPlayer.Player.Behaviors;
using VPlayer.WindowsPlayer.ViewModels;
using VPlayer.WindowsPlayer.Views;
using VPlayer.WindowsPlayer.Views.WindowsPlayer;
using VPlayer.WindowsPlayer.Vlc;
using VPlayer.WindowsPlayer.Vlc.Controls;

namespace VPlayer.WindowsPlayer.Behaviors
{
  public class FullScreenBehavior : Behavior<FrameworkElement>
  {
    public VideoView VideoView { get; set; }
    public FullscreenPlayer FullscreenPlayer { get; set; }
    public Menu VideoMenu { get; set; }

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

    private void AssociatedObject_MouseMove(object sender, MouseEventArgs e)
    {
      FullScreenManager.ResetMouse();
    }

    private void AssociatedObject_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
      if (e.ClickCount >= 2)
      {
        FullScreenManager.IsFullscreen = !FullScreenManager.IsFullscreen;
      }
    }

    private void DecideFullScreen(bool isFullScreen)
    {
      if (isFullScreen)
        MakeFullScreen();
      else
        ResetFullScreen();
    }

    #region MakeFullScreen

    private void MakeFullScreen()
    {
      VideoView.MakeFullScreen();

      FullscreenPlayer.Visibility = Visibility.Visible;

      VideoMenu.Visibility = Visibility.Collapsed;

      FullscreenPlayer.DataContext = PlayerDataContext;
    }

    #endregion

    #region ResetFullScreen

    private void ResetFullScreen()
    {
      VideoView.ResetFullScreen();

      FullscreenPlayer.Visibility = Visibility.Hidden;

      VideoMenu.Visibility = Visibility.Visible;
    }

    #endregion


    #region OnDetaching

    protected override void OnDetaching()
    {
      base.OnDetaching();

      AssociatedObject.MouseLeftButtonDown -= AssociatedObject_MouseLeftButtonDown;
      AssociatedObject.MouseMove -= AssociatedObject_MouseMove;
      fullScrennDisposable.Dispose();
    }

    #endregion
  }
}
