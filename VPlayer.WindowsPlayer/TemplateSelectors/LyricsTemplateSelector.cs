using System.Windows;
using System.Windows.Controls;
using VPlayer.Core.ViewModels;
using VPlayer.Core.ViewModels.SoundItems;
using VPlayer.Core.ViewModels.SoundItems.LRCCreators;

namespace VPlayer.WindowsPlayer.TemplateSelectors
{
  public class LyricsTemplateSelector : DataTemplateSelector
  {
    public DataTemplate LyricsDataTemplate { get; set; }
    public DataTemplate SyncLyricsDataTemplate { get; set; }
    public DataTemplate DisabledLyrics { get; set; }
    public DataTemplate CreatingLRC { get; set; }

    public override DataTemplate SelectTemplate(object item, DependencyObject container)
    {
      if (item is LRCFileViewModel)
      {
        return SyncLyricsDataTemplate;
      }
      else if(item is string stringValue)
      {
        return LyricsDataTemplate;
      }
      else if(item is bool boolValue && !boolValue)
      {
        return DisabledLyrics;
      }
      else if (item is LRCCreatorViewModel lRCCreatorViewModel)
      {
        return CreatingLRC;
      }

      return new DataTemplate();
    }
  }
}

