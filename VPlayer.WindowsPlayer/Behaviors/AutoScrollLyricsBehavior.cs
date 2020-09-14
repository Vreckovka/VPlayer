using System;
using System.Reactive.Disposables;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Ninject;
using VPlayer.Core.ViewModels;
using VPlayer.Player.ViewModels;

namespace VPlayer.Player.Behaviors
{
  public class AutoScrollLyricsBehavior : System.Windows.Interactivity.Behavior<ListView>
  {
    public double StepSize { get; set; } = 1;

    protected override void OnAttached()
    {
      base.OnAttached();

      AssociatedObject.Loaded += AssociatedObject_Loaded;
      AssociatedObject.DataContextChanged += AssociatedObject_DataContextChanged;
    }

    private void AssociatedObject_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
      Application.Current.Dispatcher.Invoke(() =>
      {
        // Get the border of the listview (first child of a listview)
        Decorator border = VisualTreeHelper.GetChild(AssociatedObject, 0) as Decorator;

        // Get scrollviewer
        ScrollViewer scrollViewer = border?.Child as ScrollViewer;

        scrollViewer?.ScrollToTop();
      });

      SubcsribeToSongChange();
    }

    protected override void OnDetaching()
    {
      base.OnDetaching();

      AssociatedObject.Loaded -= AssociatedObject_Loaded;
      AssociatedObject.DataContextChanged -= AssociatedObject_DataContextChanged;
      serialDisposable.Dispose();
    }

    private void AssociatedObject_Loaded(object sender, RoutedEventArgs e)
    {
      SubcsribeToSongChange();
    }


    private SerialDisposable serialDisposable = new SerialDisposable();
    private void SubcsribeToSongChange()
    {
      if (AssociatedObject.DataContext != null)
      {
        if (AssociatedObject.DataContext is LRCFileViewModel lRCFileViewModel)
        {
          serialDisposable.Disposable = lRCFileViewModel.ActualLineChanged.Subscribe(OnSongChanged);
        }
      }
    }

    DispatcherTimer scrollTimer = new DispatcherTimer();
    double animationLengthMiliseconds = 3500;

    private void OnSongChanged(int songInPlayListIndex)
    {
      try
      {
        Application.Current.Dispatcher.Invoke(() =>
        {
          // Get the border of the listview (first child of a listview)
          Decorator border = VisualTreeHelper.GetChild(AssociatedObject, 0) as Decorator;

          // Get scrollviewer
          ScrollViewer scrollViewer = border?.Child as ScrollViewer;


          var scrollIndexOffset = (songInPlayListIndex - 1 < 0 ? 0 : songInPlayListIndex - 1) * StepSize;


          if (scrollViewer != null)
          {
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
                value = value + change > scrollIndexOffset ? scrollIndexOffset : value + change;

                scrollViewer.ScrollToVerticalOffset(value);

                if ((scrollViewer.VerticalOffset >= scrollIndexOffset && isNewIndexGreater) || (scrollViewer.VerticalOffset <= scrollIndexOffset && !isNewIndexGreater))
                {
                  scrollTimer.Stop();
                }

                if (scrollViewer.VerticalOffset == scrollViewer.ScrollableHeight)
                {
                  scrollTimer.Stop();
                }
              };
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