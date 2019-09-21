using System;
using JetBrains.Annotations;
using VCore.Factories;
using VCore.ViewModels;
using VCore.ViewModels.Navigation;
using VPlayer.Library.ViewModels;
using VPlayer.Player.ViewModels;
using VPlayer.Player.Views;
using VPlayer.WindowsPlayer.ViewModels;

namespace VPlayer.ViewModels
{
    public class MainWindowViewModel : BaseMainWindowViewModel
    {
        private readonly IViewModelsFactory viewModelsFactory;

        public MainWindowViewModel([NotNull] IViewModelsFactory viewModelsFactory)
        {
            this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));
        }

        public override void Initialize()
        {
            base.Initialize();

            var windowsPlayer = viewModelsFactory.Create<WindowsPlayerViewModel>();
            windowsPlayer.IsActive = true;
            NavigationViewModel.Items.Add(windowsPlayer);


            var player = viewModelsFactory.Create<PlayerViewModel>();
            player.ActivateView(typeof(PlayerView));
        }

        public NavigationViewModel NavigationViewModel { get; set; } = new NavigationViewModel();
    }
}
