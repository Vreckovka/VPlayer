using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualBasic.FileIO;
using VCore.Standard.Factories.ViewModels;
using VCore.Standard.Providers;
using VCore.WPF.Interfaces.Managers;
using VCore.WPF.Managers;
using VCore.WPF.Modularity.RegionProviders;
using VCore.WPF.ViewModels.WindowsFiles;
using VPlayer.AudioStorage.DomainClasses.FolderStructure;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core;
using VPlayer.Core.FileBrowser;
using VPlayer.Core.ViewModels;

namespace VPlayer.Home.ViewModels.FileBrowser
{
  public class WindowsFileBrowserViewModel : FileBrowserViewModel<PlayableWindowsFileFolderViewModel>
  {
    public WindowsFileBrowserViewModel(
      IRegionProvider regionProvider,
      IViewModelsFactory viewModelsFactory,
      ISettingsProvider settingsProvider,
      IWindowManager windowManager,
      IStorageManager storageManager) : base(regionProvider, viewModelsFactory, windowManager, settingsProvider, storageManager)
    {
      BaseDirectoryPath = settingsProvider.GetSetting(GlobalSettings.FileBrowserInitialDirectory)?.Value;
    }

    public override FileBrowserType FileBrowserType { get; } = FileBrowserType.Local;

    protected override void OnDeleteItem(string indentificator)
    {
      if (Directory.Exists(indentificator))
      {
        FileSystem.DeleteDirectory(indentificator, UIOption.AllDialogs, RecycleOption.SendToRecycleBin);
      }
      else if (File.Exists(indentificator))
      {
        FileSystem.DeleteFile(indentificator, UIOption.AllDialogs, RecycleOption.SendToRecycleBin);
      }

      var pathNames = indentificator.Split("\\");
      var tmpItem = Items.First();

      int? indexParent = null;

      for (int i = 0; i < pathNames.Length; i++)
      {
        if (tmpItem == null)
          return;

        if (tmpItem.Name == pathNames[i] && indexParent == null)
        {
          indexParent = i;
        }
        else if (indexParent != null)
        {
          if (i > indexParent)
          {
            var tmpTmpItem = tmpItem.SubItems.ViewModels.SingleOrDefault(x => x.Name == pathNames[i]);

            if (tmpTmpItem is PlayableFileViewModel fileViewModel)
            {
              tmpItem.SubItems.Remove(fileViewModel);
              return;
            }
            else
            {
              tmpItem = tmpTmpItem;
            }
          }
        }
      }

      if (tmpItem != null && tmpItem is PlayableWindowsFileFolderViewModel deletedFolder)
      {
        if (deletedFolder.ParentFolder != null)
        {
          deletedFolder.ParentFolder.SubItems.Remove(deletedFolder);
        }
      }
    }

    protected override Task<PlayableWindowsFileFolderViewModel> GetNewFolderViewModel(string newPath)
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

        return viewModelsFactory.Create<PlayableWindowsFileFolderViewModel>(folderViewModel);
      });
    }

    protected override Task<PlayableWindowsFileFolderViewModel> GetParentFolderViewModel(string childIdentificator)
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