using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Xaml.Behaviors;
using Ninject;
using Prism.Events;
using VPlayer.Core.Events;
using VPlayer.Player.Behaviors;
using VPlayer.WindowsPlayer.Views;
using VPlayer.WindowsPlayer.Views.WindowsPlayer;

namespace VPlayer.WindowsPlayer.Behaviors
{
  public class FullScreenBehavior : Behavior<FrameworkElement>
  {
    private bool isFullScreen;
    private DependencyObject originalParent;
   

    #region EventAggregator

    public static readonly DependencyProperty EventAggregatorProperty =
      DependencyProperty.Register(
        nameof(EventAggregator),
        typeof(IEventAggregator),
        typeof(FullScreenBehavior),
        new PropertyMetadata(null));

    public IEventAggregator EventAggregator
    {
      get { return (IEventAggregator)GetValue(EventAggregatorProperty); }
      set { SetValue(EventAggregatorProperty, value); }
    }

    #endregion Kernel

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
        if (!isFullScreen)
          MakeFullScreen();
        else
          MakeNormal();
      }
    }

    #region MakeFullScreen

    private void MakeFullScreen()
    {
      isFullScreen = true;

      var eventArg = EventAggregator;
      var dataContext = AssociatedObject.DataContext;

      AssociatedObject.DataContext = dataContext;

      originalParent = DisconnectFromParent();

      eventArg.GetEvent<ContentFullScreenEvent>().Publish(new ContentFullScreenEventArgs()
      {
        IsFullScreen = isFullScreen,
        View = AssociatedObject
      });
    }

    #endregion

    #region MakeNormal

    private void MakeNormal()
    {
      isFullScreen = false;


      EventAggregator.GetEvent<ContentFullScreenEvent>().Publish(new ContentFullScreenEventArgs()
      {
        IsFullScreen = isFullScreen,
        View = AssociatedObject
      });

      DisconnectFromParent();

      if (originalParent is Panel panel)
      {
        panel.Children.Add(AssociatedObject);
      }
      else if (originalParent is ContentPresenter contentPresenter)
      {
        contentPresenter.Content = AssociatedObject;
      }

    }

    #endregion

    #region DisconnectFromParent

    private DependencyObject DisconnectFromParent()
    {
      var visualParent = VisualTreeHelper.GetParent(AssociatedObject);

      if (visualParent is Panel panelParent)
      {
        panelParent.Children.Remove(AssociatedObject);
      }
      else if (visualParent is ContentPresenter contentPresenter)
      {
        contentPresenter.Content = null;
      }

      return visualParent;
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
