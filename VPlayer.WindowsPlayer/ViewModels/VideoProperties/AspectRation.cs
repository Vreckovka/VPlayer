using System;
using System.Collections.Generic;
using System.Text;
using LibVLCSharp.Shared.Structures;
using VCore.Annotations;

namespace VPlayer.WindowsPlayer.ViewModels.VideoProperties
{
  public class AspectRatioViewModel : VideoProperty
  {
    private readonly string aspectRatio;

    public AspectRatioViewModel([NotNull] string aspectRatio)
    {
      this.aspectRatio = aspectRatio ?? throw new ArgumentNullException(nameof(aspectRatio));
    }

    public override string Description => aspectRatio;
  }
}
