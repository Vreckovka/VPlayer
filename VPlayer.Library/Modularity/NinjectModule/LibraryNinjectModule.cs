using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ninject;
using VCore.Modularity.NinjectModules;
using VPlayer.AudioStorage.Modularity.NinjectModules;
using VPlayer.Core.Interfaces.ViewModels;
using VPlayer.Core.Modularity.Regions;
using VPlayer.Library.ViewModels.AlbumsViewModels;
using VPlayer.Library.ViewModels.LibraryViewModels.ArtistsViewModels;

namespace VPlayer.Library.Modularity.NinjectModule
{
  public class LibraryNinjectModule : BaseNinjectModule
  {
    public override void Load()
    {
      base.Load();
      Kernel.Load<AudioStorageNinjectModule>();
      Kernel.Bind<IVPlayerRegionManager>().To<VPlayerLibraryRegionManager>();

    }

    public override void RegisterViewModels()
    {
      base.RegisterViewModels();

      Kernel.Bind<IAlbumsViewModel>().To<AlbumsViewModel>().InSingletonScope();
      Kernel.Bind<IArtistsViewModel>().To<ArtistsViewModel>().InSingletonScope();
    }
  }
}
