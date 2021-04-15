using System;
using System.Collections.Generic;
using System.Text;
using VCore.Standard.Modularity.NinjectModules;
using VPlayer.IPTV.ViewModels;

namespace VPlayer.IPTV.Modularity
{
  public class IPTVModule : BaseNinjectModule
  {
    public override void RegisterViewModels()
    {
      base.RegisterViewModels();

    }

    public override void RegisterProviders()
    {
      Kernel.Bind<IIptvStalkerServiceProvider>().To<IptvStalkerServiceProvider>().InSingletonScope();

    }
  }
}
