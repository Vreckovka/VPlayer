using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;
using System.Windows.Media;

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
      System.Drawing.Point p = System.Windows.Forms.Cursor.Position;

      Point position = AssociatedObject.PlacementTarget.PointToScreen(new Point(0d, 0d));

      AssociatedObject.HorizontalOffset = p.X;
      AssociatedObject.VerticalOffset = position.Y - AssociatedObject.Height;
    }

    protected override void OnDetaching()
    {
      base.OnDetaching();

      AssociatedObject.Opened -= AssociatedObject_Opened;
    }
  }
}
