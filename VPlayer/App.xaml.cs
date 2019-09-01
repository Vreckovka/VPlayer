using System.Windows;
using Ninject;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Ninject;
using Prism.Ninject.Ioc;
using VPlayer.AudioStorage.AudioDatabase;
using VPlayer.AudioStorage.Interfaces;
using VPlayer.Core.Modules;
using VPlayer.Library.ViewModels;
using VPlayer.Views;
using VPlayer.WindowsPlayer.ViewModels;


namespace VPlayer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : PrismApplication
    {
        private IKernel Kernel;
        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            Kernel = Container.GetContainer();

            Kernel.Load(new BaseNinjectModule());

            Kernel.Bind<IStorage>().To<AudioDatabaseManager>();
        }

        protected override Window CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
        {
            moduleCatalog.AddModule<WindowsPlayerViewModel>();
            moduleCatalog.AddModule<LibraryViewModel>();
        }
      
    }
}
