using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VVLC.Providers;
using Xunit;

namespace VPlayer.Tests
{
  public class SubtitleStyleTests
  {
    [Fact]
    public void PersonalAppearanceSetsFontSizeTextAndBackground()
    {
      var settings = new Dictionary<string, string>
      {
        [SubtitleAppearance.Font] = "Segoe UI",
        [SubtitleAppearance.Size] = "32",
        [SubtitleAppearance.Color] = "#FFE600",
        [SubtitleAppearance.Background] = "#651489",
        [SubtitleAppearance.BackgroundOpacity] = "255"
      };
      var options = SubtitleAppearance.Options(key => settings[key]).ToArray();
      Assert.Contains("--freetype-font=Segoe UI", options);
      Assert.Contains("--freetype-fontsize=32", options);
      Assert.Contains("--freetype-rel-fontsize=0", options);
      Assert.Contains("--freetype-color=16770560", options);
      Assert.Contains("--freetype-background-color=6624393", options);
      Assert.Contains("--freetype-background-opacity=255", options);
    }

    [Fact]
    public void InvalidSettingsFallBackToReadableDefaults()
    {
      var options = SubtitleAppearance.Options(_ => "bad\nsetting").ToArray();
      Assert.Contains("--freetype-font=Arial", options);
      Assert.Contains("--freetype-fontsize=0", options);
      Assert.Contains("--freetype-rel-fontsize=22", options);
      Assert.Contains("--freetype-color=16777215", options);
      Assert.Contains("--freetype-background-color=0", options);
      Assert.Contains("--freetype-background-opacity=150", options);
    }

    [Fact]
    public async Task ACancelledSelectionDoesNotStartTheConverter()
    {
      using var request = new CancellationTokenSource();
      request.Cancel();
      await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
        PlainSubtitleConverter.ConvertAsync("invalid-ffmpeg", "invalid-source", 0, "invalid-output", request.Token));
    }
  }
}
