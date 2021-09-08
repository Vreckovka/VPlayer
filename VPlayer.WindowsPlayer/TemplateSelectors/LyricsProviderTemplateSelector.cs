using System.Windows;
using System.Windows.Controls;
using VPlayer.AudioStorage.InfoDownloader.LRC;
using VPlayer.Core.ViewModels;

namespace VPlayer.WindowsPlayer.TemplateSelectors
{
  public class LyricsProviderTemplateSelector : DataTemplateSelector
  {
    public DataTemplate PCloudDataTemplate { get; set; }
    public DataTemplate GoogleDataTemplate { get; set; }
    public DataTemplate LocalDataTemplate { get; set; }

    public override DataTemplate SelectTemplate(object item, DependencyObject container)
    {
      if (item is LRCProviders lRCProvider)
      {
        switch (lRCProvider)
        {
          case LRCProviders.Google:
            return GoogleDataTemplate;
          case LRCProviders.Local:
            return LocalDataTemplate;
          case LRCProviders.PCloud:
            return PCloudDataTemplate;
        }
      }

      return new DataTemplate();
    }
  }
}