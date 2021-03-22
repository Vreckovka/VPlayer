using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;
using VCore.Converters;

namespace VPlayer.WindowsPlayer.Converters
{
  public class EmptyItemSourceToDisable : BaseConverter
  {
    public override object Convert(
      object value,
      Type targetType,
      object parameter,
      CultureInfo culture)
    {
      if (value is int enmerable)
      {
        return enmerable != 0;
      }

      return false;
    }
  }
}
