using Ninject;
using VCore.Modularity.NinjectModules;
using VPlayer.WindowsPlayer.Modularity.NinjectModule;

namespace VPlayer.Modularity.NinjectModules
{
  public class VPlayerNinjectModule : BaseNinjectModule
  {
    public override void Load()
    {
      base.Load();

      Kernel.Load<WindowsPlayerNinjectModule>();
    }
  }
}
