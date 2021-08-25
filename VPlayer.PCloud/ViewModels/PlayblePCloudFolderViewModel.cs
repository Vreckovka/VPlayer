using Prism.Events;
using VCore.Standard.Factories.ViewModels;
using VCore.WPF.ViewModels.WindowsFiles;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.FileBrowser;

namespace VPlayer.PCloud.ViewModels
{
  public class PlayblePCloudFolderViewModel : PlayableFolderViewModel<PCloudFolderViewModel, PCloudFileViewModel>
  {
    public PlayblePCloudFolderViewModel(PCloudFolderViewModel folderViewModel,
      IEventAggregator eventAggregator, IViewModelsFactory viewModelsFactory,
      IStorageManager storageManager) : base(folderViewModel, eventAggregator, 
      viewModelsFactory, storageManager)
    {
    }

    public override bool LoadSubItemsWhenExpanded => false;

    public override bool CanPlay { get => true; }

    #region CreateNewFolderItem

    protected override FolderViewModel<PlayableFileViewModel> CreateNewFolderItem(FolderInfo directoryInfo)
    {
      var folderVm = viewModelsFactory.Create<PCloudFolderViewModel>(directoryInfo);

      return viewModelsFactory.Create<PlayblePCloudFolderViewModel>(folderVm);
    }

    #endregion
  }
}