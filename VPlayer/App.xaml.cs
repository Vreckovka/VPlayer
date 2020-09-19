using System.Windows;
using Ninject;
using Prism.Events;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Ninject;
using Prism.Regions;
using VCore.Modularity.NinjectModules;
using VCore.Modularity.RegionProviders;
using VPlayer.Modularity.NinjectModules;
using VPlayer.ViewModels;
using VPlayer.Views;


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
