using System;
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
    private Timer cursorTimer;
    private ElapsedEventHandler hideCursorDelegate;

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

      cursorTimer = new Timer(1500);
      cursorTimer.AutoReset = false;

      hideCursorDelegate = (s, e) =>
      {
        if (isFullScreen)
          MouseExt.SafeOverrideCursor(Cursors.None);
      };

      cursorTimer.Elapsed += hideCursorDelegate;
    }

    private void AssociatedObject_MouseMove(object sender, MouseEventArgs e)
    {
      cursorTimer.Stop();
      Mouse.OverrideCursor = null; //Show cursor
      cursorTimer.Start();
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

      cursorTimer.Elapsed -= hideCursorDelegate;
      AssociatedObject.MouseLeftButtonDown -= AssociatedObject_MouseLeftButtonDown;
      AssociatedObject.MouseMove -= AssociatedObject_MouseMove;
    }

    #endregion
  }

  public static class MouseExt
  {
    public static void SafeOverrideCursor(Cursor cursor)
    {
      Application.Current.Dispatcher.Invoke(new Action(() =>
      {
        Mouse.OverrideCursor = cursor;
      }));
    }
  }
}
