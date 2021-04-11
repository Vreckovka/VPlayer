using System.Windows;
using System.Windows.Controls;
using VCore.Standard.ViewModels.WindowsFile;
using VPlayer.UPnP.ViewModels.UPnP;
using VPlayer.UPnP.ViewModels.UPnP.TreeViewItems;

namespace VPlayer.Library.TemplateSelectors
{
  public class UPnPItemTemplateSelector : DataTemplateSelector
  {
    public DataTemplate FolderTemplate { get; set; }
    public DataTemplate FileTemplate { get; set; }

    public override DataTemplate SelectTemplate(object item, DependencyObject container)
    {
      if (item is UPnPContainerViewModel)
      {
        return FolderTemplate;
      }
      else if (item is UPnPItemViewModel)
      {
        return FileTemplate;
      }


      return new DataTemplate();
    }
  }
}