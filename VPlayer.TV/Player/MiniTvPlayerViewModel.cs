using System;
using Logger;
using Ninject;
using Prism.Events;

using VCore.Standard.Factories.ViewModels;
using VCore.Standard.Helpers;
using VCore.WPF.Interfaces.Managers;
using VCore.WPF.Managers;
using VCore.WPF.Modularity.RegionProviders;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Factories;
using VPlayer.Core.Managers.Status;
using VPLayer.Domain.Contracts.IPTV;
using VPlayer.IPTV.Events;
using VPlayer.IPTV.Views;
using VPlayer.WindowsPlayer.Players;
using VVLC.Players;

namespace VPlayer.IPTV.ViewModels
{
  public class MiniTvPlayerViewModel : TvPlayerViewModel<IPTVManagerView>
  {
    private readonly IVPlayerViewModelsFactory viewModelsFactory;

    public MiniTvPlayerViewModel(IRegionProvider regionProvider, IKernel kernel, ILogger logger,
      IStorageManager storageManager,
      IEventAggregator eventAggregator,
      IVPlayerViewModelsFactory viewModelsFactory,
      IIptvStalkerServiceProvider iptvStalkerServiceProvider,
      IWindowManager windowManager,
      IStatusManager statusManager,
      VLCPlayer vLCPlayer) : base(regionProvider, kernel, logger, storageManager, eventAggregator, iptvStalkerServiceProvider,windowManager,statusManager, vLCPlayer)
    {
      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));
    }


    protected override void HookToPubSubEvents()
    {
      eventAggregator.GetEvent<PlayChannelEvent>().Subscribe(PlayChannel).DisposeWith(this);
    }

    private void PlayChannel(ITvChannel tvChannel)
    {
      PlayList.Clear();

      var vm = viewModelsFactory.CreateTvItemInPlaylistItemViewModel((TvItem)tvChannel.TvItem,tvChannel);
      
      PlayList.Add(vm);

      SetItemAndPlay(0);
    }
  }
}