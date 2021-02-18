using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media.Imaging;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.Core.ViewModels;
using VPlayer.Core.ViewModels.Albums;

namespace VPlayer.Library
{
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
        path = PlayableViewModelWithThumbnail<AlbumViewModel,Album>.GetEmptyImage();
      }

      if (!System.IO.File.Exists(path))
      {
        path = PlayableViewModelWithThumbnail<AlbumViewModel,Album>.GetEmptyImage();
      }

      var bitmapImage = new BitmapImage();
      bitmapImage.BeginInit();
      bitmapImage.StreamSource = new FileStream(path, FileMode.Open, FileAccess.Read);

      if (parameter is int pixelWidth)
      {
        bitmapImage.DecodePixelWidth = pixelWidth;
      }

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
}