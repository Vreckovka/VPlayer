using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace VVLC.Providers
{
  public static class SubtitleAppearance
  {
    public const string Font = "Subtitle font";
    public const string Color = "Subtitle color (#RRGGBB)";
    public const string Size = "Subtitle size (pixels, 0 = auto)";
    public const string Background = "Subtitle background (#RRGGBB)";
    public const string BackgroundOpacity = "Subtitle background opacity (0-255)";

    public static IReadOnlyDictionary<string, string> Defaults { get; } = new Dictionary<string, string>
    {
      [Font] = "Arial",
      [Color] = "#FFFFFF",
      [Size] = "0",
      [Background] = "#000000",
      [BackgroundOpacity] = "150"
    };

    public static IEnumerable<string> Options(Func<string, string> getSetting)
    {
      var font = getSetting?.Invoke(Font)?.Trim();
      if (string.IsNullOrEmpty(font) || font.Length > 128 || font.Any(char.IsControl)) font = Defaults[Font];
      yield return "--freetype-font=" + font;
      yield return "--freetype-color=" + ColorValue(getSetting?.Invoke(Color), 0xffffff);
      string size = IntegerValue(getSetting?.Invoke(Size), 0, 4096);
      yield return "--freetype-fontsize=" + size;
      yield return size == "0" ? "--freetype-rel-fontsize=22" : "--freetype-rel-fontsize=0";
      yield return "--freetype-background-color=" + ColorValue(getSetting?.Invoke(Background), 0);
      yield return "--freetype-background-opacity=" + IntegerValue(getSetting?.Invoke(BackgroundOpacity), 150, 255);
    }

    private static string ColorValue(string value, int fallback)
    {
      value = value?.Trim().TrimStart('#');
      return (value?.Length == 6 && int.TryParse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int color)
        ? color : fallback).ToString(CultureInfo.InvariantCulture);
    }

    private static string IntegerValue(string value, int fallback, int maximum)
      => (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int number) &&
          number >= 0 && number <= maximum ? number : fallback).ToString(CultureInfo.InvariantCulture);
  }
}
