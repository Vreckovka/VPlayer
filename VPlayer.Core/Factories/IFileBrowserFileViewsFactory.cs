using System;
using VCore.WPF.ViewModels.WindowsFiles;
using VPlayer.Core.FileBrowser;

namespace VPlayer.Core.Factories
{
  public interface IFileBrowserFileViewsFactory
  {
    Func<FileInfo,PlayableFileViewModel> CreateFileBrowserFileConstructor();
  }
}
