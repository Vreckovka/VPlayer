using Listener;
using Ninject.Extensions.Factory;
using VCore.Standard.Factories.ViewModels;
using VCore.Standard.Modularity.NinjectModules;
using VPlayer.Core.Factories;
using VPlayer.Core.Managers.Status;
using VPlayer.Core.Providers;

namespace VPlayer.Core.Modularity.Ninject
{
  public class VPlayerCoreModule : BaseNinjectModule
  {



    public override void RegisterProviders()
    {
      base.RegisterProviders();

      Kernel.BindToSelfInSingletonScope<KeyListener>();


      Kernel.Bind<IVPlayerViewModelsFactory>().To<VPlayerViewModelsFactory>();
      Kernel.Rebind<IViewModelsFactory>().To<VPlayerViewModelsFactory>();

      Kernel.Bind<IVlcProvider>().To<VlcProvider>().InSingletonScope();
      Kernel.Bind<IStatusManager>().To<StatusManager>().InSingletonScope();
    }
  }
}
