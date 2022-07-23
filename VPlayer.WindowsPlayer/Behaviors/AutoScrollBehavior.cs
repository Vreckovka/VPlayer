using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using Logger;
using Microsoft.Xaml.Behaviors;
using Ninject;
using VCore.WPF.Behaviors;
using VCore.WPF.Helpers;
using VCore.WPF.Misc;
using VPlayer.Core.ViewModels;
using Decorator = System.Windows.Controls.Decorator;
using DependencyProperty = System.Windows.DependencyProperty;
using IKernel = Ninject.IKernel;
using ListView = System.Windows.Controls.ListView;
using PropertyMetadata = System.Windows.PropertyMetadata;
using RoutedEventArgs = System.Windows.RoutedEventArgs;
using ScrollViewer = System.Windows.Controls.ScrollViewer;
using TimeSpan = System.TimeSpan;
using VisualTreeHelper = System.Windows.Media.VisualTreeHelper;

namespace VPlayer.Player.Behaviors
{
  public class AutoScrollBehavior : Behavior<ListView>
  {
    private IPlayableRegionViewModel managablePlayableRegionViewModel;

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

    #region AutoscrollCommand

    protected ActionCommand autoscrollCommand;

    public ICommand AutoscrollCommand
    {
      get
      {
        return autoscrollCommand ??= new ActionCommand(() => Autoscroll(last_songInPlayListIndex, true));
      }
    }

    #endregion

    public double StepSize { get; set; } = 1;
    public TimeSpan AnimationTime { get; set; } = TimeSpan.FromSeconds(1);

    protected override void OnAttached()
    {
      base.OnAttached();

      AssociatedObject.DataContextChanged += AssociatedObject_DataContextChanged;
      AssociatedObject.LayoutUpdated += AssociatedObject_LayoutUpdated;
    }

    private void AssociatedObject_LayoutUpdated(object sender, EventArgs e)
    {
      if (scrollViewer == null)
      {
        scrollViewer = AssociatedObject.GetFirstChildOfType<ScrollViewer>();

        SubcsribeToSongChange();
      }

    }

    private void AssociatedObject_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
      last_songInPlayListIndex = 0;
    }

    protected override void OnDetaching()
    {
      base.OnDetaching();

      AssociatedObject.LayoutUpdated -= AssociatedObject_LayoutUpdated;
      AssociatedObject.DataContextChanged -= AssociatedObject_DataContextChanged;

      actualItemChangedDisposable.Dispose();
      searchChanged.Dispose();
    }



    private SerialDisposable actualItemChangedDisposable = new SerialDisposable();
    private SerialDisposable searchChanged = new SerialDisposable();

    private ILogger logger;

    #region SubcsribeToSongChange

    private void SubcsribeToSongChange()
    {
      try
      {
        if (Kernel != null)
        {
          logger = Kernel.Get<ILogger>();

          if (lastDatacontext != AssociatedObject.DataContext)
          {
            last_songInPlayListIndex = 0;
            lastDatacontext = AssociatedObject.DataContext;
          }
          else 
          {
            var verticalOffset = GetScrollOffset(last_songInPlayListIndex);

            if (scrollViewer.VerticalOffset != verticalOffset)
              scrollViewer.ScrollToVerticalOffset(verticalOffset);
          }


          if (managablePlayableRegionViewModel == null)
          {
            managablePlayableRegionViewModel = AssociatedObject.DataContext as IPlayableRegionViewModel;

            if (managablePlayableRegionViewModel != null)
            {
              actualItemChangedDisposable.Disposable = managablePlayableRegionViewModel.ActualItemChanged
                .ObserveOn(Application.Current.Dispatcher)
                .Subscribe((x) => Autoscroll(x));

              searchChanged.Disposable = managablePlayableRegionViewModel.ObservePropertyChange(x => x.ActualSearch)
                .Throttle(TimeSpan.FromSeconds(0.5))
                .Where(x => string.IsNullOrEmpty(x))
                .ObserveOn(Application.Current.Dispatcher)
                .Subscribe((x) => Autoscroll(last_songInPlayListIndex, true));
            }
          }
        }
      }
      catch (Exception ex)
      {
        logger?.Log(ex);
      }
    }

    #endregion


    private ScrollViewer scrollViewer;
    private int last_songInPlayListIndex = 0;
    private object lastDatacontext = null;


    private double GetScrollOffset(int songIndex)
    {
      return (songIndex - 3 < 0 ? 0 : songIndex - 3) * StepSize;
    }

    private void Autoscroll(int songInPlayListIndex, bool force = false)
    {
      try
      {
        if (scrollViewer == null || !string.IsNullOrEmpty(managablePlayableRegionViewModel.ActualSearch))
        {
          return;
        }

        var scrollIndexOffset = GetScrollOffset(songInPlayListIndex);

        int max = 10;

        if (scrollViewer != null)
        {
          if (((songInPlayListIndex > last_songInPlayListIndex + max &&
                songInPlayListIndex > last_songInPlayListIndex) ||
               (songInPlayListIndex < last_songInPlayListIndex - max &&
                songInPlayListIndex < last_songInPlayListIndex)) || force)
          {
            scrollViewer.ScrollToVerticalOffset(scrollIndexOffset);
          }
          else if (songInPlayListIndex != last_songInPlayListIndex)
          {
            var diff = Math.Abs(scrollViewer.VerticalOffset - scrollIndexOffset);

            if (diff > StepSize * 3)
            {
              scrollViewer.ScrollToVerticalOffset(scrollIndexOffset);
            }
            else
            {
              DoubleAnimation verticalAnimation = new DoubleAnimation();

              verticalAnimation.From = scrollViewer.VerticalOffset;
              verticalAnimation.To = scrollIndexOffset;

              if (last_songInPlayListIndex == songInPlayListIndex - 1)
              {
                verticalAnimation.Duration = new Duration(AnimationTime);
              }
              else
              {
                verticalAnimation.Duration = new Duration(TimeSpan.FromSeconds(0.50));
              }

              Storyboard storyboard = new Storyboard();

              verticalAnimation.EasingFunction = new SineEase() {EasingMode = EasingMode.EaseOut};
              storyboard.SpeedRatio = 1.2;

              storyboard.Children.Add(verticalAnimation);
              Storyboard.SetTarget(verticalAnimation, scrollViewer);
              Storyboard.SetTargetProperty(verticalAnimation, new PropertyPath(ScrollAnimationBehavior.VerticalOffsetProperty));
              storyboard.Begin();
            }
          }
        }
      }
      catch (Exception ex)
      {
        logger?.Log(ex);
      }
      finally
      {
        last_songInPlayListIndex = songInPlayListIndex;
      }
    }
  }
}