using System;
using System.IO;
using System.Threading.Tasks;
using VCore.Modularity.RegionProviders;
using VCore.Standard.Factories.ViewModels;
using VCore.WPF.Managers;
using VCore.WPF.ViewModels.WindowsFiles;
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
    }

    protected override PlayableFolderViewModel<PCloudFolderViewModel, PCloudFileViewModel> GetNewFolderViewModel(string newPath)
    {
      var info = new FolderInfo()
      {
        Indentificator = newPath,
      };

      var folderViewModel = viewModelsFactory.Create<PCloudFolderViewModel>(info);

      return viewModelsFactory.Create<PlayableFolderViewModel<PCloudFolderViewModel, PCloudFileViewModel>>(folderViewModel);
    }

    protected override string GetParentDirectoryName(string newPath)
    {
      return "PARENT NAME";
    }

    protected override Task<bool> DirectoryExists(string newPath)
    {
      return cloudService.ExistsFolderAsync(long.Parse(newPath));
    }
  }
}