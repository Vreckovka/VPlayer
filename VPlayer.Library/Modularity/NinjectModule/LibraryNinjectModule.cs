using Ninject;
using VCore.Modularity.RegionProviders;
using VCore.Standard.Modularity.NinjectModules;
using VPlayer.AudioStorage.Modularity.NinjectModules;
using VPlayer.Core.Interfaces.ViewModels;
using VPlayer.Core.Modularity.Regions;
using VPlayer.Library.ViewModels;
using VPlayer.Library.ViewModels.AlbumsViewModels;
using VPlayer.Library.ViewModels.ArtistsViewModels;
using VPlayer.Library.ViewModels.TvShows;

namespace VPlayer.Library.Modularity.NinjectModule
{
  public class LibraryNinjectModule : BaseNinjectModule
  {
    public override void Load()
    {
      base.Load();
      Kernel.Load<AudioStorageNinjectModule>();

      Kernel.Rebind<IRegionProvider>().To<VPlayerLibraryRegionProvider>().InSingletonScope();

      var provider = Kernel.Get<IRegionProvider>() as IVPlayerRegionProvider;

      Kernel.Bind<IVPlayerRegionProvider>().ToConstant(provider);

    }

    public override void RegisterViewModels()
    {
      base.RegisterViewModels();

      Kernel.Bind<IAlbumsViewModel>().To<AlbumsViewModel>().InSingletonScope();
      Kernel.Bind<IArtistsViewModel>().To<ArtistsViewModel>().InSingletonScope();
      Kernel.Bind<ITvShowsViewModel>().To<TvShowsViewModel>().InSingletonScope();

      Kernel.Bind<SongPlaylistsViewModel>().ToSelf().InSingletonScope();
      Kernel.Bind<TvShowPlaylistsViewModel>().ToSelf().InSingletonScope();



      Kernel.BindToSelf<AlbumDetailViewModel>();

      Kernel.BindToSelf<AlbumCoversViewModel>().WithConstructorArgument("regionName", AlbumCoversViewModel.GetRegionName());
    }
  }
}
