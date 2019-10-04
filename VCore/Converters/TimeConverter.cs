using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using VCore.Behaviors.Text;

namespace VCore.Converters
{
  public enum TimeType
  {
    Second,
    Minutes,
    Hours
  }
  public class TimeConverter : BaseConverter
  {
    #region TimeType

    public TimeType TimeType { get; set; }

    #endregion

    public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value != null)
      {
        if (value is TimeSpan)
        {
          return null;
        }
        else
        {
          var time = System.Convert.ToDouble(value);

          switch (TimeType)
          {
            case TimeType.Second:
              return TimeSpan.FromSeconds(time);
            case TimeType.Minutes:
              return TimeSpan.FromMinutes(time);
            case TimeType.Hours:
              return TimeSpan.FromHours(time);
            default:
              throw new ArgumentOutOfRangeException();
          }
        }
      }
      else
        return null;
    }
  }
}
