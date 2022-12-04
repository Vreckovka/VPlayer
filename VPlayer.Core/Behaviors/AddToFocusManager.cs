using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using Microsoft.Xaml.Behaviors;
using VCore.WPF.Managers;

namespace VPlayer.Core.Behaviors
{
  public class AddToFocusManager : Behavior<FrameworkElement>
  {
    protected override void OnAttached()
    {
      base.OnAttached();

      AssociatedObject.GotFocus += AssociatedObject_GotFocus;
      AssociatedObject.LostFocus += AssociatedObject_LostFocus;
    }

    private void AssociatedObject_LostFocus(object sender, RoutedEventArgs e)
    {
      VFocusManager.RemoveFromFocusItems(AssociatedObject);
    }

    private void AssociatedObject_GotFocus(object sender, RoutedEventArgs e)
    {
      VFocusManager.AddToFocusItems(AssociatedObject);
    }

    protected override void OnDetaching()
    {
      base.OnDetaching();

      AssociatedObject.GotFocus -= AssociatedObject_GotFocus;
      AssociatedObject.LostFocus -= AssociatedObject_LostFocus;
    }
  }
}
