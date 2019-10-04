using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace VCore.Converters
{
  public class VisibilityConverter : BaseConverter
  {
    public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value is bool show)
      {
        if (show)
        {
          return Visibility.Visible;
        }
        else
          return Visibility.Hidden;
      }

      return Visibility.Collapsed;
    }
  }
}
