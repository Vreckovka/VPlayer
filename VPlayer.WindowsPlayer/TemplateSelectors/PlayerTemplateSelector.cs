using System.Windows;
using System.Windows.Controls;
using VPlayer.WindowsPlayer.ViewModels;

namespace VPlayer.WindowsPlayer.TemplateSelectors
{
  public class PlayerTemplateSelector : DataTemplateSelector
  {
    public DataTemplate Music { get; set; }
    public DataTemplate Video { get; set; }

    public override DataTemplate SelectTemplate(object item, DependencyObject container)
    {
      if (item is MusicPlayerViewModel)
      {
        return Music;
      }
      else if (item is VideoPlayerViewModel)
      {
        return Video;
      }

      return new DataTemplate();
    }
  }
}