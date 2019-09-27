using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace VCore.AttachedProperties
{
  public class AttachedProperty 
  {
    public static readonly DependencyProperty RememberInitialWidthProperty = DependencyProperty.RegisterAttached(
      "RememberInitialWidth",
      typeof(bool),
      typeof(AttachedProperty),
      new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender, OnRememberInitialSize)
    );

    private static void OnRememberInitialSize(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
    {
      if ((bool) dependencyPropertyChangedEventArgs.NewValue)
      {
        ((FrameworkElement)dependencyObject).Loaded += AttachedProperty_Loaded;
      }
    }

    private static void AttachedProperty_Loaded(object sender, RoutedEventArgs e)
    {
      var frameworkElement = (FrameworkElement) sender;
      var isBubbleSource = GetRememberInitialWidth(frameworkElement);
      if (isBubbleSource)
      {
        frameworkElement.Width = frameworkElement.ActualWidth;
      }
    }

    public static void SetRememberInitialWidth(UIElement element, bool value)
    {
      element.SetValue(RememberInitialWidthProperty, value);
    }
    public static bool GetRememberInitialWidth(UIElement element)
    {
      return (bool)element.GetValue(RememberInitialWidthProperty);
    }
  }
}
