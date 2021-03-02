using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using Microsoft.Xaml.Behaviors;
using VPlayer.WindowsPlayer.Behaviors;

namespace VPlayer.Player.Behaviors
{
  public class ShowHideMouseBehavior : Behavior<FrameworkElement>
  {
    protected override void OnAttached()
    {
      base.OnAttached();

      AssociatedObject.MouseMove += AssociatedObject_MouseMove;

      ShowHideMouseManager.OnHideMouse.Subscribe((x) =>
      {
        Application.Current.Dispatcher.Invoke(() =>
        {
          AssociatedObject.Visibility = Visibility.Hidden;
        });
      });

      ShowHideMouseManager.OnResetMouse.Subscribe((x) =>
      {
        Application.Current.Dispatcher.Invoke(() =>
        {
          AssociatedObject.Visibility = Visibility.Visible;
        });
      });
    }

    private void AssociatedObject_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
      ShowHideMouseManager.ResetMouse();
    }

    protected override void OnDetaching()
    {
      base.OnDetaching();

      AssociatedObject.MouseMove -= AssociatedObject_MouseMove;
    }
  }
}
