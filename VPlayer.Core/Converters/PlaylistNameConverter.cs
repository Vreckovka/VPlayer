using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;
using VPlayer.AudioStorage.DomainClasses;

namespace VPlayer.Core.Converters
{
  public class PlaylistNameConverter : MarkupExtension, IValueConverter
  {
    private IPlaylist savedPlaylist;
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
      if (value is IPlaylist playlist)
      {
        savedPlaylist = playlist;

        if (string.IsNullOrEmpty(playlist.Name))
        {
          var stringC = playlist.Created?.ToString("dddd,dd MMMM yyyy HH:mm");

          return $"GENERATED: {stringC}";
        }

        return playlist.Name;
      }

      return null;
    }

    public object ConvertBack(
      object value,
      Type targetType,
      object parameter,
      CultureInfo culture)
    {
      if (!string.IsNullOrEmpty(value?.ToString()))
      {
        if (savedPlaylist != null)
        {
          savedPlaylist.Name = value.ToString();
        }

        return new object[]
        {
          value,
          null
        };
      }

      return null;
    }
  }
}