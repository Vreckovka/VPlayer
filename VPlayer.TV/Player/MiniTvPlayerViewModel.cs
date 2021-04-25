using System;
using Logger;
using Ninject;
using Prism.Events;
using VCore.Modularity.RegionProviders;
using VCore.Standard.Factories.ViewModels;
using VCore.Standard.Helpers;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Factories;
using VPLayer.Domain.Contracts.IPTV;
using VPlayer.IPTV.Events;
using VPlayer.IPTV.Views;
using VPlayer.WindowsPlayer.Players;

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
      VLCPlayer vLCPlayer) : base(regionProvider, kernel, logger, storageManager, eventAggregator, iptvStalkerServiceProvider, vLCPlayer)
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