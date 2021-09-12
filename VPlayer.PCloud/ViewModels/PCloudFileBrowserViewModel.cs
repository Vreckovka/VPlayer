using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using VCore.Modularity.RegionProviders;
using VCore.Standard.Factories.ViewModels;
using VCore.Standard.Providers;
using VCore.WPF.Managers;
using VCore.WPF.ViewModels.WindowsFiles;
using VPlayer.AudioStorage.DomainClasses.FolderStructure;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core;
using VPlayer.Core.FileBrowser;
using VPlayer.Core.ViewModels;
using VPLayer.Domain.Contracts.CloudService.Providers;

namespace VPlayer.PCloud.ViewModels
{
  public class PCloudFileBrowserViewModel : FileBrowserViewModel<PlayblePCloudFolderViewModel>
  {
    private readonly ICloudService cloudService;

    public PCloudFileBrowserViewModel(
      IRegionProvider regionProvider,
      ICloudService cloudService,
      ISettingsProvider settingsProvider,
      IViewModelsFactory viewModelsFactory,
      IWindowManager windowManager,
      IStorageManager storageManager) : base(regionProvider, viewModelsFactory, windowManager, settingsProvider, storageManager)
    {
      this.cloudService = cloudService ?? throw new ArgumentNullException(nameof(cloudService));

      BaseDirectoryPath = settingsProvider.GetSetting(GlobalSettings.CloudBrowserInitialDirectory)?.Value;
    }

    public override Visibility FinderVisibility => Visibility.Collapsed;
    public override FileBrowserType FileBrowserType { get; } = FileBrowserType.Cloud;

    protected override void OnDeleteItem(string indentificator)
    {
      throw new NotImplementedException();
    }

    protected override async Task<PlayblePCloudFolderViewModel> GetNewFolderViewModel(string newPath)
    {
      var dir = await cloudService.GetFolderInfo(long.Parse(newPath));

      var info = new FolderInfo()
      {
        Indentificator = dir.id.ToString(),
        Name = dir.name,
        ParentIndentificator = dir.parentFolderId.ToString()
      };

      var folderViewModel = viewModelsFactory.Create<PCloudFolderViewModel>(info);

      return viewModelsFactory.Create<PlayblePCloudFolderViewModel>(folderViewModel);
    }

    protected override async Task<PlayblePCloudFolderViewModel> GetParentFolderViewModel(string childIdentificator)
    {
      var parent = await cloudService.GetFolderInfo(long.Parse(childIdentificator));

      if (parent != null)
      {
        var parentId = parent.parentFolderId.ToString();

        return await GetNewFolderViewModel(parentId);
      }

      return null;
    }

    protected override Task<bool> DirectoryExists(string newPath)
    {
      return cloudService.ExistsFolderAsync(long.Parse(newPath));
    }
  }
}