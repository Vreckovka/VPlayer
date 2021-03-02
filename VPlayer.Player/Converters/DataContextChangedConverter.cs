using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace VPlayer.Player.Converters
{
  public class DataContextChangedConverter : IValueConverter
  {
    object lastDataContext;
    public object Convert(
      object value,
      Type targetType,
      object parameter,
      CultureInfo culture)
    {
      if(value != lastDataContext)
      {
        lastDataContext = value;
        return true;
      }

      return false;
    }

    public object ConvertBack(
      object value,
      Type targetType,
      object parameter,
      CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}
