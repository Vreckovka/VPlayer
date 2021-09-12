using System;
using System.Linq;
using Prism.Events;
using VCore.Standard.Factories.ViewModels;
using VCore.WPF.Managers;
using VCore.WPF.ViewModels.WindowsFiles;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.FileBrowser;
using VPlayer.WindowsPlayer.ViewModels.Windows;
using VPlayer.WindowsPlayer.Windows.TvShow;

namespace VPlayer.Home.ViewModels.FileBrowser
{
  public class PlayableWindowsFileFolderViewModel : PlayableFolderViewModel<WindowsFolderViewModel, WindowsFileViewModel>
  {
    private readonly IWindowManager windowManager;
    private readonly IStorageManager storageManager;
    private readonly WindowsFileBrowserViewModel windowsFileBrowserViewModel;

    public PlayableWindowsFileFolderViewModel(
      WindowsFolderViewModel folderViewModel,
      IEventAggregator eventAggregator,
      IViewModelsFactory viewModelsFactory,
      IWindowManager windowManager,
      IStorageManager storageManager,
      WindowsFileBrowserViewModel windowsFileBrowserViewModel) : base(folderViewModel, eventAggregator, viewModelsFactory, storageManager)
    {
      this.windowManager = windowManager ?? throw new ArgumentNullException(nameof(windowManager));
      this.storageManager = storageManager ?? throw new ArgumentNullException(nameof(storageManager));
      this.windowsFileBrowserViewModel = windowsFileBrowserViewModel ?? throw new ArgumentNullException(nameof(windowsFileBrowserViewModel));
    }

    #region CreateNewFolderItem

    protected override FolderViewModel<PlayableFileViewModel> CreateNewFolderItem(FolderInfo directoryInfo)
    {
      var folderVm = viewModelsFactory.Create<WindowsFolderViewModel>(directoryInfo);

      return viewModelsFactory.Create<PlayableWindowsFileFolderViewModel>(folderVm);
    }

    #endregion

    #region OnGetFolderInfo

    protected override void OnGetFolderInfo()
    {
      base.OnGetFolderInfo();

      if (windowsFileBrowserViewModel?.Bookmarks != null)
      {
        IsBookmarked = windowsFileBrowserViewModel.Bookmarks.Any(x => x.Model.Indentificator == Model.Indentificator);
      }
    }

    #endregion

    #region OnLoadNewItem

    public override void OnLoadNewItem()
    {
      base.OnLoadNewItem();

      if (FolderType == FolderType.Sound)
      {
        storageManager.StoreData(this.Path);
      }
      else
      {
        var vm = viewModelsFactory.Create<AddNewTvShowViewModel>();
        vm.TvShowPath = Path;
        vm.TemporaryName = Name;

        windowManager.ShowPrompt<AddNewTvShowPrompt>(vm);
      }
    } 

    #endregion
  }
}