using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.WindowsAPICodePack.Dialogs;
using VCore;
using VCore.Standard.Factories.ViewModels;
using VCore.Standard.Helpers;
using VCore.Standard.Providers;
using VCore.WPF.Helpers;
using VCore.WPF.Interfaces.Managers;
using VCore.WPF.Managers;
using VCore.WPF.Misc;
using VCore.WPF.Modularity.RegionProviders;
using VCore.WPF.ViewModels;
using VCore.WPF.ViewModels.Navigation;
using VPlayer.AudioStorage.DataLoader;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core;
using VPlayer.Core.Modularity.Regions;
using VPlayer.Core.ViewModels.Settings;
using VPlayer.Home.ViewModels;
using VPlayer.IPTV.ViewModels;
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
    private readonly ISettingsProvider settingsProvider;
    private readonly IStorageManager storageManager;
    private readonly IWindowManager windowManager;

    #endregion Fields

    #region Constructors

    public WindowsViewModel(
      IRegionProvider regionProvider,
      IViewModelsFactory viewModelsFactory,
      NavigationViewModel navigationViewModel,
       DataLoader dataLoader,
      ISettingsProvider settingsProvider,
      IStorageManager storageManager,
       IWindowManager windowManager) : base(regionProvider)
    {
      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));
      this.settingsProvider = settingsProvider ?? throw new ArgumentNullException(nameof(settingsProvider));
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
      dialog.InitialDirectory = settingsProvider.GetSetting(GlobalSettings.MusicInitialDirectory)?.Value; 
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

    public override void Initialize()
    {
      if (WasInitilized)
      {
        return;
      }

      base.Initialize();

      InitilizeMenu();
    }

    #endregion

    #region OnActivation

    public override void OnActivation(bool firstActivation)
    {
      base.OnActivation(firstActivation);

      if(firstActivation)
      {
        NavigationViewModel.Items[0].IsActive = true;
      }
    }

    #endregion

    private void InitilizeMenu()
    {
      var homeViewModel = viewModelsFactory.Create<HomeViewModel>();
      var musicPlayer = viewModelsFactory.Create<MusicPlayerViewModel>();
      var videoPlayer = viewModelsFactory.Create<VideoPlayerViewModel>();
      var tvPlayer = viewModelsFactory.Create<WindowsIPTVPlayer>();


      NavigationViewModel.Items.Add(new NavigationItem(homeViewModel)
      {
        IconPathData = "M567.5 229.7l-263.1-224C299 1.891 293.5 .0029 288 .0029S276.1 1.891 272.5 5.672L8.471 229.7C2.877 234.4 0 241.2 0 247.1c0 16.03 13.69 24 24.01 24c5.484 0 11-1.865 15.52-5.686L64 245.5l.0039 186.5c.002 44.18 35.82 79.1 80 79.1h287.1c44.18 0 79.1-35.82 80-79.1l-.001-186.5l24.47 20.76c4.516 3.812 10.03 5.688 15.52 5.688c10.16 0 24.02-8.031 24.02-24C575.1 241.2 573.1 234.4 567.5 229.7zM335.1 463.1H240V320h95.1V463.1zM463.1 431.1c0 17.6-14.4 32-32 32h-47.1V312c0-22.06-17.94-40-40-40H232C209.9 272 192 289.9 192 312v151.1H144c-17.6 0-32-14.4-32-32V207.1c0-.9629-.4375-1.783-.5488-2.717L287.1 55.46l175.1 149.4V431.1z"
      });



      var musicPlayerNavigationItem = new NavigationItem(musicPlayer)
      {
        IconPathData = "M480 0c-3.25 0-6.5 .4896-9.625 1.49l-304 96.01C153.1 101.8 144 114 144 128v235.1c-15-7.375-31.38-11.12-48-11.12C42.1 352 0 387.8 0 432S42.1 512 95.1 512c49.38 0 89.5-31.12 94.88-71.13c.75-2.75 1.123-5.95 1.123-8.825L192 256l272-85.88v129c-15-7.375-31.38-11.12-48.01-11.12c-53 0-95.1 35.75-95.1 79.1s42.1 79.1 95.1 79.1c49.38 0 89.51-31.25 95.01-71.13c.625-2.75 .875-5.5 1-8.25V31.99C512 14.36 497.8 0 480 0zM96 464c-28.25 0-48-16.88-48-32s19.75-32 48-32s48 16.88 48 32S124.2 464 96 464zM464 368c0 15.12-19.75 32-48 32s-48-16.88-48-32s19.75-32 48-32S464 352.9 464 368zM464 119.8L192 205.6V139.8l272-85.88V119.8z"
      };

      NavigationViewModel.Items.Add(musicPlayerNavigationItem);



      var videoPlayerNavigationItem = new NavigationItem(videoPlayer)
      {
        IconPathData = "M448 32H64C28.65 32 0 60.65 0 96v320c0 35.35 28.65 64 64 64h384c35.35 0 64-28.65 64-64V96C512 60.65 483.3 32 448 32zM254.1 80h67.88l-80 80H174.1L254.1 80zM48 126.1L94.06 80h67.88l-80 80H48V126.1zM464 416c0 8.822-7.178 16-16 16H64c-8.822 0-16-7.178-16-16V208h416V416zM464 97.94L401.9 160h-67.88l80-80H448c8.822 0 16 7.178 16 16V97.94zM218.7 400c1.959 0 3.938-.5605 5.646-1.682l106.7-68.97C334.1 327.3 336 323.8 336 319.1s-1.896-7.34-5.021-9.354l-106.7-68.97C221.1 239.5 216.9 239.5 213.5 241.4C210.1 243.3 208 247 208 251v137.9c0 4.008 2.104 7.705 5.5 9.656C215.1 399.5 216.9 400 218.7 400z"
      };

      NavigationViewModel.Items.Add(videoPlayerNavigationItem);



      var tvPlayerNavigationItem = new NavigationItem(tvPlayer)
      {
        IconPathData = "M201.5 344.5l47.48-47.48c9.375-9.375 9.375-24.56 0-33.94s-24.56-9.375-33.94 0L167.5 310.5L72.06 215C67.11 210.1 60.33 207.7 53.27 208.1C46.3 208.6 39.89 212.2 35.75 217.8C12.36 249.7 0 287.4 0 327.1C0 429.1 82.95 512 184.9 512c39.66 0 77.45-12.38 109.3-35.75c5.641-4.156 9.203-10.53 9.734-17.53c.5313-6.969-2.016-13.84-6.969-18.78L201.5 344.5zM184.9 464C109.4 464 48 402.6 48 327.1c0-19.66 4.109-38.72 12.02-56.22L241.1 452C223.6 459.9 204.6 464 184.9 464zM216 0C202.8 0 192 10.75 192 24S202.8 48 216 48c136.8 0 248 111.3 248 248c0 13.25 10.75 24 24 24S512 309.3 512 296C512 132.8 379.2 0 216 0zM216 104C202.8 104 192 114.8 192 128s10.75 24 24 24c79.41 0 144 64.59 144 144C360 309.3 370.8 320 384 320s24-10.75 24-24C408 190.1 321.9 104 216 104z"
      };

      NavigationViewModel.Items.Add(tvPlayerNavigationItem);



      musicPlayer.ObservePropertyChange(x => x.IsSelectedToPlay).ObserveOnDispatcher().Subscribe(x => musicPlayerNavigationItem.IsBackroundActive = x).DisposeWith(this);
      videoPlayer.ObservePropertyChange(x => x.IsSelectedToPlay).ObserveOnDispatcher().Subscribe(x => videoPlayerNavigationItem.IsBackroundActive = x).DisposeWith(this);
      tvPlayer.ObservePropertyChange(x => x.IsSelectedToPlay).ObserveOnDispatcher().Subscribe(x => tvPlayerNavigationItem.IsBackroundActive = x).DisposeWith(this);
    }

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