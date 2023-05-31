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
using System.Windows.Input;
using IPTVStalker;
using IPTVStalker.Domain;
using LibVLCSharp.Shared;
using Logger;
using Ninject;
using Prism.Events;
using VCore;

using VCore.Standard;
using VCore.Standard.Factories.ViewModels;
using VCore.Standard.Modularity.Interfaces;
using VCore.Standard.ViewModels.TreeView;
using VCore.WPF.Helpers;
using VCore.WPF.Interfaces.Managers;
using VCore.WPF.Managers;
using VCore.WPF.Misc;
using VCore.WPF.Modularity.RegionProviders;
using VPlayer.AudioStorage.DomainClasses.IPTV;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Events;
using VPlayer.Core.Managers.Status;
using VPlayer.Core.Modularity.Regions;
using VPlayer.Core.ViewModels;
using VPLayer.Domain.Contracts.IPTV;
using VPlayer.WindowsPlayer.Players;
using VVLC.Players;
using MediaPlayer = LibVLCSharp.Shared.MediaPlayer;

namespace VPlayer.IPTV.ViewModels
{
  public class TvPlayerViewModel<TView> : PlayableRegionViewModel<TView, TvItemInPlaylistItemViewModel, TvPlaylist, TvPlaylistItem, TvItem> where TView : class, IView
  {
    private readonly IIptvStalkerServiceProvider iptvStalkerServiceProvider;
    private SerialDisposable channelLoadedSerialDisposable = new SerialDisposable();
    private TaskCompletionSource<bool> loadedTask = new TaskCompletionSource<bool>();

    public TvPlayerViewModel(IRegionProvider regionProvider, IKernel kernel, ILogger logger,
      IStorageManager storageManager,
      IEventAggregator eventAggregator,
      IIptvStalkerServiceProvider iptvStalkerServiceProvider,
      IWindowManager windowManager,
      IStatusManager statusManager,
      IViewModelsFactory viewModels,
      VLCPlayer vLCPlayer) : base(regionProvider, kernel, logger, storageManager, eventAggregator,statusManager,viewModels, windowManager, vLCPlayer)
    {
      this.iptvStalkerServiceProvider = iptvStalkerServiceProvider ?? throw new ArgumentNullException(nameof(iptvStalkerServiceProvider));
    }

    public override bool ContainsNestedRegions => true;
    public override string RegionName { get; protected set; } = RegionNames.WindowsPlayerContentRegion;
    public override string Header => "IPTV player";

    #region Commands

    #region VideoViewInitlized

    private ActionCommand videoViewInitlized;

    public ICommand VideoViewInitlized
    {
      get
      {
        if (videoViewInitlized == null)
        {
          videoViewInitlized = new ActionCommand(OnVideoViewInitlized);
        }

        return videoViewInitlized;
      }
    }

    public void OnVideoViewInitlized()
    {
      if (!loadedTask.Task.IsCompleted)
        loadedTask.SetResult(true);
    }

    #endregion

    #endregion

    #region WaitForInitilization

    protected override Task WaitForVlcInitilization()
    {
      return loadedTask.Task;
    }

    #endregion 

    #region OnSetActualItem

    public override void OnSetActualItem(TvItemInPlaylistItemViewModel itemViewModel, bool isPlaying)
    {
      if (isPlaying)
      {
        if (ActualItem != null)
        {
          channelLoadedSerialDisposable.Disposable?.Dispose();
          channelLoadedSerialDisposable.Disposable = ActualItem.ObservePropertyChange(x => x.Source).Subscribe(x => OnChannelLoaded());

          ActualItem.State = TVChannelState.GettingData;

          MediaPlayer.Media = null;
          MediaPlayer.Stop();
        }
      }
      else
      {
        refreshSourceDisposable?.Dispose();
      }
    }

    #endregion

    #region OnChannelLoaded

    private IDisposable refreshSourceDisposable;
    private async void OnChannelLoaded()
    {
      if (ActualItem.Source != null)
      {
        refreshSourceDisposable?.Dispose();

        ActualItem.State = TVChannelState.Loading;

        await SetMedia(ActualItem.Model);

        await Play();
      }
      else
      {
        MediaPlayer.Media = null;
        MediaPlayer?.Stop();
      }
    }

    #endregion

    #region OnVlcError

    protected override void OnVlcError()
    {
      if (ActualItem != null)
      {
        ActualItem.State = TVChannelState.Error;
        ActualItem.RefreshSource();
      }
    }

    #endregion

    #region OnMediaPlayerStopped

    protected override void OnMediaPlayerStopped()
    {
      if (MediaPlayer.Media != null)
      {
        ActualItem.State = TVChannelState.Error;

        Task.Run(() =>
        {
          Thread.Sleep(1500);

          if (!MediaPlayer.IsPlaying)
            ActualItem.RefreshConnection();
        });

      }
    }

    #endregion

    #region HookToVlcEvents

    protected override async void HookToPlayerEvents()
    {
      base.HookToPlayerEvents();

      MediaPlayer.Buffering += MediaPlayer_Buffering;

    }

    #endregion

    #region MediaPlayer_Buffering

    private void MediaPlayer_Buffering(object sender, PlayerBufferingEventArgs e)
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
