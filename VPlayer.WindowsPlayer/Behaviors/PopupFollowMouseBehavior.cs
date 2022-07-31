using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;

namespace VPlayer.WindowsPlayer.Behaviors
{
  public class PopupFollowMouseBehavior : Behavior<Popup>
  {
    protected override void OnAttached()
    {
      base.OnAttached();

      AssociatedObject.Opened += AssociatedObject_Opened;
    }


    private void AssociatedObject_Opened(object sender, EventArgs e)
    {
      Point p = Mouse.GetPosition((IInputElement)sender);

      AssociatedObject.HorizontalOffset = p.X;
    }

    protected override void OnDetaching()
    {
      base.OnDetaching();

      AssociatedObject.Opened -= AssociatedObject_Opened;
    }
  }
}
