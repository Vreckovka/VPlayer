using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using VCore.Converters;

namespace VPlayer.Library.Converters
{
  public class MultiplyValueConverter : BaseConverter
  {
    public override object Convert(
      object value,
      Type targetType,
      object parameter,
      CultureInfo culture)
    {
      if(double.TryParse(value.ToString(), out var count) && double.TryParse(parameter.ToString(), out var parameterDouble))
      {
        return count * parameterDouble;
      }

      return 0;
    }
  }
}
