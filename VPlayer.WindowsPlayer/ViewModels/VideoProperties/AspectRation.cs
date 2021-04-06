using System;
using System.Collections.Generic;
using System.Text;
using LibVLCSharp.Shared.Structures;

namespace VPlayer.WindowsPlayer.ViewModels.VideoProperties
{
  public class AspectRatioViewModel : VideoProperty
  {
    private readonly string aspectRatio;

    public AspectRatioViewModel(string aspectRatio)
    {
      this.aspectRatio = aspectRatio ?? throw new ArgumentNullException(nameof(aspectRatio));

      Value = aspectRatio;
    }

    public override string Description => aspectRatio;

    public string Value { get; set; }

    public bool IsDefault { get; set; }

    public AspectRatioViewModel Copy()
    {
      return new AspectRatioViewModel(aspectRatio)
      {
        IsDefault = IsDefault,
        Value = Value
      };
    }
  }
}
