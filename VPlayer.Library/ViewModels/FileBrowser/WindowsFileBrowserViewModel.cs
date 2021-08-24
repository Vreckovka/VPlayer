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

    protected override Task<PlayableFolderViewModel<WindowsFolderViewModel, WindowsFileViewModel>> GetNewFolderViewModel(string newPath)
    {
      return Task.Run(() =>
      {
        var dirInfo = new DirectoryInfo(newPath);

        var info = new FolderInfo()
        {
          Indentificator = newPath,
          Name = dirInfo.Name,
          ParentIndentificator = dirInfo.Parent?.FullName
        };

        var folderViewModel = viewModelsFactory.Create<WindowsFolderViewModel>(info);

        return viewModelsFactory.Create<PlayableFolderViewModel<WindowsFolderViewModel, WindowsFileViewModel>>(folderViewModel);
      });
    }

    protected override Task<PlayableFolderViewModel<WindowsFolderViewModel, WindowsFileViewModel>> GetParentFolderViewModel(string childIdentificator)
    {
      var dirInfo = new DirectoryInfo(childIdentificator).Parent;

      if (dirInfo != null)
      {
        return GetNewFolderViewModel(dirInfo.FullName);
      }

      return null;
    }


    protected override Task<bool> DirectoryExists(string newPath)
    {
      return Task.Run(() => Directory.Exists(newPath));
    }
  }
}