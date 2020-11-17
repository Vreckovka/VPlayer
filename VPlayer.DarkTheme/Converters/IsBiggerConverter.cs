using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace VPlayer.Library
{
  public class IsBiggerConverter : MarkupExtension, IMultiValueConverter
  {
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
      if (values.Length == 2 && values[0] != DependencyProperty.UnsetValue && values[1] != DependencyProperty.UnsetValue)
      {
        if (System.Convert.ToDouble(values[0]) > System.Convert.ToDouble(values[1]))
          return true;
      }
      else if (values[0] != DependencyProperty.UnsetValue && values.Length == 1)
      {
        if (System.Convert.ToDouble(values[0]) > 0)
          return true;
      }

      return false;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
      return null;
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
      return this;
    }
  }
}