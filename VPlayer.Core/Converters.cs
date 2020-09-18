using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.Core.ViewModels;

namespace VPlayer.Library
{
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

  public class CacheImageConverter : MarkupExtension, IValueConverter
  {
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
      return this;
    }

    public object Convert(
      object value,
      Type targetType,
      object parameter,
      CultureInfo culture)
    {
      if (value is BitmapImage bitmapImage1)
      {
        return bitmapImage1;
      }

      string path = "";

      if (value != DependencyProperty.UnsetValue && value != null)
      {
        path = value.ToString().Replace("file:///", "");
      }
      else
      {
        path = PlayableViewModelWithThumbnail<Album>.GetEmptyImage();
      }

      if (!System.IO.File.Exists(path))
      {
        path = PlayableViewModelWithThumbnail<Album>.GetEmptyImage();
      }

     

      var bitmapImage = new BitmapImage();
      bitmapImage.BeginInit();
      bitmapImage.StreamSource = new FileStream(path, FileMode.Open, FileAccess.Read);
      bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
      bitmapImage.EndInit();
      bitmapImage.StreamSource.Dispose();
      return bitmapImage;
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

  public class IsBiggerConverter : MarkupExtension, IMultiValueConverter
  {
    public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value[0] != DependencyProperty.UnsetValue && value[1] != DependencyProperty.UnsetValue)
        if (System.Convert.ToDouble(value[0]) > System.Convert.ToDouble(value[1]))
          return true;

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

}
