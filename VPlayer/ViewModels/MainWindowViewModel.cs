using System;
using VCore.Factories;
using VCore.ViewModels;
using VCore.ViewModels.Navigation;
using VPlayer.Library.ViewModels;
using VPlayer.Player.ViewModels;
using VPlayer.Player.Views;
using VPlayer.WindowsPlayer.ViewModels;
using WindowsPlayerViewModel = VPlayer.Player.ViewModels.WindowsPlayerViewModel;

namespace VPlayer.ViewModels
{
  public class MainWindowViewModel : BaseMainWindowViewModel
  {
    private readonly IViewModelsFactory viewModelsFactory;

    public MainWindowViewModel(IViewModelsFactory viewModelsFactory)
    {
      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));
    }

    public override void Initialize()
    {
      base.Initialize();

      var windowsPlayer = viewModelsFactory.Create<WindowsViewModel>();
      windowsPlayer.IsActive = true;
      NavigationViewModel.Items.Add(windowsPlayer);


      var player = viewModelsFactory.Create<PlayerViewModel>();
      player.IsActive = true;
    }

    public NavigationViewModel NavigationViewModel { get; set; } = new NavigationViewModel();
  }
}
