using UPnP.Common;
using VCore.Standard.ViewModels.WindowsFile;
using VPlayer.AudioStorage.Interfaces.Storage;

namespace VPlayer.UPnP.ViewModels.UPnP.TreeViewItems
{
  public class UPnPItemViewModel : UPnPTreeViewItem<Item>
  {
    public UPnPItemViewModel(Item model) : base(model)
    {
      Name = Model.Title;

      HighlitedText = Name;
    }

    #region FileType

    private FileType fileType;

    public FileType FileType
    {
      get { return fileType; }
      set
      {
        if (value != fileType)
        {
          fileType = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion
  }
}