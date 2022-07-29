using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualBasic.FileIO;
using VCore.Standard.Factories.ViewModels;
using VCore.Standard.Providers;
using VCore.Standard.ViewModels.TreeView;
using VCore.WPF.Interfaces.Managers;
using VCore.WPF.ItemsCollections.VirtualList;
using VCore.WPF.ItemsCollections.VirtualList.VirtualLists;
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
    private IPlayableRegionViewModel[] allPlayers;
    public WindowsFileBrowserViewModel(
      IRegionProvider regionProvider,
      IViewModelsFactory viewModelsFactory,
      ISettingsProvider settingsProvider,
      IWindowManager windowManager,
      IStorageManager storageManager,
      IPlayableRegionViewModel[] players) : base(regionProvider, viewModelsFactory, windowManager, settingsProvider, storageManager)
    {
      BaseDirectoryPath = settingsProvider.GetSetting(GlobalSettings.FileBrowserInitialDirectory)?.Value;
      allPlayers = players;
    }

    public override FileBrowserType FileBrowserType { get; } = FileBrowserType.Local;

    public override async Task<bool> SetUpManager()
    {
      var result = await base.SetUpManager();

      if (!result)
      {
        BaseDirectoryPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        result = await SetBaseDirectory();
      }

      return result;
    }


    protected override async void OnDeleteItem(string indentificator)
    {
      foreach (var player in allPlayers.OrderByDescending(x => x.IsSelectedToPlay))
      {
        bool isItemInPlaylist = false;
        var isDir = Directory.Exists(indentificator);

        var items = player.GetAllItemsSources();

        if (isDir)
        {
          isItemInPlaylist = items.Any(x => x.Contains(indentificator));
        }
        else
        {
          isItemInPlaylist = items.Contains(indentificator);
        }


        if (isItemInPlaylist)
        {
          await player.ClearPlaylist();
          break;
        }
      }

      if (Directory.Exists(indentificator))
      {
        FileSystem.DeleteDirectory(indentificator, UIOption.AllDialogs, RecycleOption.SendToRecycleBin);
      }
      else if (File.Exists(indentificator))
      {
        FileSystem.DeleteFile(indentificator, UIOption.AllDialogs, RecycleOption.SendToRecycleBin);
      }

      var pathNames = indentificator.Split("\\");

      var allItems = Items.ToList();
      allItems.Add(RootFolder);

      TreeViewItemViewModel tmpItem = null;

      for (int i = 0; i < pathNames.Length; i++)
      {
        if (tmpItem == null)
          tmpItem = allItems.OfType<PlayableWindowsFileFolderViewModel>().SingleOrDefault(x => x.Name == pathNames[i]);

        if (tmpItem != null)
        {
          var tmpTmpItem = tmpItem.SubItems.ViewModels.SingleOrDefault(x => x.Name == pathNames[i]);

          if (tmpTmpItem is PlayableFileViewModel fileViewModel)
          {
            tmpItem.SubItems.Remove(fileViewModel);

            if (tmpItem == RootFolder)
            {
              var generator = new ItemsGenerator<TreeViewItemViewModel>(RootFolder.SubItems.ViewModels.Select(x => x), 30);

              Items = new VirtualList<TreeViewItemViewModel>(generator);
            }

            return;
          }
          else if(tmpTmpItem != null)
          {
            tmpItem = tmpTmpItem;
          }
        }
      }

      if (tmpItem != null && tmpItem is PlayableWindowsFileFolderViewModel deletedFolder)
      {
        if (deletedFolder.ParentFolder != null)
        {
          deletedFolder.ParentFolder.SubItems.Remove(deletedFolder);

          if (deletedFolder.ParentFolder == RootFolder)
          {
            var generator = new ItemsGenerator<TreeViewItemViewModel>(RootFolder.SubItems.ViewModels.Select(x => x), 30);

            Items = new VirtualList<TreeViewItemViewModel>(generator);
          }
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