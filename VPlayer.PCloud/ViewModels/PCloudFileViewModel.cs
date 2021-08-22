using System;
using VCore.WPF.ViewModels.WindowsFiles;

namespace VPlayer.PCloud.ViewModels
{
  public class PCloudFileViewModel : FileViewModel
  {
    public PCloudFileViewModel(FileInfo fileInfo) : base(fileInfo)
    {
    }

    public override void OnOpenContainingFolder()
    {
      throw new NotImplementedException();
    }
  }
}