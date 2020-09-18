using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interactivity;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Ninject;
using ScrollAnimateBehavior.AttachedBehaviors;
using VPlayer.Core.ViewModels;
using Application = System.Windows.Application;
using Console = System.Console;
using Decorator = System.Windows.Controls.Decorator;
using DependencyProperty = System.Windows.DependencyProperty;
using DispatcherTimer = System.Windows.Threading.DispatcherTimer;
using Exception = System.Exception;
using IKernel = Ninject.IKernel;
using ListView = System.Windows.Controls.ListView;
using PropertyMetadata = System.Windows.PropertyMetadata;
using RoutedEventArgs = System.Windows.RoutedEventArgs;
using ScrollViewer = System.Windows.Controls.ScrollViewer;
using TimeSpan = System.TimeSpan;
using VisualTreeHelper = System.Windows.Media.VisualTreeHelper;
using WindowsPlayerViewModel = VPlayer.WindowsPlayer.ViewModels.WindowsPlayerViewModel;

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

    public double StepSize { get; set; } = 1;
    public TimeSpan AnimationTime { get; set; } = TimeSpan.FromSeconds(1);

    private WindowsPlayerViewModel windowsPlayerViewModel;

    protected override void OnAttached()
    {
      base.OnAttached();
      AssociatedObject.Loaded += AssociatedObject_Loaded;
      AssociatedObject.DataContextChanged += AssociatedObject_DataContextChanged;
    }

    private void AssociatedObject_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
      last_songInPlayListIndex = 0;
    }

    protected override void OnDetaching()
    {
      base.OnDetaching();

      AssociatedObject.Loaded -= AssociatedObject_Loaded;
      AssociatedObject.DataContextChanged -= AssociatedObject_DataContextChanged;
      AssociatedObject.Initialized += AssociatedObject_Initialized;

      disposable.Dispose();
    }

    private void AssociatedObject_Initialized(object sender, EventArgs e)
    {
    }

    private void AssociatedObject_Loaded(object sender, RoutedEventArgs e)
    {
      SubcsribeToSongChange();
    }

    private SerialDisposable disposable = new SerialDisposable();


    private void SubcsribeToSongChange()
    {
      if (Kernel != null)
      {
        if (lastDatacontext != AssociatedObject.DataContext)
        {
          last_songInPlayListIndex = null;
          lastDatacontext = AssociatedObject.DataContext;
        }
        else if (last_songInPlayListIndex != null)
        {
          var verticalOffset = GetScrollOffset(last_songInPlayListIndex.Value);

          if (scrollViewer.VerticalOffset != verticalOffset)
            scrollViewer.ScrollToVerticalOffset(verticalOffset);
        }


        if (windowsPlayerViewModel == null)
        {
          windowsPlayerViewModel = Kernel.Get<WindowsPlayerViewModel>();

          if (windowsPlayerViewModel != null)
          {
            disposable.Disposable = windowsPlayerViewModel.ActualSongChanged.Subscribe(OnSongChanged);
          }
        }
      }
    }


    private ScrollViewer scrollViewer;
    private Decorator border;
    private int? last_songInPlayListIndex = null;
    private object lastDatacontext = null;

    private double GetScrollOffset(int songIndex)
    {
      return (songIndex - 3 < 0 ? 0 : songIndex - 3) * StepSize;
    }

    private void OnSongChanged(int songInPlayListIndex)
    {

      if (border == null)
      {
        border = VisualTreeHelper.GetChild(AssociatedObject, 0) as Decorator;
      }

      if (scrollViewer == null)
      {
        scrollViewer = border?.Child as ScrollViewer;
      }


      var scrollIndexOffset = GetScrollOffset(songInPlayListIndex);

      int max = 10;

      if (scrollViewer != null)
      {
        if ((songInPlayListIndex > last_songInPlayListIndex + max &&
            songInPlayListIndex > last_songInPlayListIndex) ||
            (songInPlayListIndex < last_songInPlayListIndex - max &&
             songInPlayListIndex < last_songInPlayListIndex))
        {
          scrollViewer.ScrollToVerticalOffset(scrollIndexOffset);
        }
        else if (songInPlayListIndex != last_songInPlayListIndex)
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

          storyboard.Children.Add(verticalAnimation);
          Storyboard.SetTarget(verticalAnimation, scrollViewer);
          Storyboard.SetTargetProperty(verticalAnimation, new PropertyPath(ScrollAnimationBehavior.VerticalOffsetProperty));
          storyboard.Begin();
        }
      }

      last_songInPlayListIndex = songInPlayListIndex;
    }
  }
}