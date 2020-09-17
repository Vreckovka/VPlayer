using VCore.Modularity.NinjectModules;
using VPlayer.AudioStorage.AudioDatabase;
using VPlayer.AudioStorage.InfoDownloader;
using VPlayer.AudioStorage.InfoDownloader.LRC.Clients.Google;
using VPlayer.AudioStorage.Interfaces.Storage;

namespace VPlayer.AudioStorage.Modularity.NinjectModules
{
  public class AudioStorageNinjectModule : BaseNinjectModule
  {
    private string googleApi = "C:\\Users\\Roman Pecho\\Desktop\\vplayer-289518-7ec8818053a1.json";

    #region Methods

    public override void RegisterProviders()
    {
      base.RegisterProviders();

      Kernel.Bind<IStorageManager>().To<AudioDatabaseManager>().InSingletonScope();

      Kernel.Bind<IGoogleDriveServiceProvider>().To<GoogleDriveServiceProvider>().InSingletonScope().WithConstructorArgument("keyPath", googleApi);

      Kernel.BindToSelf<GoogleDriveLrcProvider>().InSingletonScope();
      Kernel.BindToSelf<AudioInfoDownloader>().InSingletonScope();
    }

    #endregion Methods
  }
}