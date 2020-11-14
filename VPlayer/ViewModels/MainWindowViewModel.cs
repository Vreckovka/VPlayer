using System;
using VCore.Standard.Factories.ViewModels;
using VCore.ViewModels;
using VCore.ViewModels.Navigation;
using VPlayer.Player.ViewModels;
using VPlayer.WindowsPlayer.ViewModels;

namespace VPlayer.ViewModels
{

  public class MainWindowViewModel : BaseMainWindowViewModel
  {
    #region Fields

    private readonly IViewModelsFactory viewModelsFactory;

    #endregion

    #region Constructors

    public MainWindowViewModel(IViewModelsFactory viewModelsFactory)
    {
      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));
    }

    #endregion

    #region Properties

    public NavigationViewModel NavigationViewModel { get; set; } = new NavigationViewModel();
    public override string Title => "VPlayer";

    #region IsWindows

    private bool isWindows;
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

    #region Methods

    #region Initilize

    public override void Initialize()
    {
      base.Initialize();

      var windowsPlayer = viewModelsFactory.Create<WindowsViewModel>();
      windowsPlayer.IsActive = true;

      isWindows = true;
      NavigationViewModel.Items.Add(windowsPlayer);


      var player = viewModelsFactory.Create<PlayerViewModel>();
      player.IsActive = true;

    }

    #endregion

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

  }
}

