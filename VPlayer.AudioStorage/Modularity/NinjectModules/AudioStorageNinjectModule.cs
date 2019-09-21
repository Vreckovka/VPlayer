using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCore.Modularity.NinjectModules;
using VPlayer.AudioInfoDownloader;
using VPlayer.AudioStorage.AudioDatabase;
using VPlayer.AudioStorage.Interfaces;

namespace VPlayer.AudioStorage.Modularity.NinjectModules
{
    public class AudioStorageNinjectModule : BaseNinjectModule
    {
        public override void RegisterProviders()
        {
            base.RegisterProviders();

            Kernel.Bind<IStorageManager>().To<AudioDatabaseManager>().InSingletonScope();
            Kernel.BindToSelfInSingletonScope<AudioInfoDownloaderProvider>();
        }
    }
}
