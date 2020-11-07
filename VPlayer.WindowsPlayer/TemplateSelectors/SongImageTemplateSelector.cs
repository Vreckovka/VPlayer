using System.Windows;
using System.Windows.Controls;

namespace VPlayer.WindowsPlayer.TemplateSelectors
{
  public class SongImageTemplateSelector : DataTemplateSelector
  {
    public DataTemplate AlbumDataTemplate { get; set; }
    public DataTemplate GifDataTemplate { get; set; }

    public override DataTemplate SelectTemplate(object item, DependencyObject container)
    {
      if (item is bool isGif )
      {
        if (isGif)
        {
          return GifDataTemplate;
        }
        else
        {
          return AlbumDataTemplate;
        }
      }

      return new DataTemplate();
    }
  }
}
