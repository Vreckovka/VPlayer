using System.Windows;
using System.Windows.Controls;
using VPlayer.AudioStorage.InfoDownloader.LRC;
using VPlayer.Core.ViewModels;

namespace VPlayer.WindowsPlayer.TemplateSelectors
{
  public class LyricsProviderTemplateSelector : DataTemplateSelector
  {
    public DataTemplate GoogleDataTemplate { get; set; }
    public DataTemplate LocalDataTemplate { get; set; }

    public override DataTemplate SelectTemplate(object item, DependencyObject container)
    {
      if (item is SongInPlayList songInPlaylist)
      {
        switch (songInPlaylist?.LRCFile?.Provider)
        {
          case LRCProviders.Google:
            return GoogleDataTemplate;
          case LRCProviders.Local:
            return LocalDataTemplate;
        }
      }

      return new DataTemplate();
    }
  }
}