using System;
using System.Collections.Generic;
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
    public int? DecodeWidth { get; set; }
    public int? DecodeHeight { get; set; }

    public static Dictionary<string, List<CacheImageConverter>> AllConverters = new Dictionary<string, List<CacheImageConverter>>();
    string lastLoadedImagePath;
    BitmapImage lastLoadedImage;

    public CacheImageConverter()
    {
      //AllConverters.Add(null, this);
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
      return this;
    }

    #region Convert

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
        path = PlayableViewModelWithThumbnail<AlbumViewModel, Album>.GetEmptyImage();
      }

      if (!System.IO.File.Exists(path))
      {
        path = PlayableViewModelWithThumbnail<AlbumViewModel, Album>.GetEmptyImage();
      }

      if (lastLoadedImagePath == path)
      {
        return lastLoadedImage;
      }

      var bitmapImage = new BitmapImage();
      bitmapImage.BeginInit();
      bitmapImage.StreamSource = new FileStream(path, FileMode.Open, FileAccess.Read);

      if (int.TryParse(parameter?.ToString(), out var pixelSize) || DecodeWidth != null || DecodeHeight != null)
      {
        if (pixelSize > 0)
          bitmapImage.DecodePixelWidth = pixelSize;

        if (pixelSize > 0)
          bitmapImage.DecodePixelHeight = pixelSize;

        if(DecodeWidth != null)
          bitmapImage.DecodePixelWidth = DecodeWidth.Value;

        if(DecodeHeight != null)
          bitmapImage.DecodePixelHeight = DecodeHeight.Value;
      }

      bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
      bitmapImage.EndInit();
      bitmapImage.StreamSource.Dispose();

      lastLoadedImagePath = path;
      lastLoadedImage = bitmapImage;



      return bitmapImage;
    }

    #endregion

    #region RefreshDictionary

    public static void RefreshDictionary(string imagePath)
    {
      if (AllConverters.TryGetValue(imagePath, out var converters))
      {
        foreach (var converter in converters)
        {
          converter.lastLoadedImage = null;
          converter.lastLoadedImagePath = null;
        }

        AllConverters[imagePath] = null;
      }
    }

    #endregion

    private void AddConverterToDictionary()
    {

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