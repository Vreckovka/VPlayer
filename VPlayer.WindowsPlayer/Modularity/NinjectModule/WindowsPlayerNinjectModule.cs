using Ninject;
using Ninject.Activation;
using Ninject.Activation.Strategies;
using VCore.Standard.Modularity.NinjectModules;
using VPlayer.Core.ViewModels;
using VPlayer.IPTV.ViewModels;
using VPlayer.Library.Modularity.NinjectModule;
using VPlayer.WindowsPlayer.ViewModels;

namespace VPlayer.WindowsPlayer.Modularity.NinjectModule
{
  public class WindowsPlayerNinjectModule : BaseNinjectModule
  {
  #region Methods

    public override void Load()
    {
      Kernel.Load<LibraryNinjectModule>();

      base.Load();
    }

    public override void RegisterViewModels()
    {
      base.RegisterViewModels();

      Kernel.Bind<VideoPlayerViewModel>().ToSelf().InSingletonScope();
      Kernel.Bind<MusicPlayerViewModel>().ToSelf().InSingletonScope();
      Kernel.Bind<TvPlayerViewModel>().ToSelf().InSingletonScope();

      var videoPlayerViewModel = Kernel.Get<VideoPlayerViewModel>();
      var musicPlayerViewModel = Kernel.Get<MusicPlayerViewModel>();
      var tvPlayerViewModel = Kernel.Get<TvPlayerViewModel>();

      Kernel.Bind<IPlayableRegionViewModel>().ToConstant(videoPlayerViewModel);
      Kernel.Bind<IPlayableRegionViewModel>().ToConstant(musicPlayerViewModel);
      Kernel.Bind<IPlayableRegionViewModel>().ToConstant(tvPlayerViewModel);

    }

    public override void RegisterViews()
    {
      base.RegisterViewModels();
    }


    #endregion Methods
  }
}