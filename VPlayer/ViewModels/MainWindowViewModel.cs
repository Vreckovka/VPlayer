using System;
using System.Windows.Input;
using Prism.Events;
using VCore;
using VCore.Annotations;
using VCore.Standard.Factories.ViewModels;
using VCore.ViewModels;
using VCore.ViewModels.Navigation;
using VPlayer.AudioStorage.InfoDownloader.Clients.MiniLyrics;
using VPlayer.Core.Events;
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

    #endregion

    #region Constructors

    public MainWindowViewModel(IViewModelsFactory viewModelsFactory, [NotNull] IEventAggregator eventAggregator)
    {
      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));
      this.eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
    }

    #endregion

    #region Properties

    public NavigationViewModel NavigationViewModel { get; set; } = new NavigationViewModel();
    public override string Title => "VPlayer";

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

