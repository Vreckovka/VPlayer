using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using VPlayer.Core.ViewModels;
using VPlayer.Core.ViewModels.TvShows;
using VPlayer.IPTV.ViewModels;

namespace VPlayer.WindowsPlayer.TemplateSelectors
{
  public class WindowsPlayerItemsTemplateSelector : DataTemplateSelector
  {
    public DataTemplate SongDataTemplate { get; set; }
    public DataTemplate TvShowEpisodeDataTemplate { get; set; }
    public DataTemplate VideoItemDataTemplate { get; set; }
    public DataTemplate TvChannelDataTemplate { get; set; }

    public override DataTemplate SelectTemplate(object item, DependencyObject container)
    {
      if(item is SoundItemInPlaylistViewModel )
      {
        return SongDataTemplate;
      }
      else if(item is TvShowEpisodeInPlaylistViewModel)
      {
        return TvShowEpisodeDataTemplate;
      }
      else if (item is VideoItemInPlaylistViewModel)
      {
        return VideoItemDataTemplate;
      }
      else if (item is TvItemInPlaylistItemViewModel)
      {
        return TvChannelDataTemplate;
      }


      return new DataTemplate();
    }
  }
}
