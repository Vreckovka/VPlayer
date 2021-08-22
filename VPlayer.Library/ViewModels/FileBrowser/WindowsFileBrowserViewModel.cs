using System.IO;
using System.Threading.Tasks;
using VCore.Modularity.RegionProviders;
using VCore.Standard.Factories.ViewModels;
using VCore.WPF.Managers;
using VCore.WPF.ViewModels.WindowsFiles;
using VPlayer.Core.FileBrowser;
using VPlayer.Core.ViewModels;

namespace VPlayer.Home.ViewModels.FileBrowser
{
  public class WindowsFileBrowserViewModel : FileBrowserViewModel<PlayableFolderViewModel<WindowsFolderViewModel, WindowsFileViewModel>>
  {
    public WindowsFileBrowserViewModel(
      IRegionProvider regionProvider,
      IViewModelsFactory viewModelsFactory,
      IWindowManager windowManager) : base(regionProvider, viewModelsFactory, windowManager)
    {
    }

    protected override PlayableFolderViewModel<WindowsFolderViewModel, WindowsFileViewModel> GetNewFolderViewModel(string newPath)
    {
      var info = new FolderInfo()
      {
        Indentificator = newPath,
        Name = (new DirectoryInfo(newPath)).Name
      };

      var folderViewModel = viewModelsFactory.Create<WindowsFolderViewModel>(info);

      return viewModelsFactory.Create<PlayableFolderViewModel<WindowsFolderViewModel, WindowsFileViewModel>>(folderViewModel);
    }

    protected override string GetParentDirectoryName(string newPath)
    {
      return new DirectoryInfo(newPath).Parent?.FullName;
    }

    protected override Task<bool> DirectoryExists(string newPath)
    {
      return Task.Run(() => Directory.Exists(newPath));
    }
  }
}