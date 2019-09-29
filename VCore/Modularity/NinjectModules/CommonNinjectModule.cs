using VCore.Factories;
using VCore.Factories.Views;
using VCore.Modularity.Navigation;
using VCore.Modularity.RegionProviders;

namespace VCore.Modularity.NinjectModules
{
  public class CommonNinjectModule : BaseNinjectModule
  {
    #region Methods

    public override void RegisterFactories()
    {
      Kernel.Bind<IViewModelsFactory>().To<BaseViewModelsFactory>();
      Kernel.Bind<IViewFactory>().To<BaseViewFactory>();
    }

    public override void RegisterProviders()
    {
      Kernel.Bind<IRegionProvider>().To<BaseRegionProvider>().InSingletonScope();
      Kernel.Bind<INavigationProvider>().To<NavigationProvider>().InSingletonScope();
    }

    #endregion Methods
  }
}