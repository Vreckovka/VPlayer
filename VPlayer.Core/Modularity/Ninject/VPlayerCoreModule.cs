using Listener;
using Logger;
using VCore.Standard.Modularity.NinjectModules;
using VPlayer.Core.Managers.Status;

namespace VPlayer.Core.Modularity.Ninject
{
  public class VPlayerCoreModule : BaseNinjectModule
  {

    private string logFilePath = "Loggs\\logs.txt";

    public override void RegisterProviders()
    {
      base.RegisterProviders();

      Kernel.BindToSelfInSingletonScope<KeyListener>();

      Kernel.Bind<ILogger>().To<Logger.Logger>();
      Kernel.Bind<ILoggerContainer>().To<Logger.ConsoleLogger>();


      Kernel.Bind<FileLoggerContainer>().ToSelf().WithConstructorArgument("logFilePath", logFilePath);
      Kernel.Bind<IStatusManager>().To<StatusManager>().InSingletonScope();
    }
  }
}
