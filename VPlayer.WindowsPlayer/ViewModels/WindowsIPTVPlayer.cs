using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Logger;
using Ninject;
using Prism.Events;
using VCore;
using VCore.Standard.Factories.ViewModels;
using VCore.WPF.Interfaces.Managers;
using VCore.WPF.Managers;
using VCore.WPF.Modularity.RegionProviders;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Managers.Status;
using VPlayer.Core.Modularity.Regions;
using VPlayer.IPTV.ViewModels;
using VPlayer.Player.Views.WindowsPlayer;
using VPlayer.WindowsPlayer.Players;
using VPlayer.WindowsPlayer.Views.WindowsPlayer.IPTV;
using VVLC.Players;

namespace VPlayer.WindowsPlayer.ViewModels
{
  public class WindowsIPTVPlayer : TvPlayerViewModel<WindowsPlayerView>
  {


    public WindowsIPTVPlayer(IRegionProvider regionProvider, IKernel kernel, ILogger logger,
      IStorageManager storageManager,
      IEventAggregator eventAggregator,
      IIptvStalkerServiceProvider iptvStalkerServiceProvider,
      IWindowManager windowManager,
      IStatusManager statusManager,
      IViewModelsFactory viewModelsFactory,
      VLCPlayer vLCPlayer) : base(regionProvider, kernel, logger, storageManager, eventAggregator, iptvStalkerServiceProvider, windowManager, statusManager, viewModelsFactory, vLCPlayer)
    {
    }



    #region Methods

    #region OnActivation

    public override void OnActivation(bool firstActivation)
    {
      base.OnActivation(firstActivation);

      if (firstActivation)
      {
        var view = regionProvider.RegisterView<IPTVPlayerView, WindowsIPTVPlayer>(RegionNames.PlayerContentRegion, this, false, out var guid, RegionManager);
      }
    }

    #endregion

    #endregion
  }
}
