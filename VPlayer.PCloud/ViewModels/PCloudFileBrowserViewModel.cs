﻿using System;
using System.IO;
using System.Threading.Tasks;
using VCore.Modularity.RegionProviders;
using VCore.Standard.Factories.ViewModels;
using VCore.WPF.Managers;
using VCore.WPF.ViewModels.WindowsFiles;
using VPlayer.Core;
using VPlayer.Core.FileBrowser;
using VPlayer.Core.ViewModels;
using VPLayer.Domain.Contracts.CloudService.Providers;

namespace VPlayer.PCloud.ViewModels
{
  public class PCloudFileBrowserViewModel : FileBrowserViewModel<PlayableFolderViewModel<PCloudFolderViewModel, PCloudFileViewModel>>
  {
    private readonly ICloudService cloudService;

    public PCloudFileBrowserViewModel(
      IRegionProvider regionProvider, 
      ICloudService cloudService,
      IViewModelsFactory viewModelsFactory, 
      IWindowManager windowManager) : base(regionProvider, viewModelsFactory, windowManager)
    {
      this.cloudService = cloudService ?? throw new ArgumentNullException(nameof(cloudService));

      BaseDirectoryPath = GlobalSettings.CloudBrowserInitialDirectory;
    }

    protected override async Task<PlayableFolderViewModel<PCloudFolderViewModel, PCloudFileViewModel>> GetNewFolderViewModel(string newPath)
    {
      var dir = await cloudService.GetFolderInfo(long.Parse(newPath));

      var info = new FolderInfo()
      {
        Indentificator = dir.id.ToString(),
        Name = dir.name,
        ParentIndentificator = dir.parentFolderId.ToString()
      };

      var folderViewModel = viewModelsFactory.Create<PCloudFolderViewModel>(info);

      return viewModelsFactory.Create<PlayableFolderViewModel<PCloudFolderViewModel, PCloudFileViewModel>>(folderViewModel);
    }

    protected override async Task<PlayableFolderViewModel<PCloudFolderViewModel, PCloudFileViewModel>> GetParentFolderViewModel(string childIdentificator)
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