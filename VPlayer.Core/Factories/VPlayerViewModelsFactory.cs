using System;
using System.Collections.Generic;
using System.Text;
using Ninject;
using System.Linq;
using Ninject.Infrastructure;
using Ninject.Planning.Bindings;
using Logger;
using Prism.Events;
using VCore.WPF.Interfaces.Managers;
using VPlayer.Core.Interfaces.ViewModels;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.AudioStorage.InfoDownloader;
using VPlayer.AudioStorage.InfoDownloader.Clients.PCloud;
using VPlayer.AudioStorage.InfoDownloader.Clients.MusixMatch;
using Ninject.Parameters;
using VCore.Standard.Factories.ViewModels;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.DomainClasses.Video;
using VPlayer.Core.ViewModels;
using VPlayer.Core.ViewModels.SoundItems;
using VPlayer.Core.ViewModels.TvShows;
using VPLayer.Domain.Contracts.IPTV;
using VPlayer.IPTV.ViewModels;

namespace VPlayer.Core.Factories
{
  public class VPlayerViewModelsFactory : BaseViewModelsFactory, IVPlayerViewModelsFactory, ISavedSongViewsFactory, IIncomingPlaylistViewsFactory
  {
    public VPlayerViewModelsFactory(IKernel kernel) : base(kernel)
    {
    }

    internal const string DefaultFactoryMetadata="VPlayerDefaultViewFactory";
    public const string SavedSongWindowConstructorMetadata="VPlayerSavedSongWindowConstructor";
    private static readonly Type[] SongServices={typeof(IEventAggregator),typeof(IAlbumsViewModel),
      typeof(IArtistsViewModel),typeof(AudioInfoDownloader),typeof(PCloudLyricsProvider),typeof(ILogger),
      typeof(IStorageManager),typeof(IWindowManager),typeof(MusixMatchLyricsProvider)};

    public IEnumerable<SoundItemInPlaylistViewModel> CreateIncomingPlaylistViews(IEnumerable<PlaylistSoundItem> rows)
    {
      if(rows==null) throw new ArgumentNullException(nameof(rows));
      return CreateIncomingPlaylistViewsCore(rows);
    }

    private IEnumerable<SoundItemInPlaylistViewModel> CreateIncomingPlaylistViewsCore(IEnumerable<PlaylistSoundItem> rows)
    {
      Func<SoundItem,SoundItemInPlaylistViewModel> create=null;
      foreach(var row in rows)
      {
        if(create==null)
        {
          if(CanShareIncomingServices())
          {
            var events=kernel.Get<IEventAggregator>();
            var storage=kernel.Get<IStorageManager>();
            create=item=>new SoundItemInPlaylistViewModel(item,events,storage);
          }
          else create=item=>((IViewModelsFactory)this).Create<SoundItemInPlaylistViewModel>(item);
        }
        yield return create(row.ReferencedItem);
      }
    }

    private bool CanShareIncomingServices()
    {
      // Custom views, factories and contextual/transient services keep container activation per row.
      if(GetType()!=typeof(VPlayerViewModelsFactory) ||
        kernel.GetBindings(typeof(SoundItemInPlaylistViewModel)).Any(binding=>!binding.IsImplicit)) return false;
      var factories=kernel.GetBindings(typeof(IViewModelsFactory)).ToArray();
      if(factories.Length!=1 || factories[0].IsConditional ||
        factories[0].ScopeCallback!=StandardScopeCallbacks.Transient ||
        !factories[0].Metadata.Has(DefaultFactoryMetadata) ||
        !factories[0].Metadata.Get<bool>(DefaultFactoryMetadata)) return false;
      foreach(var service in new[] {typeof(IEventAggregator),typeof(IStorageManager)})
      {
        var bindings=kernel.GetBindings(service).ToArray();
        if(bindings.Length!=1 || bindings[0].IsConditional ||
          (bindings[0].Target!=BindingTarget.Constant && bindings[0].ScopeCallback!=StandardScopeCallbacks.Singleton))
          return false;
      }
      return true;
    }
    public IEnumerable<SoundItemInPlaylistViewModel> CreateSavedSongViews(IEnumerable<PlaylistSoundItem> rows)
    {
      if(rows==null) throw new ArgumentNullException(nameof(rows));
      return CreateSavedSongViewsCore(rows);
    }

    private IEnumerable<SoundItemInPlaylistViewModel> CreateSavedSongViewsCore(IEnumerable<PlaylistSoundItem> rows)
    {
      // Resolve once per enumeration, and only when there is an actual occurrence.
      Func<Song,SongInPlayListViewModel> create=null;
      foreach(var row in rows)
      {
        if(create==null)
          create=CanShareSongServices()?CreateSongConstructor():song=>((IViewModelsFactory)this).Create<SongInPlayListViewModel>(song);
        yield return create(new Song {ItemModel=row.ReferencedItem});
      }
    }

