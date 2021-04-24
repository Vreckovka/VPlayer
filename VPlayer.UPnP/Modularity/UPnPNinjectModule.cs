using System;
using System.Collections.Generic;
using System.Text;
using VCore.Standard.Modularity.NinjectModules;
using VPlayer.UPnP.ViewModels;

namespace VPlayer.UPnP.Modularity
{
  public class UPnPNinjectModule : BaseNinjectModule
  {
    public override void RegisterViewModels()
    {
      base.RegisterViewModels();

      Kernel.Bind<UPnPManagerViewModel>().ToSelf().InSingletonScope();
    }
  }
}
