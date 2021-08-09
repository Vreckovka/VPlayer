using System;
using System.Collections.Generic;
using System.Reactive;
using System.Text;
using System.Windows;
using Microsoft.Xaml.Behaviors;
using VPlayer.WindowsPlayer.Behaviors;

namespace VPlayer.Player.Behaviors
{
  public class ShowHideMouseBehavior : Behavior<FrameworkElement>
  {
    IDisposable hideMouse;
    IDisposable showMouse;
    protected override void OnAttached()
    {
      base.OnAttached();

      AssociatedObject.MouseMove += AssociatedObject_MouseMove;

      hideMouse = FullScreenManager.OnHideMouse.Subscribe((x) =>
      {
        Application.Current.Dispatcher.Invoke(() =>
        {
          AssociatedObject.Visibility = Visibility.Hidden;
        });
      });

      showMouse = FullScreenManager.OnResetMouse.Subscribe((x) =>
      {
        Application.Current.Dispatcher.Invoke(() =>
        {
          AssociatedObject.Visibility = Visibility.Visible;
        });
      });
    }


    private void AssociatedObject_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
      FullScreenManager.ResetMouse();
    }

    protected override void OnDetaching()
    {
      base.OnDetaching();

      AssociatedObject.MouseMove -= AssociatedObject_MouseMove;

      hideMouse?.Dispose();
      showMouse?.Dispose();
    }
  }
}
