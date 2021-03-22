using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

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

namespace VPlayer.WindowsPlayer.Behaviors
{
  public class FullScreenBehavior : Behavior<FrameworkElement>
  {
    #region VideoView

    public static readonly DependencyProperty VideoViewProperty =
      DependencyProperty.Register(
        nameof(VideoView),
        typeof(VideoView),
        typeof(FullScreenBehavior),
        new PropertyMetadata(null));

    public VideoView VideoView
    {
      get { return (VideoView)GetValue(VideoViewProperty); }
      set { SetValue(VideoViewProperty, value); }
    }

    #endregion

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

    protected override void OnAttached()
    {
      base.OnAttached();

      AssociatedObject.MouseLeftButtonDown += AssociatedObject_MouseLeftButtonDown;
      AssociatedObject.MouseMove += AssociatedObject_MouseMove;
    }

    private void AssociatedObject_MouseMove(object sender, MouseEventArgs e)
    {
      ShowHideMouseManager.ResetMouse();
    }

    private void AssociatedObject_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
      if (e.ClickCount >= 2)
      {
        MakeFullScreen();
      }
    }

    #region MakeFullScreen

    private void MakeFullScreen()
    {
      VideoView.PlayerDataContext = PlayerDataContext;
      VideoView.MakeFullScreen();
    }

    #endregion



    #region OnDetaching

    protected override void OnDetaching()
    {
      base.OnDetaching();

      AssociatedObject.MouseLeftButtonDown -= AssociatedObject_MouseLeftButtonDown;
      AssociatedObject.MouseMove -= AssociatedObject_MouseMove;
    }

    #endregion
  }
}
