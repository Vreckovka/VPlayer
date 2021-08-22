using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using VCore.Standard.ViewModels.WindowsFile;
using VCore.WPF.ViewModels.WindowsFiles;

namespace VPlayer.Library.TemplateSelectors
{
  public class WindowItemTemplateSelector : DataTemplateSelector
  {
    public DataTemplate FolderTemplate { get; set; }
    public DataTemplate FileTemplate { get; set; }

    public override DataTemplate SelectTemplate(object item, DependencyObject container)
    {
      if (item is FolderHierarchyItemViewModel<FolderInfo>)
      {
        return FolderTemplate;
      }
      else if(item is FileViewModel)
      {
        return FileTemplate;
      }


      return new DataTemplate();
    }
  }
}
