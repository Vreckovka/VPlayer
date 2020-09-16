using VCore.Modularity.NinjectModules;
using VPlayer.AudioStorage.AudioDatabase;
using VPlayer.AudioStorage.InfoDownloader;
using VPlayer.AudioStorage.InfoDownloader.LRC;
using VPlayer.AudioStorage.InfoDownloader.LRC.Clients.Google;
using VPlayer.AudioStorage.Interfaces.Storage;

namespace VPlayer.AudioStorage.Modularity.NinjectModules
{
  public class AudioStorageNinjectModule : BaseNinjectModule
  {
    #region Methods

    public override void RegisterProviders()
    {
      base.RegisterProviders();

      Kernel.Bind<IStorageManager>().To<AudioDatabaseManager>().InSingletonScope();

      Kernel.BindToSelf<GoogleDriveLrcProvider>().InSingletonScope();
      Kernel.BindToSelf<AudioInfoDownloader>().InSingletonScope();
    }

    #endregion Methods
  }
}