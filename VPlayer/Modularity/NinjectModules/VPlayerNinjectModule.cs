using KeyListener;
using Ninject;
using VCore.Modularity.NinjectModules;
using VPlayer.Player.ViewModels;
using VPlayer.WindowsPlayer.Modularity.NinjectModule;

namespace VPlayer.Modularity.NinjectModules
{
  public class VPlayerNinjectModule : BaseNinjectModule
  {
    public override void Load()
    {
      base.Load();

      Kernel.Load<WindowsPlayerNinjectModule>();

      Kernel.BindToSelfInSingletonScope<PlayerViewModel>();
    }
  }
}
