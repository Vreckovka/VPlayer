using System;
using System.Globalization;
using System.IO;
using VCore.WPF.Converters;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.Core.ViewModels;
using VPlayer.Core.ViewModels.Albums;

namespace VPlayer.Core.Converters
{
  public class VPlayerCacheImageConverter : CacheImageConverter
  {
    protected override Stream GetEmptyImage()
    {
      return new FileStream(PlayableViewModelWithThumbnail<AlbumViewModel, Album>.GetEmptyImage(), FileMode.Open, FileAccess.Read);
    }
  }
}