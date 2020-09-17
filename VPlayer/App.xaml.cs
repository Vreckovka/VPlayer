using System.Windows;
using System.Windows.Navigation;
using Ninject;
using Prism.Events;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Ninject;
using Prism.Ninject.Ioc;
using Prism.Regions;
using VCore.Factories;
using VCore.Modularity.NinjectModules;
using VCore.Modularity.RegionProviders;
using VCore.ViewModels.Navigation;
using VPlayer.AudioStorage.AudioDatabase;
using VPlayer.AudioStorage.Interfaces;
using VPlayer.Core.Managers;
using VPlayer.Library.ViewModels;
using VPlayer.Modularity.NinjectModules;
using VPlayer.ViewModels;
using VPlayer.Views;
using VPlayer.WindowsPlayer.Modularity.NinjectModule;
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

      Kernel.Load<CommonNinjectModule>();
      Kernel.Load<VPlayerNinjectModule>();

      ImageDeleteManager.Initlize(Kernel.Get<IEventAggregator>(),Kernel, Kernel.Get<IRegionProvider>());
    }

    protected override Window CreateShell()
    {
      var shell = Container.Resolve<MainWindow>();

      RegionManager.SetRegionManager(shell, Kernel.Get<IRegionManager>());
      RegionManager.UpdateRegions();

      var dataContext = Container.Resolve<MainWindowViewModel>();
      shell.DataContext = dataContext;

      return shell;
    }

    protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
    {
      //moduleCatalog.AddModule<WindowsViewModel>();
      //moduleCatalog.AddModule<LibraryViewModel>();
    }

    protected override void OnInitialized()
    {
      base.OnInitialized();
    }
  }
}
