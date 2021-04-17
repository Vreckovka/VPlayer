using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using IPTVStalker;
using IPTVStalker.Domain;
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
  public class TvPlayerViewModel : PlayableRegionViewModel<WindowsPlayerView, TvItemInPlaylistItemViewModel, TvPlaylist, TvPlaylistItem, TvItem>
  {
    private readonly IIptvStalkerServiceProvider iptvStalkerServiceProvider;
    private SerialDisposable channelLoadedSerialDisposable = new SerialDisposable();

    public TvPlayerViewModel(IRegionProvider regionProvider, IKernel kernel, ILogger logger,
      IStorageManager storageManager,
      IEventAggregator eventAggregator,
      IIptvStalkerServiceProvider iptvStalkerServiceProvider,
      IVlcProvider vlcProvider) : base(regionProvider, kernel, logger, storageManager, eventAggregator, vlcProvider)
    {
      this.iptvStalkerServiceProvider = iptvStalkerServiceProvider ?? throw new ArgumentNullException(nameof(iptvStalkerServiceProvider));
    }

    public override bool ContainsNestedRegions => true;
    public override string RegionName { get; protected set; } = RegionNames.WindowsPlayerContentRegion;
    public override string Header => "IPTV Player";

   

    #region OnSetActualItem

    public override void OnSetActualItem(TvItemInPlaylistItemViewModel itemViewModel, bool isPlaying)
    {
      if (isPlaying)
      {
        if (ActualItem != null)
        {
          channelLoadedSerialDisposable.Disposable?.Dispose();

          if (ActualItem.Model.Source == null)
            channelLoadedSerialDisposable.Disposable = ActualItem.ObservePropertyChange(x => x.Source).Subscribe(x => OnChannelLoaded());

          ActualItem.State = TVChannelState.GettingData;
        }
      }
      else
      {
        refreshSourceDisposable?.Dispose();
      }
    }

    #endregion

    #region OnActivation

    public override void OnActivation(bool firstActivation)
    {
      base.OnActivation(firstActivation);

      if (firstActivation)
      {
        var view = regionProvider.RegisterView<IPTVPlayerView, TvPlayerViewModel>(RegionNames.PlayerContentRegion, this, false, out var guid, RegionManager);
      }
    }

    #endregion

    #region OnChannelLoaded

    private IDisposable refreshSourceDisposable;
    private async void OnChannelLoaded()
    {
      if (ActualItem.Source != null)
      {
        Debug.WriteLine("tv play tv item: " + ActualItem.Source);

        libVLC?.Dispose();
        libVLC = new LibVLC();

        refreshSourceDisposable?.Dispose();

        ActualItem.State = TVChannelState.Loading;

        await SetVlcMedia(ActualItem.Model);

        await Play();
      }
      else
      {
        libVLC?.Dispose();
        MediaPlayer.Media = null;
        MediaPlayer?.Stop();
      }
    }

    #endregion


    protected override void OnVlcError()
    {
      if (ActualItem != null)
      {
        ActualItem.State = TVChannelState.Error;
        ActualItem.RefreshSource();
      }
        
    }

    protected override void OnMediaPlayerStopped()
    {
      if (MediaPlayer.Media != null)
      {
        ActualItem.State = TVChannelState.Error;
        ActualItem.RefreshConnection();
      }
    }

    protected override void OnEndReached()
    {
    }

  

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
      if (ActualItem != null)
      {
        if ((int)e.Cache == 100)
        {
          ActualItem.State = TVChannelState.Playing;
        }
        else
          ActualItem.State = TVChannelState.Loading;

        ActualItem.BufferingValue = e.Cache;
      }
    }

    #endregion

    #region Dispose

    protected override TvPlaylistItem GetNewPlaylistItemViewModel(TvItemInPlaylistItemViewModel itemViewModel, int index)
    {
      throw new NotImplementedException();
    }

    protected override void OnRemoveItemsFromPlaylist(DeleteType deleteType, RemoveFromPlaylistEventArgs<TvItemInPlaylistItemViewModel> args)
    {
      throw new NotImplementedException();
    }

    protected override void ItemsRemoved(EventPattern<TvItemInPlaylistItemViewModel> eventPattern)
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
