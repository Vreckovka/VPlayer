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
using VPlayer.Player.ViewModels;
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
using WindowsPlayerViewModel = VPlayer.Player.ViewModels.WindowsPlayerViewModel;

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
    }

    protected override void OnDetaching()
    {
      base.OnDetaching();
      AssociatedObject.Loaded -= AssociatedObject_Loaded;
      serialDisposable.Dispose();
    }

    private void AssociatedObject_Loaded(object sender, RoutedEventArgs e)
    {
      SubcsribeToSongChange();
    }


    private SerialDisposable serialDisposable = new SerialDisposable();
    private int last_songInPlayListIndex;

    private void SubcsribeToSongChange()
    {
      if (Kernel != null)
      {
        if (windowsPlayerViewModel == null)
          windowsPlayerViewModel = Kernel.Get<WindowsPlayerViewModel>();

        if (windowsPlayerViewModel != null)
        {
          last_songInPlayListIndex = 0;
          serialDisposable.Disposable = windowsPlayerViewModel.ActualSongChanged.Subscribe(OnSongChanged);
        }
      }
    }

    
    private void OnSongChanged(int songInPlayListIndex)
    {
      // Get the border of the listview (first child of a listview)
      Decorator border = VisualTreeHelper.GetChild(AssociatedObject, 0) as Decorator;

      // Get scrollviewer
      ScrollViewer scrollViewer = border?.Child as ScrollViewer;

      var scrollIndexOffset = (songInPlayListIndex - 3 < 0 ? 0 : songInPlayListIndex - 3) * StepSize;


      int max = 10;
      if (scrollViewer != null )
      {
        if ((songInPlayListIndex > last_songInPlayListIndex + max &&
            songInPlayListIndex > last_songInPlayListIndex) ||
            (songInPlayListIndex < last_songInPlayListIndex - max &&
             songInPlayListIndex < last_songInPlayListIndex))
        {
          scrollViewer.ScrollToVerticalOffset(scrollIndexOffset);
        }
        else if(songInPlayListIndex != last_songInPlayListIndex)
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