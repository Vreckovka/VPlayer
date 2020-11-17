using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media.Imaging;
using VCore.Converters;

namespace VPlayer.Library
{
  #region ScrollingTextDurationConverter

  public class ScrollingTextDurationConverter : MarkupExtension, IMultiValueConverter
  {
    public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value[0] != DependencyProperty.UnsetValue && value[1] != DependencyProperty.UnsetValue)
      {
        double multiplicator = 1.26;
        double gridSize = System.Convert.ToDouble(value[0]);
        double textSize = System.Convert.ToDouble(value[1]);

        if ((textSize * multiplicator) > gridSize && gridSize != 0)
        {
          double perc = ((textSize * multiplicator) / gridSize) - 1;

          if (perc < 0.25)
            return new Duration(TimeSpan.FromSeconds(2.5));
          else if (perc < 1)
            return new Duration(TimeSpan.FromSeconds((perc * 10)));
          else
            return new Duration(TimeSpan.FromSeconds((perc * 10) / (perc)));
        }
        else
          return new Duration(TimeSpan.FromSeconds(0));
      }

      return null;
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

  #endregion

  #region CacheImageConverter

  #endregion

  #region ScrollingTextMarginConverter

  public class ScrollingTextMarginConverter : MarkupExtension, IMultiValueConverter
  {
    public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value[0] != DependencyProperty.UnsetValue && value[1] != DependencyProperty.UnsetValue)
      {
        double multiplicator = 1.26;
        double gridSize = System.Convert.ToDouble(value[0]);
        double textSize = System.Convert.ToDouble(value[1]);

        if (gridSize > (textSize * multiplicator))
          return new Thickness(0);
        else
        {
          double newTextSize = textSize * (multiplicator + 0.02);
          var newThickness = new Thickness((gridSize - (newTextSize)), 0, 0, 0);
          Console.WriteLine(newThickness);
          return newThickness;

        }
      }

      return null;
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

  #endregion

  #region IsBiggerConverter

  #endregion

  #region ImageLazyLoadingConverter

  public class ImageLazyLoadingConverter : MarkupExtension, IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value != null)
      {
        using (var ms = new System.IO.MemoryStream((byte[])value))
        {
          var image = new BitmapImage();
          image.DecodePixelHeight = 1;
          image.DecodePixelWidth = 1;
          image.BeginInit();
          image.CacheOption = BitmapCacheOption.OnLoad;
          image.StreamSource = ms;
          image.EndInit();
          image.Freeze();
          return image;
        }
      }

      return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
      return this;
    }
  }

  #endregion

  #region HashConverter

  public class HashConverter : BaseConverter
  {
    public override object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      int? subString = null;
      if (parameter is int it)
      {
        subString = it;
      }

      if (subString != null)
      {
        return GetHashString(value.ToString()).Substring(0, subString.Value);
      }
      else
      {
        return GetHashString(value.ToString());
      }

    }

    public static byte[] GetHash(string inputString)
    {
      using (HashAlgorithm algorithm = SHA256.Create())
        return algorithm.ComputeHash(Encoding.UTF8.GetBytes(inputString));
    }

    public static string GetHashString(string inputString)
    {
      StringBuilder sb = new StringBuilder();
      foreach (byte b in GetHash(inputString))
        sb.Append(b.ToString("X2"));

      return sb.ToString();
    }


  }

  #endregion

}
