using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Listener;
using Logger;
using VCore.Modularity.NinjectModules;

namespace VPlayer.Core.Modularity.Ninject
{
  public class VPlayerCoreModule : BaseNinjectModule
  {
    public override void RegisterProviders()
    {
      base.RegisterProviders();

      Kernel.BindToSelfInSingletonScope<KeyListener>();

      Kernel.Bind<ILogger>().To<Logger.Logger>();
      Kernel.Bind<ILoggerContainer>().To<Logger.ConsoleLogger>();
    }
  }
}
