using System.Data.Common;
using Ninject;
using VCore.Modularity.NinjectModules;
using VCore.Standard.Modularity.NinjectModules;
using VPlayer.Core.Modularity.Ninject;
using VPlayer.Player.ViewModels;
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
        Kernel.Load<VPlayerCoreModule>();
       
        Kernel.Load<WindowsPlayerNinjectModule>();
      
        Kernel.BindToSelfInSingletonScope<PlayerViewModel>();
       
      }
    }
  }
}
