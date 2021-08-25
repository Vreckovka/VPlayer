using Prism.Events;
using VCore.Standard.Factories.ViewModels;
using VCore.WPF.ViewModels.WindowsFiles;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.FileBrowser;

namespace VPlayer.Home.ViewModels.FileBrowser
{
  public class PlayableWindowsFileFolderViewModel : PlayableFolderViewModel<WindowsFolderViewModel, WindowsFileViewModel>
  {
    public PlayableWindowsFileFolderViewModel(WindowsFolderViewModel folderViewModel, IEventAggregator eventAggregator, IViewModelsFactory viewModelsFactory, IStorageManager storageManager) : base(folderViewModel, eventAggregator, viewModelsFactory, storageManager)
    {
    }

    #region CreateNewFolderItem

    protected override FolderViewModel<PlayableFileViewModel> CreateNewFolderItem(FolderInfo directoryInfo)
    {
      var folderVm = viewModelsFactory.Create<WindowsFolderViewModel>(directoryInfo);

      return viewModelsFactory.Create<PlayableWindowsFileFolderViewModel>(folderVm);
    }

    #endregion
  }
}