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
  public class CacheImageParameters
  {
    public int? DecodeHeight { get; set; }
    public int? DecodeWidth { get; set; }
  }

  public class CacheImageConverter : MarkupExtension, IValueConverter
  {
    public int? DecodeWidth { get; set; }
    public int? DecodeHeight { get; set; }

    public static Dictionary<string, List<CacheImageConverter>> AllConverters = new Dictionary<string, List<CacheImageConverter>>();
    string lastLoadedImagePath;
    BitmapImage lastLoadedImage;

    public CacheImageConverter()
    {

    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
      return this;
    }

    protected BitmapImage GetBitmapImage(object value, object parameter)
    {
      if (value is BitmapImage bitmapImage1)
      {
        return bitmapImage1;
      }

      string path = "";


      if (value != DependencyProperty.UnsetValue && value != null)
      {
        path = value.ToString()?.Replace("file:///", "");
      }
      else
      {
        path = PlayableViewModelWithThumbnail<AlbumViewModel, Album>.GetEmptyImage();
      }

      if (!File.Exists(path))
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

      DecodePixelSize(parameter, bitmapImage);

      bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
      bitmapImage.EndInit();
      bitmapImage.StreamSource.Dispose();

      lastLoadedImagePath = path;
      lastLoadedImage = bitmapImage;

      if (!AllConverters.ContainsKey(path))
      {
        AllConverters.Add(path, new List<CacheImageConverter>()
        {
          this
        });
      }
      else
      {
        var allConvertes = AllConverters[path];

        if (allConvertes == null)
        {
          allConvertes = new List<CacheImageConverter>()
          {
            this
          };
        }
        else if (!allConvertes.Contains(this))
        {
          allConvertes.Add(this);
        }

        AllConverters[path] = allConvertes;
      }


      return bitmapImage;
    }

    #region Convert

    public virtual object Convert(
      object value,
      Type targetType,
      object parameter,
      CultureInfo culture)
    {
      if (value == null)
      {
        return null;
      }

      if (parameter is CacheImageParameters imageParameters)
      {
        DecodeHeight = imageParameters.DecodeHeight;
        DecodeWidth = imageParameters.DecodeWidth;
      }

      var stringValue = value?.ToString();

      if (stringValue != null && stringValue.Contains("http"))
      {
        return stringValue;
      }


      return GetBitmapImage(value, parameter);
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

    protected virtual void DecodePixelSize(object parameter, BitmapImage bitmapImage)
    {
      if (int.TryParse(parameter?.ToString(), out var pixelSize) || DecodeWidth != null || DecodeHeight != null)
      {
        if (pixelSize > 0)
          bitmapImage.DecodePixelWidth = pixelSize;

        if (pixelSize > 0)
          bitmapImage.DecodePixelHeight = pixelSize;

        if (DecodeWidth != null)
          bitmapImage.DecodePixelWidth = DecodeWidth.Value;

        if (DecodeHeight != null)
          bitmapImage.DecodePixelHeight = DecodeHeight.Value;
      }
    }

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