using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using VPlayer.Core.ViewModels;

namespace VPlayer.WindowsPlayer.TemplateSelectors
{
  public class LyricsTemplateSelector : DataTemplateSelector
  {
    public DataTemplate LyricsDataTemplate { get; set; }
    public DataTemplate SyncLyricsDataTemplate { get; set; }

    public override DataTemplate SelectTemplate(object item, DependencyObject container)
    {
      if (item is LRCFileViewModel)
      {
        return SyncLyricsDataTemplate;
      }
      else
      {
        return LyricsDataTemplate;
      }
    }
  }
}

