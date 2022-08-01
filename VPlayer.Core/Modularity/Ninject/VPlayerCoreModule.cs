using Listener;
using Ninject.Extensions.Factory;
using VCore.Standard.Factories.ViewModels;
using VCore.Standard.Modularity.NinjectModules;
using VFfmpeg;
using VPlayer.Core.Factories;
using VPlayer.Core.Managers.Status;
using VVLC.Providers;

namespace VPlayer.Core.Modularity.Ninject
{
  public class VPlayerCoreModule : BaseNinjectModule
  {



    public override void RegisterProviders()
    {
      base.RegisterProviders();



      Kernel.Bind<IVFfmpegProvider>().To<VFfmpegProvider>().InSingletonScope().WithConstructorArgument("ffmpegFolderPath", "ffmpeg");
      Kernel.Bind<IVPlayerViewModelsFactory>().To<VPlayerViewModelsFactory>();
      Kernel.Rebind<IViewModelsFactory>().To<VPlayerViewModelsFactory>();

      Kernel.Bind<IVlcProvider>().To<VlcProvider>().InSingletonScope();
      Kernel.Bind<IStatusManager>().To<VPlayerStatusManager>().InSingletonScope();
    }
  }
}
