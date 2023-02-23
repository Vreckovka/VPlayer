using Ninject;
using VCore.Standard.Modularity.NinjectModules;
using VCore.WPF.Modularity.RegionProviders;
using VPlayer.AudioStorage.Modularity.NinjectModules;
using VPlayer.Core.Interfaces.ViewModels;
using VPlayer.Core.Modularity.Regions;
using VPlayer.Core.ViewModels.FileBrowser.PCloud;
using VPlayer.Home.ViewModels;
using VPlayer.Home.ViewModels.Albums;
using VPlayer.Home.ViewModels.Artists;
using VPlayer.Home.ViewModels.FileBrowser;
using VPlayer.Home.ViewModels.Statistics;
using VPlayer.Home.ViewModels.TvShows;

namespace VPlayer.Home.Modularity.NinjectModule
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

      Kernel.Bind<SoundItemPlaylistsViewModel>().ToSelf().InSingletonScope();
      Kernel.Bind<VideoPlaylistsViewModel>().ToSelf().InSingletonScope();
      Kernel.Bind<StatisticsViewModel>().ToSelf().InSingletonScope();

      Kernel.Bind<WindowsFileBrowserViewModel>().ToSelf().InSingletonScope();
      Kernel.Bind<PCloudFileBrowserViewModel>().ToSelf().InSingletonScope();


      Kernel.BindToSelf<AlbumDetailViewModel>();

      Kernel.BindToSelf<AlbumCoversViewModel>().WithConstructorArgument("regionName", AlbumCoversViewModel.GetRegionName());
    }
  }
}
