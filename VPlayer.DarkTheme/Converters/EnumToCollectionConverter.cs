﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;
using VCore.Standard.Helpers;

namespace VPlayer.Library
{
  [ValueConversion(typeof(Enum), typeof(IEnumerable<EnumHelper.ValueDescription>))]
  public class EnumToCollectionConverter : MarkupExtension, IValueConverter
  {
    public object Convert(
      object value,
      Type targetType,
      object parameter,
      CultureInfo culture)
    {
      return EnumHelper.GetAllValuesAndDescriptions(value.GetType());
    }

    public object ConvertBack(
      object value,
      Type targetType,
      object parameter,
      CultureInfo culture)
    {
      return null;
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
      return this;
    }
  }
}