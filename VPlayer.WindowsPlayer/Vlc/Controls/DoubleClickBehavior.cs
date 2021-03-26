using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;
using VPlayer.WindowsPlayer.Behaviors;

namespace VPlayer.WindowsPlayer.Vlc.Controls
{
  public class DoubleClickBehavior : Behavior<FrameworkElement>
  {
    #region Command

    public static readonly DependencyProperty CommandProperty =
      DependencyProperty.Register(
        nameof(Command),
        typeof(ICommand),
        typeof(DoubleClickBehavior),
        new PropertyMetadata(null));

    public ICommand Command
    {
      get { return (ICommand)GetValue(CommandProperty); }
      set { SetValue(CommandProperty, value); }
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
      FullScreenManager.ResetMouse();
    }

    private void AssociatedObject_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
      if(e.ClickCount == 2)
      {
        Command?.Execute(null);
      }
    }

    protected override void OnDetaching()
    {
      AssociatedObject.MouseLeftButtonDown -= AssociatedObject_MouseLeftButtonDown;
      AssociatedObject.MouseMove -= AssociatedObject_MouseMove;
    }
  }
}
