using VCore.Modularity.NinjectModules;
using VPlayer.AudioStorage.AudioDatabase;
using VPlayer.AudioStorage.Interfaces;

namespace VPlayer.AudioStorage.Modularity.NinjectModules
{
  public class AudioStorageNinjectModule : BaseNinjectModule
  {
    #region Methods

    public override void RegisterProviders()
    {
      base.RegisterProviders();

      Kernel.Bind<IStorageManager>().To<AudioDatabaseManager>().InSingletonScope();
      Kernel.BindToSelfAndInitialize<VPlayer.AudioInfoDownloader.AudioInfoDownloader>();
    }

    #endregion Methods
  }
}