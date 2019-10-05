using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interactivity;
using System.Windows.Media;
using System.Windows.Media.Animation;
using FlowDirection = System.Windows.FlowDirection;

namespace VCore.Behaviors.Text
{
  public class PanningTextWithFontSizeBehavior : Behavior<TextBlock>
  {
    #region Fields

    private Duration fontSizeAnimationDuration;
    private double originalFontSize;
    private Thickness originalMargin;
    private double thicknessOffset = 10;

    #endregion Fields

    #region Properties

    #region BiggerFontSize

    public static readonly DependencyProperty BiggerFontSizeProperty =
      DependencyProperty.Register(
        nameof(BiggerFontSize),
        typeof(double),
        typeof(PanningTextWithFontSizeBehavior),
        new PropertyMetadata(null));

    public double BiggerFontSize
    {
      get { return (double)GetValue(BiggerFontSizeProperty); }
      set { SetValue(BiggerFontSizeProperty, value); }
    }

    #endregion BiggerFontSize

    #region Container

    public static readonly DependencyProperty ContainerProperty =
      DependencyProperty.Register(
        nameof(Container),
        typeof(FrameworkElement),
        typeof(PanningTextWithFontSizeBehavior),
        new PropertyMetadata(null));

    public FrameworkElement Container
    {
      get { return (FrameworkElement)GetValue(ContainerProperty); }
      set { SetValue(ContainerProperty, value); }
    }

    #endregion Container

    #region Container

    public static readonly DependencyProperty ContainerSizeProperty =
      DependencyProperty.Register(
        nameof(ContainerSize),
        typeof(double?),
        typeof(PanningTextWithFontSizeBehavior),
        new PropertyMetadata(null));

    public double? ContainerSize
    {
      get { return (double?)GetValue(ContainerSizeProperty); }
      set { SetValue(ContainerSizeProperty, value); }
    }

    #endregion Container

    #region IgnoreSizeAnimation

    public static readonly DependencyProperty IgnoreSizeAnimationProperty =
      DependencyProperty.Register(
        nameof(IgnoreSizeAnimation),
        typeof(bool),
        typeof(PanningTextWithFontSizeBehavior),
        new PropertyMetadata(false, (x, y) =>
        {
          if ((bool)y.NewValue)
          {
            ((PanningTextWithFontSizeBehavior)x).AssociatedObject?.BeginAnimation(TextBlock.FontSizeProperty, null);
          }
        }));

    public bool IgnoreSizeAnimation
    {
      get { return (bool)GetValue(IgnoreSizeAnimationProperty); }
      set { SetValue(IgnoreSizeAnimationProperty, value); }
    }

    #endregion IgnoreSizeAnimation

    #region IsMouseOverRelativeToContainer

    public bool IsMouseOverRelativeToContainer { get; set; }

    #endregion IsMouseOverRelativeToContainer

    #endregion Properties

    #region OnAttached

    protected override void OnAttached()
    {
      base.OnAttached();

      AssociatedObject.Loaded += AssociatedObject_Loaded;
    }

    #region AssociatedObject_Loaded

    private void AssociatedObject_Loaded(object sender, RoutedEventArgs e)
    {
      if (IsMouseOverRelativeToContainer)
      {
        Container.MouseEnter += MouseEnter;
        Container.MouseLeave += MouseLeave;
      }
      else
      {
        AssociatedObject.MouseEnter += MouseEnter;
        AssociatedObject.MouseLeave += MouseLeave;
      }

      originalFontSize = AssociatedObject.FontSize;
      originalMargin = AssociatedObject.Margin;

      fontSizeAnimationDuration = new Duration(TimeSpan.FromSeconds(0.25));
    }

    #endregion AssociatedObject_Loaded

    #endregion OnAttached

    #region MouseLeave

    private void MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
      AssociatedObject.BeginAnimation(TextBlock.MarginProperty, null);

      if (!IgnoreSizeAnimation)
      {
        DoFontSizeAnimation(BiggerFontSize, originalFontSize);
      }
    }

    #endregion MouseLeave

    #region MouseEnter

    private void MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
      if (!IgnoreSizeAnimation)
      {
        DoFontSizeAnimation(originalFontSize, BiggerFontSize);
      }

      DoPanning();
    }

    #endregion MouseEnter

    #region DoFontSizeAnimation

    private void DoFontSizeAnimation(double fromFontSize, double toFontSize)
    {
      AssociatedObject.BeginAnimation(TextBlock.FontSizeProperty, null);
      var fontAnimation = new DoubleAnimation(fromFontSize, toFontSize, fontSizeAnimationDuration);
      AssociatedObject.BeginAnimation(TextBlock.FontSizeProperty, fontAnimation);
    }

    #endregion DoFontSizeAnimation

    #region DoPanning

    private void DoPanning()
    {
      AssociatedObject.BeginAnimation(TextBlock.MarginProperty, null);

      Size desiredSize = MeasureTextSize(AssociatedObject, BiggerFontSize);

      double containerSize = 0;

      if (ContainerSize == null)
        containerSize = Container.ActualWidth;
      else
        containerSize = ContainerSize.Value;

      if (desiredSize.Width > containerSize)
      {
        var thickness = new Thickness((containerSize - desiredSize.Width - thicknessOffset) + originalMargin.Left, originalMargin.Top, originalMargin.Right, originalMargin.Bottom);
        var thicknessAnimation = new ThicknessAnimation(thickness, GetPanningDuration(Container.ActualWidth, desiredSize.Width));
        thicknessAnimation.AutoReverse = true;
        thicknessAnimation.RepeatBehavior = RepeatBehavior.Forever;
        thicknessAnimation.AccelerationRatio = 0.5;
        thicknessAnimation.DecelerationRatio = 0.5;

        AssociatedObject.BeginAnimation(TextBlock.MarginProperty, thicknessAnimation);
      }
    }

    #endregion DoPanning

    #region GetPanningDuration

    private Duration GetPanningDuration(double gridSize, double textSize)
    {
      double multiplicator = 1.26;

      if ((textSize * multiplicator) > gridSize && gridSize != 0)
      {
        double perc = ((textSize * multiplicator) / gridSize) - 1;

        if (perc < 0.25)
          return new Duration(TimeSpan.FromSeconds(2.5));
        else if (perc < 1)
          return new Duration(TimeSpan.FromSeconds((perc * 10)));
        else
          return new Duration(TimeSpan.FromSeconds((perc * 10) / (perc)));
      }
      else
        return new Duration(TimeSpan.FromSeconds(0));
    }

    #endregion GetPanningDuration

    #region MeasureTextSize

    public Size MeasureTextSize(TextBlock textBlock, double? desiredFontSize = null)
    {
      var fontSize = textBlock.FontSize;

      if (desiredFontSize.HasValue)
      {
        fontSize = desiredFontSize.Value;
      }

      FormattedText formattedText = new FormattedText(textBlock.Text, CultureInfo.GetCultureInfo("en-us"),
        FlowDirection.LeftToRight, new Typeface(textBlock.FontFamily, textBlock.FontStyle, textBlock.FontWeight, textBlock.FontStretch),
        fontSize, Brushes.Black,
        VisualTreeHelper.GetDpi(AssociatedObject).PixelsPerDip);

      return new Size(formattedText.Width, formattedText.Height);
    }

    #endregion MeasureTextSize
  }
}