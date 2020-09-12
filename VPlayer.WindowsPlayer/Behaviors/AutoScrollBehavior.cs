using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interactivity;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Ninject;
using VPlayer.Core.ViewModels;
using VPlayer.Player.ViewModels;

namespace VPlayer.Player.Behaviors
{
  public class AutoScrollBehavior : Behavior<ListView>
  {
    #region Kernel

    public static readonly DependencyProperty KernelProperty =
      DependencyProperty.Register(
        nameof(Kernel),
        typeof(IKernel),
        typeof(AutoScrollBehavior),
        new PropertyMetadata(null));

    public IKernel Kernel
    {
      get { return (IKernel)GetValue(KernelProperty); }
      set { SetValue(KernelProperty, value); }
    }

    #endregion Kernel

    private WindowsPlayerViewModel windowsPlayerViewModel;

    protected override void OnAttached()
    {
      base.OnAttached();
      AssociatedObject.Initialized += AssociatedObject_Initialized;
      AssociatedObject.Loaded += AssociatedObject_Loaded;
    }

    private void AssociatedObject_Loaded(object sender, RoutedEventArgs e)
    {
      SubcsribeToSongChange();
    }

    private void AssociatedObject_Initialized(object sender, System.EventArgs e)
    {
      SubcsribeToSongChange();
    }

    private void SubcsribeToSongChange()
    {
      if (Kernel != null)
      {
        if (windowsPlayerViewModel == null)
          windowsPlayerViewModel = Kernel.Get<WindowsPlayerViewModel>();

        windowsPlayerViewModel.ActualSongChanged.Subscribe(OnSongChanged);
      }
    }

    DispatcherTimer scrollTimer = new DispatcherTimer();
    double animationLengthMiliseconds = 3500;
    private void OnSongChanged(int songInPlayListIndex)
    {
      // Get the border of the listview (first child of a listview)
      Decorator border = VisualTreeHelper.GetChild(AssociatedObject, 0) as Decorator;

      // Get scrollviewer
      ScrollViewer scrollViewer = border?.Child as ScrollViewer;

    
      var scrollIndexOffset = (songInPlayListIndex - 3 < 0 ? 0 : songInPlayListIndex - 3) * 50;

      //scrollViewer?.ScrollToVerticalOffset(scrollIndex);
    

      if (scrollViewer != null)
      {
        try
        {
          bool isNewIndexGreater = scrollIndexOffset > scrollViewer.VerticalOffset;

          scrollTimer.Stop();
          scrollTimer = new DispatcherTimer();

          double fps = 100;
          var difference = scrollIndexOffset - scrollViewer.VerticalOffset;
          var change = difference / (animationLengthMiliseconds / fps);

          scrollTimer.Start();

          scrollTimer.Interval = TimeSpan.FromMilliseconds(1000.0 / fps);

          var value = scrollViewer.VerticalOffset;
          scrollTimer.Tick += (s, e) =>
          {
            value += change;
            
            scrollViewer.ScrollToVerticalOffset(value); 

            if ((scrollViewer.VerticalOffset >= scrollIndexOffset && isNewIndexGreater) || (scrollViewer.VerticalOffset <= scrollIndexOffset && !isNewIndexGreater))
            {
              scrollTimer.Stop();
            }
          };
        }
        catch (Exception ex)
        {
        }
      }
    }
  }
}