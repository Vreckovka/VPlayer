using Ninject;
using VCore.Standard.Modularity.NinjectModules;
using VPlayer.Core.Modularity.Ninject;
using VPlayer.Player.ViewModels;
using VPlayer.WebPlayer.ViewModels;
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


        Kernel.BindToSelfInSingletonScope<WindowsPlayerViewModel>();
        Kernel.BindToSelfInSingletonScope<WebPlayerViewModel>();
        Kernel.BindToSelfInSingletonScope<PlayerViewModel>();
        
      }
    }
  }
}
