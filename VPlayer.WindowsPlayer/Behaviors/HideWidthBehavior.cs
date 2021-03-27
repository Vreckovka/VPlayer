using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Microsoft.Xaml.Behaviors;
using VCore.Helpers;

namespace VPlayer.WindowsPlayer.Behaviors
{
  public class HideWidthBehavior : Behavior<FrameworkElement>
  {
    public double MinWidth { get; set; }
    public string ExecuteButtonName { get; set; }

    public string GridSplitterName { get; set; }
    public Duration Duration { get; set; } = new Duration(TimeSpan.FromSeconds(1));

    #region IsHidden

    public static readonly DependencyProperty IsHiddenProperty =
      DependencyProperty.Register(
        nameof(IsHidden),
        typeof(bool),
        typeof(HideWidthBehavior),
        new PropertyMetadata(null));

    public bool IsHidden
    {
      get { return (bool)GetValue(IsHiddenProperty); }
      set { SetValue(IsHiddenProperty, value); }
    }

    #endregion

    #region CanHide

    public static readonly DependencyProperty CanHideProperty =
      DependencyProperty.Register(
        nameof(CanHide),
        typeof(bool),
        typeof(HideWidthBehavior),
        new PropertyMetadata(true));

    public bool CanHide
    {
      get { return (bool)GetValue(CanHideProperty); }
      set { SetValue(CanHideProperty, value); }
    }

    #endregion

    private Button executeButton;

    protected override void OnAttached()
    {
      base.OnAttached();

      AssociatedObject.Loaded += AssociatedObject_Loaded;

     
    }

    private GridSplitter gridSplitter;
    private Grid parentGrid;
    private void AssociatedObject_Loaded(object sender, RoutedEventArgs e)
    {
      var grid = (Grid)AssociatedObject;
      parentGrid = (Grid)VisualTreeHelper.GetParent(grid);

      executeButton = parentGrid.FindChildByName<Button>(ExecuteButtonName);
      gridSplitter = parentGrid.FindChildByName<GridSplitter>(GridSplitterName);


      if (executeButton != null)
      {
        executeButton.Click += Button_Click;
      }
    }

    private double lastWidth;

    private bool isResizing;

    private void Button_Click(object sender, RoutedEventArgs e)
    {
      if (!isResizing)
      {
        IsHidden = !IsHidden;
        CanHide = !IsHidden;

        isResizing = true;

        if (gridSplitter != null)
        {
          parentGrid.ColumnDefinitions[2].Width = new GridLength(0, GridUnitType.Auto);
        }

        if (IsHidden)
        {
          lastWidth = AssociatedObject.ActualWidth;

          var doubleAnim = new DoubleAnimation(lastWidth, MinWidth, Duration);

          doubleAnim.FillBehavior = FillBehavior.Stop;

          doubleAnim.Completed += new EventHandler((x, y) => { DoubleAnim_Completed(MinWidth); });


          AssociatedObject.BeginAnimation(FrameworkElement.WidthProperty, doubleAnim);
        }
        else
        {
          var doubleAnim = new DoubleAnimation(MinWidth, lastWidth, Duration);
          doubleAnim.FillBehavior = FillBehavior.Stop;

          doubleAnim.Completed += new EventHandler((x, y) => { DoubleAnim_Completed(lastWidth); });

          AssociatedObject.BeginAnimation(FrameworkElement.WidthProperty, doubleAnim);
        }
      }
    }

    private void DoubleAnim_Completed(double width)
    {
      AssociatedObject.BeginAnimation(FrameworkElement.WidthProperty, null);

      if (width == MinWidth)
        AssociatedObject.Width = width;
      else
      {
        AssociatedObject.Width = double.NaN;
        parentGrid.ColumnDefinitions[2].Width = new GridLength(width);
      }

      isResizing = false;
    }


    protected override void OnDetaching()
    {
      AssociatedObject.Loaded -= AssociatedObject_Loaded;

      if (executeButton != null)
        executeButton.Click += Button_Click;
    }
  }
}
