using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.WindowsAPICodePack.Dialogs;
using VCore;
using VCore.Annotations;
using VCore.Modularity.RegionProviders;
using VCore.Standard.Factories.ViewModels;
using VCore.ViewModels;
using VCore.ViewModels.Navigation;
using VCore.WPF.Managers;
using VPlayer.AudioStorage.DataLoader;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Modularity.Regions;
using VPlayer.Core.ViewModels.Settings;
using VPlayer.Library.ViewModels;
using VPlayer.WindowsPlayer.ViewModels.Windows;
using VPlayer.WindowsPlayer.Views;
using VPlayer.WindowsPlayer.Windows.TvShow;

namespace VPlayer.WindowsPlayer.ViewModels
{
  public class WindowsViewModel : RegionViewModel<WindowsView>, INavigationItem
  {
    #region Fields

    private readonly IViewModelsFactory viewModelsFactory;
    private readonly IStorageManager storageManager;
    private readonly IWindowManager windowManager;

    #endregion Fields

    #region Constructors

    public WindowsViewModel(
      IRegionProvider regionProvider,
      IViewModelsFactory viewModelsFactory,
      NavigationViewModel navigationViewModel,
      [NotNull] DataLoader dataLoader,
      IStorageManager storageManager,
      [NotNull] IWindowManager windowManager) : base(regionProvider)
    {
      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));
      this.storageManager = storageManager ?? throw new ArgumentNullException(nameof(storageManager));
      this.windowManager = windowManager ?? throw new ArgumentNullException(nameof(windowManager));
      NavigationViewModel = navigationViewModel ?? throw new ArgumentNullException(nameof(navigationViewModel));
    }

    #endregion Constructors

    #region Properties

    public override bool ContainsNestedRegions => true;
    public string Header => "Windows player";
    public NavigationViewModel NavigationViewModel { get; set; }
    public override string RegionName { get; protected set; } = RegionNames.ContentRegion;

    #endregion Properties

    #region Commands

    #region LoadFromFolder

    private ActionCommand loadFromFolder;

    public ICommand LoadFromFolder
    {
      get
      {
        if (loadFromFolder == null)
        {
          loadFromFolder = new ActionCommand(OnLoadFromFolder);
        }

        return loadFromFolder;
      }
    }

    public void OnLoadFromFolder()
    {
      CommonOpenFileDialog dialog = new CommonOpenFileDialog();

      dialog.AllowNonFileSystemItems = true;
      dialog.IsFolderPicker = true;
      dialog.Title = "Select folders with music files";

      if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
      {
        Task.Run(() =>
        {
          AddFolder(dialog.FileName);
        });
      }
    }

    #endregion

    #region LoadTvShow

    private ActionCommand loadTvShow;

    public ICommand LoadTvShow
    {
      get
      {
        if (loadTvShow == null)
        {
          loadTvShow = new ActionCommand(OnLoadLoadTvShow);
        }

        return loadTvShow;
      }
    }

    public void OnLoadLoadTvShow()
    {
      var vm = viewModelsFactory.Create<AddNewTvShowViewModel>();

      windowManager.ShowPrompt<AddNewTvShowWindow>(vm);
    }

    #endregion

    #endregion

    #region Methods

    #region Initialize


    #endregion

    #region OnActivation

    public override void OnActivation(bool firstActivation)
    {
      base.OnActivation(firstActivation);


      if (firstActivation)
      {
        var libraryViewModel = viewModelsFactory.Create<LibraryViewModel>();

        var playerViewModel = viewModelsFactory.Create<MusicPlayerViewModel>();

        var settings = viewModelsFactory.Create<SettingsViewModel>();

         var videoPlayer = viewModelsFactory.Create<VideoPlayerViewModel>();
       

        NavigationViewModel.Items.Add(new NavigationItem(libraryViewModel));
        NavigationViewModel.Items.Add(new NavigationItem(playerViewModel));
        NavigationViewModel.Items.Add(new NavigationItem(videoPlayer));
        NavigationViewModel.Items.Add(new NavigationItem(settings));


        libraryViewModel.IsActive = true;
      }
    }

    #endregion

    #endregion Methods

    public void AddFolder(string folderPath)
    {
      storageManager.StoreData(folderPath);
    }

    public override void Dispose()
    {
      base.Dispose();

      foreach (var item in NavigationViewModel.Items)
      {
        item?.Dispose();
      }
    }
  }
}