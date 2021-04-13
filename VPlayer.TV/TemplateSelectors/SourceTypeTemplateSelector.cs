using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using VPlayer.AudioStorage.DomainClasses.IPTV;
using VPlayer.IPTV.ViewModels;

namespace VPlayer.IPTV.TemplateSelectors
{
  public class SourceTypeTemplateSelector : DataTemplateSelector
  {
    public DataTemplate MP3U { get; set; }
    public DataTemplate Source { get; set; }
    public DataTemplate IPTVStalker { get; set; }

    public override DataTemplate SelectTemplate(object item, DependencyObject container)
    {
      if (item is TVSourceViewModel sourceViewModel)
      {
        switch (sourceViewModel.TvSourceType)
        {
          case TVSourceType.Source:
            return Source;
          case TVSourceType.M3U:
            return MP3U;
          case TVSourceType.IPTVStalker:
            return IPTVStalker;
          default:
            throw new ArgumentOutOfRangeException();
        }
      }

      return new DataTemplate();
    }
  }
}
