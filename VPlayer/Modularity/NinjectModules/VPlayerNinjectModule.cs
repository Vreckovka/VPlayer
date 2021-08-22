using System.Data.Common;
using Ninject;
using VCore.Modularity.NinjectModules;
using VCore.Standard.Modularity.NinjectModules;
using VPlayer.Core.Modularity.Ninject;
using VPlayer.Core.Providers;
using VPLayer.Domain.Contracts.CloudService.Providers;
using VPlayer.IPTV.Modularity;
using VPlayer.PCloud;
using VPlayer.Player.ViewModels;
using VPlayer.UPnP.Modularity;
using VPlayer.ViewModels;
using VPlayer.WindowsPlayer.Modularity.NinjectModule;
using VPlayer.WindowsPlayer.ViewModels;

namespace VPlayer.Modularity.NinjectModules
{
  public class VPlayerNinjectModule : BaseNinjectModule
  {
    public override void Load()
    {
      base.Load();

      if (Kernel != null)
      {

        Kernel.Bind<IBasicInformationProvider>().To<VPlayerBasicInformationProvider>().InSingletonScope();
        Kernel.Bind<ICloudService>().To<CloudService>()
          .InSingletonScope()
          .WithConstructorArgument(System.Configuration.ConfigurationManager.AppSettings["PCloudPath"])
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
