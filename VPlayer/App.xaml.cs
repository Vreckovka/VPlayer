using System;
using System.Windows;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Unity;
using VPlayer.Library.ViewModels;
using VPlayer.Library.ViewModels.ArtistsViewModels;
using VPlayer.Views;
using VPlayer.WindowsPlayer.ViewModels;

namespace VPlayer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : PrismApplication
    {
        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
        }

        protected override Window CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
        {
            moduleCatalog.AddModule<WindowsPlayerViewModel>();
            moduleCatalog.AddModule<LibraryViewModel>();
            moduleCatalog.AddModule<ArtistsViewModel>();
        }
    }
}
