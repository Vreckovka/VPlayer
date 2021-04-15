using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive;
using System.Reactive.Disposables;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using LibVLCSharp.Shared;
using Logger;
using Ninject;
using Prism.Events;
using VCore.Helpers;
using VCore.Modularity.RegionProviders;
using VCore.Standard;
using VCore.Standard.ViewModels.TreeView;
using VPlayer.AudioStorage.DomainClasses.IPTV;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Events;
using VPlayer.Core.Modularity.Regions;
using VPlayer.Core.Providers;
using VPlayer.Core.ViewModels;
using VPlayer.Player.Views.WindowsPlayer;
using VPlayer.WindowsPlayer.ViewModels;
using VPlayer.WindowsPlayer.Views.WindowsPlayer;
using VPlayer.WindowsPlayer.Views.WindowsPlayer.IPTV;
using MediaPlayer = LibVLCSharp.Shared.MediaPlayer;

namespace VPlayer.IPTV.ViewModels
{
  public class TvPlayerViewModel : PlayableRegionViewModel<WindowsPlayerView, TvPlaylistItemViewModel, TvPlaylist, TvPlaylistItem, TvPlaylistItem>
  {
    private SerialDisposable channelLoadedSerialDisposable = new SerialDisposable();

    public TvPlayerViewModel(IRegionProvider regionProvider, IKernel kernel, ILogger logger,
      IStorageManager storageManager,
      IEventAggregator eventAggregator,
      IVlcProvider vlcProvider) : base(regionProvider, kernel, logger, storageManager, eventAggregator, vlcProvider)
    {
    }

    public override bool ContainsNestedRegions => true;
    public override string RegionName { get; protected set; } = RegionNames.WindowsPlayerContentRegion;

    public override string Header => "IPTV Player";

    #region OnSetActualItem

    public override void OnSetActualItem(TvPlaylistItemViewModel itemViewModel, bool isPlaying)
    {
      if (ActualItem != null)
      {
        if (ActualItem.Model.Source == null)
          channelLoadedSerialDisposable.Disposable = ActualItem.ObservePropertyChange(x => x.Source).Subscribe(x => OnChannelLoaded());

        ActualItem.State = TVChannelState.Loading;
      }

      base.OnSetActualItem(itemViewModel, isPlaying);
    }

    #endregion

    #region OnActivation

    public override void OnActivation(bool firstActivation)
    {
      base.OnActivation(firstActivation);

      if (firstActivation)
      {
        var view = regionProvider.RegisterView<VideoPlayerView, TvPlayerViewModel>(RegionNames.PlayerContentRegion, this, false, out var guid, RegionManager);
      }
    }

    #endregion

    #region OnChannelLoaded

    private void OnChannelLoaded()
    {
      channelLoadedSerialDisposable.Disposable?.Dispose();
      SetItemAndPlay(0,true);
    }

    #endregion

    #region HookToVlcEvents

    protected override async Task HookToVlcEvents()
    {
      await base.HookToVlcEvents();

      MediaPlayer.Buffering += MediaPlayer_Buffering;

    }

    #endregion

    #region MediaPlayer_Buffering

    private void MediaPlayer_Buffering(object sender, MediaPlayerBufferingEventArgs e)
    {
      if ((int)e.Cache == 100)
      {
        ActualItem.State = TVChannelState.Playing;
      }
      else
        ActualItem.State = TVChannelState.Loading;

      ActualItem.BufferingValue = e.Cache;
    }

    #endregion
    
    #region Dispose

    protected override TvPlaylistItem GetNewPlaylistItemViewModel(TvPlaylistItemViewModel itemViewModel, int index)
    {
      throw new NotImplementedException();
    }

    protected override void OnRemoveItemsFromPlaylist(DeleteType deleteType, RemoveFromPlaylistEventArgs<TvPlaylistItemViewModel> args)
    {
      throw new NotImplementedException();
    }

    protected override void ItemsRemoved(EventPattern<TvPlaylistItemViewModel> eventPattern)
    {
      throw new NotImplementedException();
    }

    protected override void FilterByActualSearch(string predictate)
    {
      throw new NotImplementedException();
    }

    protected override TvPlaylist GetNewPlaylistModel(List<TvPlaylistItem> playlistModels, bool isUserCreated)
    {
      throw new NotImplementedException();
    }

    public override void Dispose()
    {
      base.Dispose();

      channelLoadedSerialDisposable?.Dispose();
    } 

    #endregion

  }
}
