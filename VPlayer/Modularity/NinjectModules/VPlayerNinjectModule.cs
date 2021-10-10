using System.Configuration;
using System.Data.Common;
using System.Reflection;
using Ninject;
using VCore.Modularity.NinjectModules;
using VCore.Standard.Modularity.NinjectModules;
using VCore.Standard.Providers;
using VPlayer.Core.Modularity.Ninject;
using VPLayer.Domain;
using VPLayer.Domain.Contracts.CloudService.Providers;
using VPlayer.IPTV.Modularity;
using VPlayer.PCloud;
using VPlayer.Player.ViewModels;
using VPlayer.Providers;
using VPlayer.UPnP.Modularity;
using VPlayer.ViewModels;
using VPlayer.WindowsPlayer.Modularity.NinjectModule;
using VPlayer.WindowsPlayer.ViewModels;

namespace VPlayer.Modularity.NinjectModules
{
  public class VPlayerInfoProvider : IVPlayerInfoProvider
  {
    public string GetApplicationVersion()
    {
      var assemlby = Assembly.GetExecutingAssembly();

      return BasicInformationProvider.GetFormattedBuildVersion(assemlby);
    }
  }

  public class VPlayerNinjectModule : BaseNinjectModule
  {
    public override void Load()
    {
      base.Load();

      if (Kernel != null)
      {
        Kernel.Bind<IVPlayerInfoProvider>().To<VPlayerInfoProvider>();

        Kernel.Bind<IVPlayerCloudService>().To<VPlayerCloudService>()
          .InSingletonScope()
          .WithConstructorArgument(ConfigurationManager.AppSettings["PCloudPath"])
          .OnActivation(x => x.Initilize()); ;

        Kernel.Bind<ICloudService>().To<VPlayerCloudService>()
          .InSingletonScope()
          .WithConstructorArgument(ConfigurationManager.AppSettings["PCloudPath"])
          .OnActivation(x => x.Initilize());

        Kernel.Load<VPlayerCoreModule>();

        Kernel.Load<IPTVModule>();
        Kernel.Load<UPnPNinjectModule>();

        Kernel.Load<WindowsPlayerNinjectModule>();

        Kernel.BindToSelfInSingletonScope<PlayerViewModel>();
       
      }
    }

    public override void RegisterViewModels()
    {
      base.RegisterViewModels();

      Kernel.Bind<MainWindowViewModel>().ToSelf().InSingletonScope();
     
    }
  }
}