    private bool CanShareSongServices()
    {
      // Explicit item bindings can carry custom providers, injection or activation.
      // Conditional services and transient services other than window managers
      // retain container creation. Window-manager lifetime is handled per occurrence.
      if(GetType()!=typeof(VPlayerViewModelsFactory)) return RecordBatchFallback("factory type");
      if(kernel.GetBindings(typeof(SongInPlayListViewModel)).Any(binding=>!binding.IsImplicit)) return RecordBatchFallback("view binding");
      var factories=kernel.GetBindings(typeof(IViewModelsFactory)).ToArray();
      if(factories.Length!=1 || factories[0].IsConditional ||
        factories[0].ScopeCallback!=StandardScopeCallbacks.Transient ||
        !factories[0].Metadata.Has(DefaultFactoryMetadata) ||
        !factories[0].Metadata.Get<bool>(DefaultFactoryMetadata)) return RecordBatchFallback("factory binding");
      foreach(var service in SongServices)
      {
        var bindings=kernel.GetBindings(service).ToArray();
        if(bindings.Length!=1 || bindings[0].IsConditional ||
          (bindings[0].Target!=BindingTarget.Constant && bindings[0].ScopeCallback!=StandardScopeCallbacks.Singleton &&
           !(service==typeof(IWindowManager) && HasDefaultWindowConstructor(bindings[0]))))
          return RecordBatchFallback(service.Name);
      }
      VPLayer.Domain.Diagnostics.StartupMeasurements.RecordObservation("UI / saved song views / batch constructor",1);
      return true;
    }

    private static bool HasDefaultWindowConstructor(IBinding binding) =>
      binding.ScopeCallback==StandardScopeCallbacks.Transient &&
      binding.Metadata.Has(SavedSongWindowConstructorMetadata) &&
      binding.Metadata.Get<object>(SavedSongWindowConstructorMetadata) is Func<IWindowManager> &&
      !binding.ActivationActions.Any() && !binding.DeactivationActions.Any() && !binding.Parameters.Any();

    private bool RecordBatchFallback(string reason)
    {
      VPLayer.Domain.Diagnostics.StartupMeasurements.RecordObservation("UI / saved song views / fallback "+reason,1);
      return false;
    }

    private Func<Song,SongInPlayListViewModel> CreateSongConstructor()
    {
      var events=kernel.Get<IEventAggregator>();
      var albums=kernel.Get<IAlbumsViewModel>();
      var artists=kernel.Get<IArtistsViewModel>();
      var downloader=kernel.Get<AudioInfoDownloader>();
      var cloud=kernel.Get<PCloudLyricsProvider>();
      var logger=kernel.Get<ILogger>();
      var storage=kernel.Get<IStorageManager>();
      var windowBinding=kernel.GetBindings(typeof(IWindowManager)).Single();
      Func<IWindowManager> windows;
      if(windowBinding.Target!=BindingTarget.Constant && windowBinding.ScopeCallback==StandardScopeCallbacks.Transient)
      {
        // The app supplies its known parameterless constructor. Custom activation,
        // deactivation or constructor parameters still go through the container.
        if(windowBinding.Metadata.Has(SavedSongWindowConstructorMetadata) &&
          !windowBinding.ActivationActions.Any() && !windowBinding.DeactivationActions.Any() && !windowBinding.Parameters.Any())
        {
          windows=windowBinding.Metadata.Get<Func<IWindowManager>>(SavedSongWindowConstructorMetadata);
          VPLayer.Domain.Diagnostics.StartupMeasurements.RecordObservation("UI / saved song views / direct window constructor",1);
        }
        else windows=()=>kernel.Get<IWindowManager>();
      }
      else
      {
        var shared=kernel.Get<IWindowManager>();
        windows=()=>shared;
      }
      var lyrics=kernel.Get<MusixMatchLyricsProvider>();
      // Preserve the default per-item factory lifetime and its constructor cache.
      return song=>new SongInPlayListViewModel(events,albums,artists,downloader,cloud,song,
        logger,storage,windows(),new VPlayerViewModelsFactory(kernel),lyrics);
    }

    public TvShowEpisodeInPlaylistViewModel CreateTvShowEpisodeInPlayList(VideoItem videoItem, TvShowEpisode tvShowEpisode)
    {
      var pVideoItem = new ConstructorArgument("model", videoItem);
      var ptvShowEpisode = new ConstructorArgument("tvShowEpisode", tvShowEpisode);

      return kernel.Get<TvShowEpisodeInPlaylistViewModel>(pVideoItem, ptvShowEpisode);
    }

    public SongInPlayListViewModel CreateSongInPlayListViewModel(SoundItem soundItem, Song song)
    {
      var pVideoItem = new ConstructorArgument("soundItem", soundItem);
      var ptvShowEpisode = new ConstructorArgument("model", song);

      return kernel.Get<SongInPlayListViewModel>(pVideoItem, ptvShowEpisode);
    }

    public TvItemInPlaylistItemViewModel CreateTvItemInPlaylistItemViewModel(TvItem model, ITvPlayableItem tvPlayableItem)
    {
      var pVideoItem = new ConstructorArgument(nameof(model), model);
      var ptvShowEpisode = new ConstructorArgument(nameof(tvPlayableItem), tvPlayableItem);

      return kernel.Get<TvItemInPlaylistItemViewModel>(pVideoItem, ptvShowEpisode);
    }
  }
}
