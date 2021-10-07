using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows;
using VCore.Converters;

namespace VPlayer.WindowsPlayer.Converters
{
  public class MarginToMatchChromeHeightValueConverter : BaseConverter
  {
    public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (double.TryParse(value?.ToString(), out var doubleValue))
      {
        return new Thickness(0, -doubleValue, 0, 0);
      }

      return value;
    }
  }
}
