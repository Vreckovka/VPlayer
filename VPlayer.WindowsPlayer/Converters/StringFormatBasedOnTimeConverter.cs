using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using VCore.WPF.Converters;

namespace VPlayer.WindowsPlayer.Converters
{
    public class StringFormatBasedOnTimeConverter : BaseMultiValueConverter
  {
    public override object Convert(
      object[] values,
      Type targetType,
      object parameter,
      CultureInfo culture)
    {
      if(values.Length > 1 && values[0] is int lengthInt && values[1] is double perc)
      {
        var length = TimeSpan.FromSeconds(lengthInt);
        string result;

        if (length < TimeSpan.FromMinutes(15))
        {
          result = perc.ToString("N0");
        }
        else if(length < TimeSpan.FromMinutes(90))
        {
          result = perc.ToString("N1");
        }
        else
        {
          result = perc.ToString("N2");
        }

        return $"({result}%)";
      }

      return null;
    }
  }
}
