using Ninject;
using Ninject.Activation;
using Ninject.Activation.Strategies;
using VCore.Standard.Modularity.NinjectModules;
using VPlayer.Core.ViewModels;
using VPlayer.Home.Modularity.NinjectModule;
using VPlayer.IPTV.ViewModels;
using VPlayer.WindowsPlayer.ViewModels;

namespace VPlayer.WindowsPlayer.Modularity.NinjectModule
{
  public class WindowsPlayerNinjectModule : BaseNinjectModule
  {
    #region Methods

    public override void Load()
    {
      using (VPLayer.Domain.Diagnostics.StartupMeasurements.Measure("Application / library module")) Kernel.Load<LibraryNinjectModule>();

      base.Load();
    }

    public override void RegisterViewModels()
    {
      base.RegisterViewModels();

      Kernel.Bind<VideoPlayerViewModel>().ToSelf().InSingletonScope();
      Kernel.Bind<MusicPlayerViewModel>().ToSelf().InSingletonScope();
      Kernel.Bind<WindowsIPTVPlayer>().ToSelf().InSingletonScope();

      VideoPlayerViewModel videoPlayerViewModel;
      using (VPLayer.Domain.Diagnostics.StartupMeasurements.Measure("Application / video player activation"))
        videoPlayerViewModel = Kernel.Get<VideoPlayerViewModel>();
      MusicPlayerViewModel musicPlayerViewModel;
      using (VPLayer.Domain.Diagnostics.StartupMeasurements.Measure("Application / music player activation"))
        musicPlayerViewModel = Kernel.Get<MusicPlayerViewModel>();
      //var tvPlayerViewModel = Kernel.Get<WindowsIPTVPlayer>();

      Kernel.Bind<IPlayableRegionViewModel>().ToConstant(videoPlayerViewModel);
      Kernel.Bind<IPlayableRegionViewModel>().ToConstant(musicPlayerViewModel);
      //Kernel.Bind<IPlayableRegionViewModel>().ToConstant(tvPlayerViewModel);

    }

    public override void RegisterViews()
    {
      base.RegisterViewModels();
    }


    #endregion Methods
  }
}
