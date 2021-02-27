using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using VPlayer.Core.ViewModels;
using VPlayer.Core.ViewModels.TvShow;

namespace VPlayer.WindowsPlayer.TemplateSelectors
{
  public class WindowsPlayerItemsTemplateSelector : DataTemplateSelector
  {
    public DataTemplate SongDataTemplate { get; set; }
    public DataTemplate VideoDataTemplate { get; set; }
    public override DataTemplate SelectTemplate(object item, DependencyObject container)
    {
      if(item is SongInPlayListViewModel )
      {
        return SongDataTemplate;
      }
      else if(item is TvShowEpisodeInPlaylistViewModel)
      {
        return VideoDataTemplate;
      }


      return new DataTemplate();
    }
  }
}
