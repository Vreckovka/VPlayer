using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.WindowsAPICodePack.Dialogs;
using VCore;
using VCore.Helpers;
using VCore.Modularity.RegionProviders;
using VCore.Standard.Factories.ViewModels;
using VCore.Standard.Helpers;
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
       DataLoader dataLoader,
      IStorageManager storageManager,
       IWindowManager windowManager) : base(regionProvider)
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

      windowManager.ShowPrompt<AddNewTvShowPrompt>(vm);
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

        var musicPlayer = viewModelsFactory.Create<MusicPlayerViewModel>();

        var videoPlayer = viewModelsFactory.Create<VideoPlayerViewModel>();

        var settings = viewModelsFactory.Create<SettingsViewModel>();

      
        NavigationViewModel.Items.Add(new NavigationItem(libraryViewModel)
        {
          ImagePath = "pack://application:,,,/VPlayer;component/Resources/Icons/library.png"
        });

        var musicPlayerNavigationItem = new NavigationItem(musicPlayer)
        {
          ImagePath = "pack://application:,,,/VPlayer;component/Resources/Icons/music-note.png"
        };

        NavigationViewModel.Items.Add(musicPlayerNavigationItem);

        var videoPlayerNavigationItem = new NavigationItem(videoPlayer)
        {
          ImagePath = "pack://application:,,,/VPlayer;component/Resources/Icons/video-play.png"
        };

        NavigationViewModel.Items.Add(videoPlayerNavigationItem);

        NavigationViewModel.Items.Add(new NavigationItem(settings)
        {
          ImagePath = "pack://application:,,,/VPlayer;component/Resources/Icons/settings.png"
        });
        

        musicPlayer.ObservePropertyChange(x => x.IsSelectedToPlay).ObserveOnDispatcher().Subscribe(x => musicPlayerNavigationItem.IsBackroundActive = x).DisposeWith(this);
        videoPlayer.ObservePropertyChange(x => x.IsSelectedToPlay).ObserveOnDispatcher().Subscribe(x => videoPlayerNavigationItem.IsBackroundActive = x).DisposeWith(this);

        libraryViewModel.IsActive = true;
      }
    }

    #endregion

    #region AddFolder

    public void AddFolder(string folderPath)
    {
      storageManager.StoreData(folderPath);
    }

    #endregion

    #region Dispose

    public override void Dispose()
    {
      base.Dispose();

      foreach (var item in NavigationViewModel.Items)
      {
        item?.Dispose();
      }
    }

    #endregion

    #endregion Methods

  }
}