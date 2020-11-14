using Ninject;
using VCore.Standard.Modularity.NinjectModules;
using VPlayer.AudioStorage.Modularity.NinjectModules;
using VPlayer.Core.Interfaces.ViewModels;
using VPlayer.Core.Modularity.Regions;
using VPlayer.Library.ViewModels;
using VPlayer.Library.ViewModels.AlbumsViewModels;
using VPlayer.Library.ViewModels.ArtistsViewModels;

namespace VPlayer.Library.Modularity.NinjectModule
{
  public class LibraryNinjectModule : BaseNinjectModule
  {
    public override void Load()
    {
      base.Load();
      Kernel.Load<AudioStorageNinjectModule>();
      Kernel.Bind<IVPlayerRegionProvider>().To<VPlayerLibraryRegionProvider>();

    }

    public override void RegisterViewModels()
    {
      base.RegisterViewModels();

      Kernel.Bind<IAlbumsViewModel>().To<AlbumsViewModel>().InSingletonScope();
      Kernel.Bind<IArtistsViewModel>().To<ArtistsViewModel>().InSingletonScope();

      Kernel.Bind<PlaylistsViewModel>().ToSelf().InSingletonScope();

      Kernel.BindToSelf<AlbumDetailViewModel>()
        .WithConstructorArgument("regionName", AlbumDetailViewModel.GetRegionName());

      Kernel.BindToSelf<AlbumCoversViewModel>()
        .WithConstructorArgument("regionName", AlbumCoversViewModel.GetRegionName());
    }
  }
}
