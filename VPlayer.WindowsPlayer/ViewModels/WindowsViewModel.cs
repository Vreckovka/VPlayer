using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.WindowsAPICodePack.Dialogs;
using VCore;
using VCore.Annotations;
using VCore.Modularity.RegionProviders;
using VCore.Standard.Factories.ViewModels;
using VCore.ViewModels;
using VCore.ViewModels.Navigation;
using VPlayer.AudioStorage.DataLoader;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Modularity.Regions;
using VPlayer.Core.ViewModels.Settings;
using VPlayer.Library.ViewModels;
using VPlayer.WindowsPlayer.Views;

namespace VPlayer.WindowsPlayer.ViewModels
{
  public class WindowsViewModel : RegionViewModel<WindowsView>, INavigationItem
  {
    #region Fields

    private readonly IViewModelsFactory viewModelsFactory;
    private readonly DataLoader dataLoader;
    private readonly IStorageManager storageManager;

    #endregion Fields

    #region Constructors

    public WindowsViewModel(
      IRegionProvider regionProvider,
      IViewModelsFactory viewModelsFactory,
      NavigationViewModel navigationViewModel,
      [NotNull] DataLoader dataLoader,
      IStorageManager storageManager) : base(regionProvider)
    {
      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));
      this.dataLoader = dataLoader ?? throw new ArgumentNullException(nameof(dataLoader));
      this.storageManager = storageManager ?? throw new ArgumentNullException(nameof(storageManager));
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

    #region LoadFromFolder

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
      CommonOpenFileDialog dialog = new CommonOpenFileDialog();

      dialog.AllowNonFileSystemItems = true;
      dialog.IsFolderPicker = true;
      dialog.Title = "Select folders with tv show";

      if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
      {
        Task.Run(async () =>
        {
          var tvShow = dataLoader.LoadTvShow("The Simpsons", dialog.FileName);

          await storageManager.StoreTvShow(tvShow);

          Console.WriteLine("TV show stored");
        });
      }
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

        libraryViewModel.RegionManager = RegionManager;
        libraryViewModel.IsActive = true;

        var playerViewModel = viewModelsFactory.Create<WindowsPlayerViewModel>();
        playerViewModel.RegionManager = RegionManager;

        var settings = viewModelsFactory.Create<SettingsViewModel>();
        settings.RegionManager = RegionManager;

         var videoPlayer = viewModelsFactory.Create<VideoPlayerViewModel>();
         videoPlayer.RegionManager = RegionManager;

        NavigationViewModel.Items.Add(new NavigationItem(libraryViewModel));
        NavigationViewModel.Items.Add(new NavigationItem(playerViewModel));
        NavigationViewModel.Items.Add(new NavigationItem(videoPlayer));
        NavigationViewModel.Items.Add(new NavigationItem(settings));
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