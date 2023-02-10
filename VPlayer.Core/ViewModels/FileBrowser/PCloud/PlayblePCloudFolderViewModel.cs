using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Prism.Events;
using VCore.Standard.Factories.ViewModels;
using VCore.WPF.ViewModels.WindowsFiles;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.FileBrowser;
using VPLayer.Domain;

namespace VPlayer.Core.ViewModels.FileBrowser.PCloud
{
  public class PlayblePCloudFolderViewModel : PlayableFolderViewModel<PCloudFolderViewModel, PCloudFileViewModel>
  {
    private readonly IVPlayerCloudService iVPlayerCloudService;

    public PlayblePCloudFolderViewModel(
      PCloudFolderViewModel folderViewModel,
      IEventAggregator eventAggregator, 
      IViewModelsFactory viewModelsFactory,
      IVPlayerCloudService iVPlayerCloudService,
      IStorageManager storageManager) : base(folderViewModel, eventAggregator, 
      viewModelsFactory, storageManager)
    {
      this.iVPlayerCloudService = iVPlayerCloudService ?? throw new ArgumentNullException(nameof(iVPlayerCloudService));
    }

    public override int MaxAutomaticLoadLevel => 0;

    protected override bool IsRecursive => true;
    public override bool CanPlay { get => true; }

    #region CreateNewFolderItem

    protected override FolderViewModel<PlayableFileViewModel> CreateNewFolderItem(FolderInfo directoryInfo)
    {
      var folderVm = viewModelsFactory.Create<PCloudFolderViewModel>(directoryInfo);

      return viewModelsFactory.Create<PlayblePCloudFolderViewModel>(folderVm);
    }

    #endregion

    public override Task<IEnumerable<FileInfo>> GetItemSources(IEnumerable<FileInfo> fileInfo)
    {
      return iVPlayerCloudService.GetItemSources(fileInfo).Process;
    }
  }
}