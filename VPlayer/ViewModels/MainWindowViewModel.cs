using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Prism.Events;
using Prism.Regions;
using VCore;
using VCore.Annotations;
using VCore.Helpers;
using VCore.Standard.Factories.ViewModels;
using VCore.Standard.Helpers;
using VCore.ViewModels;
using VCore.ViewModels.Navigation;
using VCore.WPF.Behaviors;
using VPlayer.AudioStorage.InfoDownloader.Clients.MiniLyrics;
using VPlayer.AudioStorage.Parsers;
using VPlayer.Core.Events;
using VPlayer.Core.Modularity.Regions;
using VPlayer.Core.ViewModels;
using VPlayer.Player.ViewModels;
using VPlayer.WindowsPlayer.ViewModels;

namespace VPlayer.ViewModels
{

  public class MainWindowViewModel : BaseMainWindowViewModel
  {
    #region Fields

    private readonly IViewModelsFactory viewModelsFactory;
    private readonly IEventAggregator eventAggregator;
    private readonly IRegionManager regionManager;

    #endregion

    #region Constructors

    public MainWindowViewModel(
      IViewModelsFactory viewModelsFactory,
      IEventAggregator eventAggregator,
      IRegionManager regionManager,
      ICSFDWebsiteScrapper cSFDWebsiteScrapper)
    {
      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));
      this.eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
      this.regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));

      eventAggregator.GetEvent<ContentFullScreenEvent>().Subscribe(OnContentFullScreenEvent).DisposeWith(this);

      //Task.Run(async () =>
      //{
      //  var movie = "https://www.csfd.cz/film/69516-cerveny-trpaslik/komentare/";

      //  var tvShow = cSFDWebsiteScrapper.LoadTvShow(movie);
      //});
    }



    #endregion

    #region Properties

    public NavigationViewModel NavigationViewModel { get; set; } = new NavigationViewModel();
    public override string Title => "VPlayer";

    #region IsFullScreenContentVisible

    private bool isFullScreenContentVisible;

    public bool IsFullScreenContentVisible
    {
      get { return isFullScreenContentVisible; }
      set
      {
        if (value != isFullScreenContentVisible)
        {
          isFullScreenContentVisible = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region IsWindows

    private bool isWindows = true;
    public bool IsWindows
    {
      get { return isWindows; }
      set
      {
        if (value != isWindows)
        {
          isWindows = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #endregion

    #region PlayStop

    private ActionCommand playStop;

    public ICommand PlayStop
    {
      get
      {
        if (playStop == null)
        {
          playStop = new ActionCommand(OnPlayStop);
        }

        return playStop;
      }
    }

    public void OnPlayStop()
    {
      eventAggregator.GetEvent<PlayPauseEvent>().Publish();
    }

    #endregion 

    #region Methods

    #region Initilize

    public override void Initialize()
    {
      base.Initialize();

      var windowsPlayer = viewModelsFactory.Create<WindowsViewModel>();

      windowsPlayer.IsActive = true;

      NavigationViewModel.Items.Add(new NavigationItem(windowsPlayer));

      var player = viewModelsFactory.Create<PlayerViewModel>();
      player.IsActive = true;

    }

    #endregion

    #region OnContentFullScreenEvent

    private void OnContentFullScreenEvent(ContentFullScreenEventArgs args)
    {
      IsFullScreenContentVisible = args.IsFullScreen;

      ChangeFullScreen(IsFullScreenContentVisible);

      var region = regionManager.Regions.SingleOrDefault(x => x.Name == RegionNames.FullscreenRegion);

      if (region != null)
      {
        if (args.IsFullScreen)
        {
          var existingView = region.Views.SingleOrDefault(x => x == args.View);

          if (existingView == null)
          {
            regionManager.AddToRegion(RegionNames.FullscreenRegion, args.View);
          }
        }
        else
        {
          region.Remove(args.View);
        }
      }
    }

    #endregion

    #region ChangeFullScreen

    private void ChangeFullScreen(bool isFullScreen)
    {
      var mainWindow = Application.Current.MainWindow;

      if (mainWindow != null)
      {
        if (isFullScreen)
        {
          WindowChromeVisiblity = Visibility.Collapsed;

          mainWindow.WindowState = WindowState.Maximized;
        }
        else
        {
          WindowChromeVisiblity = Visibility.Visible;

          mainWindow.WindowState = WindowState.Normal;
        }
      }
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

    #endregion



  }


}

