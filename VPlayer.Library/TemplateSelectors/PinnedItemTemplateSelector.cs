using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using VPlayer.Home.ViewModels;
using VPlayer.Library.ViewModels;

namespace VPlayer.Home.TemplateSelectors
{
  public class PinnedItemTemplateSelector : DataTemplateSelector
  {
    public DataTemplate Playlist { get; set; }
    public DataTemplate Other { get; set; }
    public override DataTemplate SelectTemplate(object item, DependencyObject container)
    {
      if (item is PinnedItemViewModel viewModel)
      {
        return viewModel.ItemObject is IPlaylistViewModel ? Playlist : Other;
      }

      return Other;
    }
  }
}
