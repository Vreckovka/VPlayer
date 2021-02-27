using VCore.Standard.Modularity.NinjectModules;
using VPlayer.AudioStorage.AudioDatabase;
using VPlayer.AudioStorage.InfoDownloader;
using VPlayer.AudioStorage.InfoDownloader.LRC.Clients.Google;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.AudioStorage.Parsers;

namespace VPlayer.AudioStorage.Modularity.NinjectModules
{
  public class AudioStorageNinjectModule : BaseNinjectModule
  {
    private string googleApi = "D:\\Google drive API\\client_secret_907316640180-haiji0coibj73q45gk9tdern7i1f5hsd.apps.googleusercontent.com.json";

    #region Methods

    public override void RegisterProviders()
    {
      base.RegisterProviders();

      Kernel.Bind<IStorageManager>().To<AudioDatabaseManager>().InSingletonScope();

      Kernel.Bind<IGoogleDriveServiceProvider>().To<GoogleDriveServiceProvider>().InSingletonScope().WithConstructorArgument("keyPath", googleApi);

      Kernel.BindToSelf<GoogleDriveLrcProvider>().InSingletonScope();
      Kernel.BindToSelf<AudioInfoDownloader>().InSingletonScope();

      Kernel.Bind<ICSFDWebsiteScrapper>().To<CSFDWebsiteScrapper>();
    }

    #endregion Methods
  }
}