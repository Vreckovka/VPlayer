using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;

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
      if (value != DependencyProperty.UnsetValue && value != null)
      {
        // do this so that the image file can be moved in the file system
        BitmapImage result = new BitmapImage();
        // Create new BitmapImage   
        Stream stream = new MemoryStream(); // Create new MemoryStream   
        var asd = value.ToString();
        var ddd = new Uri(value.ToString());
        Bitmap bitmap = new Bitmap(ddd.LocalPath);
        // Create new Bitmap (System.Drawing.Bitmap) from the existing image file (albumArtSource set to its path name)   
        bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
        // Save the loaded Bitmap into the MemoryStream - Png format was the only one I tried that didn't cause an error (tried Jpg, Bmp, MemoryBmp)   
        bitmap.Dispose(); // Dispose bitmap so it releases the source image file   
        result.BeginInit(); // Begin the BitmapImage's initialisation   
        result.StreamSource = stream;
        // Set the BitmapImage's StreamSource to the MemoryStream containing the image   
        result.EndInit(); // End the BitmapImage's initialisation   
                          // return result; // Finally, set the WPF Image component's source to the BitmapImage

      }

      return DependencyProperty.UnsetValue;
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
