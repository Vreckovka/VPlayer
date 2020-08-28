using System;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using Microsoft.WindowsAPICodePack.Dialogs;
using VCore;
using VCore.Factories;
using VCore.Modularity.RegionProviders;
using VCore.ViewModels;
using VCore.ViewModels.Navigation;
using Vlc.DotNet.Core.Interops;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Modularity.Regions;
using VPlayer.Core.ViewModels;
using VPlayer.Core.ViewModels.Settings;
using VPlayer.Library.ViewModels;
using VPlayer.Player.ViewModels;
using VPlayer.WindowsPlayer.Views;

namespace VPlayer.WindowsPlayer.ViewModels
{
  public class WindowsViewModel : RegionViewModel<WindowsView>, INavigationItem
  {
    #region Fields

    private readonly IViewModelsFactory viewModelsFactory;
    private readonly IStorageManager storageManager;

    #endregion Fields

    #region Constructors

    public WindowsViewModel(
      IRegionProvider regionProvider,
      IViewModelsFactory viewModelsFactory,
      NavigationViewModel navigationViewModel,
      IStorageManager storageManager) : base(regionProvider)
    {
      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));
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

    #endregion

    #region Methods

    #region Initialize

    public override void Initialize()
    {
      base.Initialize();

      var libraryViewModel = viewModelsFactory.Create<LibraryViewModel>();
      libraryViewModel.IsActive = true;

      var playerViewModel = viewModelsFactory.Create<Player.ViewModels.WindowsPlayerViewModel>();
      var item = playerViewModel;

      var settings = viewModelsFactory.Create<SettingsViewModel>();


      NavigationViewModel.Items.Add(libraryViewModel);
      NavigationViewModel.Items.Add(item);
      NavigationViewModel.Items.Add(settings);
    }

    #endregion

    #endregion Methods

    public void AddFolder(string folderPath)
    {
      storageManager.StoreData(folderPath);
    }
  }
}