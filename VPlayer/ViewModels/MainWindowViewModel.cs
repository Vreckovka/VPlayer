using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using HtmlAgilityPack;
using Prism.Events;
using Prism.Regions;
using SoundManagement;
using VCore;
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
using VPlayer.UPnP.ViewModels;
using VPlayer.WindowsPlayer.Behaviors;
using VPlayer.WindowsPlayer.ViewModels;

namespace VPlayer.ViewModels
{
  public class MainWindowViewModel : BaseMainWindowViewModel
  {
    #region Fields

    private readonly IViewModelsFactory viewModelsFactory;
    private readonly IEventAggregator eventAggregator;
    private readonly IRegionManager regionManager;
    private readonly UPnPManagerViewModel uPnPManagerViewModel;

    #endregion

    #region Constructors

    public MainWindowViewModel(
      IViewModelsFactory viewModelsFactory,
      IEventAggregator eventAggregator,
      IRegionManager regionManager,
      UPnPManagerViewModel uPnPManagerViewModel)
    {
      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));
      this.eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
      this.regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
      this.uPnPManagerViewModel = uPnPManagerViewModel ?? throw new ArgumentNullException(nameof(uPnPManagerViewModel));

      AudioDeviceManager.Instance.RefreshAudioDevices();

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

    public ICommand SwitchBehaviorCommand { get; set; }

    #endregion

    #region Commads

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

    #region SwitchScreenCommand

    private ActionCommand switchScreenCommand;

    public ICommand SwitchScreenCommand
    {
      get
      {
        return switchScreenCommand ??= new ActionCommand(SwitchScreen);
      }
    }


    private void SwitchScreen()
    {
      SwitchBehaviorCommand?.Execute(null);

      FullScreenManager.IsFullscreen = false;
    }

    #endregion

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

