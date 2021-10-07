using System.Windows;
using System.Windows.Controls;
using VPlayer.Core.ViewModels.SoundItems;
using VPlayer.Core.ViewModels.TvShows;

namespace VPlayer.Core.TemplateSelectors
{
  public class PlaylistItemTypeTemplateSelector : DataTemplateSelector
  {
    public DataTemplate Sound { get; set; }
    public DataTemplate Video { get; set; }

    public override DataTemplate SelectTemplate(object item, DependencyObject container)
    {
      if (item is SoundItemInPlaylistViewModel)
      {
        return Sound;
      }
      else if (item is VideoItemInPlaylistViewModel)
      {
        return Video;
      }

      return new DataTemplate();
    }
  }
}
