using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Microsoft.Xaml.Behaviors;
using VCore.WPF.Behaviors;
using VCore.WPF.Helpers;
using VCore.WPF.Views;
using VPlayer.Core.ViewModels;
using VPlayer.Core.ViewModels.SoundItems;
using VPlayer.Core.ViewModels.SoundItems.LRCCreators;

namespace VPlayer.Player.Behaviors
{
  public class AutoScrollLyricsBehavior : Behavior<ListView>
  {
    public double StepSize { get; set; } = 1;
    public TimeSpan AnimationTime { get; set; } = TimeSpan.FromSeconds(1);
    private bool wasUnloaded;
      

    protected override void OnAttached()
    {
      base.OnAttached();

      AssociatedObject.Loaded += AssociatedObject_Loaded;
      AssociatedObject.DataContextChanged += AssociatedObject_DataContextChanged;
      AssociatedObject.Unloaded += AssociatedObject_Unloaded;
    }

    private void AssociatedObject_Unloaded(object sender, RoutedEventArgs e)
    {
      serialDisposable.Dispose();
      wasUnloaded = true;
    }

    private void AssociatedObject_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
      Application.Current?.Dispatcher?.Invoke(() =>
      {
        var childCount = VisualTreeHelper.GetChildrenCount(AssociatedObject);

        if (childCount > 0)
        {
          Decorator border = VisualTreeHelper.GetChild(AssociatedObject, 0) as Decorator;
          ScrollViewer scrollViewer = border?.Child as ScrollViewer;
          scrollViewer?.ScrollToTop();
        }

      });

      SubcsribeToSongChange();
    }

    protected override void OnDetaching()
    {
      base.OnDetaching();

      AssociatedObject.Loaded -= AssociatedObject_Loaded;
      AssociatedObject.DataContextChanged -= AssociatedObject_DataContextChanged;
      AssociatedObject.Unloaded -= AssociatedObject_Unloaded;
      serialDisposable.Dispose();
    }

    private void AssociatedObject_Loaded(object sender, RoutedEventArgs e)
    {
      SubcsribeToSongChange();

      RenderOptions.SetBitmapScalingMode(AssociatedObject, BitmapScalingMode.LowQuality);
    }


    private SerialDisposable serialDisposable = new SerialDisposable();

    private void SubcsribeToSongChange(bool afterUnload = false)
    {
      if (AssociatedObject.DataContext != null)
      {
        serialDisposable = new SerialDisposable();

        if (AssociatedObject.DataContext is LRCFileViewModel lRCFileViewModel)
        {
          serialDisposable.Disposable = lRCFileViewModel.ActualLineChanged.Subscribe(OnLineChanged);
        }
        else if (AssociatedObject.DataContext is LRCCreatorViewModel creatorViewModel)
        {
          serialDisposable.Disposable = creatorViewModel.ObservePropertyChange(x => x.ActualLine)
            .ObserveOn(Application.Current.Dispatcher)
            .Subscribe((x) =>
            {
              if (x != null)
              {
                OnLineChanged(creatorViewModel.Lines.IndexOf(x));
              }
            });
        }
      }
    }

    private Decorator border;
    private ScrollViewer scrollViewer;
    private void OnLineChanged(int lineIndex)
    {
      try
      {
        Application.Current?.Dispatcher?.Invoke(() =>
        {
          if (border == null)
          {
            border = VisualTreeHelper.GetChild(AssociatedObject, 0) as Decorator;
          }

          if (scrollViewer == null)
          {
            scrollViewer = border?.Child as ScrollViewer;
          }

          var scrollIndexOffset = (lineIndex - 1 < 0 ? 0 : lineIndex - 1) * StepSize;

          if (scrollViewer != null)
          {
            var diff = Math.Abs(scrollViewer.VerticalOffset - scrollIndexOffset);

            if (diff > StepSize * 10 || wasUnloaded)
            {
              scrollViewer.ScrollToVerticalOffset(scrollIndexOffset);
              wasUnloaded = false;
            }
            else
            {
              DoubleAnimation verticalAnimation = new DoubleAnimation();
              Storyboard storyboard = new Storyboard();

              //verticalAnimation.EasingFunction = new SineEase() { EasingMode = EasingMode.EaseOut };
              storyboard.SpeedRatio = 0.95;
              storyboard.AccelerationRatio = 0.2;
              storyboard.DecelerationRatio = 0.8;

              verticalAnimation.From = scrollViewer.VerticalOffset;
              verticalAnimation.To = scrollIndexOffset;
              verticalAnimation.Duration = new Duration(AnimationTime);

              storyboard.Children.Add(verticalAnimation);

              Storyboard.SetTarget(verticalAnimation, scrollViewer);
              Storyboard.SetTargetProperty(verticalAnimation, new PropertyPath(ScrollAnimationBehavior.VerticalOffsetProperty));
              storyboard.Begin();
            }
          }
        });
      }
      catch (Exception ex)
      {
        Console.WriteLine(ex);
      }
    }
  }
}