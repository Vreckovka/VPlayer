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
  public class VPlayerViewModelsFactory : BaseViewModelsFactory, IVPlayerViewModelsFactory, ISavedSongViewsFactory
  {
    public VPlayerViewModelsFactory(IKernel kernel) : base(kernel)
    {
    }

    internal const string DefaultFactoryMetadata="VPlayerDefaultViewFactory";
    private static readonly Type[] SharedSongServices={typeof(IEventAggregator),typeof(IAlbumsViewModel),
      typeof(IArtistsViewModel),typeof(AudioInfoDownloader),typeof(PCloudLyricsProvider),typeof(ILogger),
      typeof(IStorageManager),typeof(IWindowManager),typeof(MusixMatchLyricsProvider)};

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
          create=CanShareSongServices()?CreateSongConstructor():song=>Create<SongInPlayListViewModel>(song);
        yield return create(new Song {ItemModel=row.ReferencedItem});
      }
    }

    private bool CanShareSongServices()
    {
      // Explicit item bindings can carry custom providers, injection or activation.
      // They and services with transient/conditional scopes retain container creation.
      if(GetType()!=typeof(VPlayerViewModelsFactory) ||
        kernel.GetBindings(typeof(SongInPlayListViewModel)).Any(binding=>!binding.IsImplicit)) return false;
      var factories=kernel.GetBindings(typeof(IViewModelsFactory)).ToArray();
      if(factories.Length!=1 || factories[0].IsConditional ||
        factories[0].ScopeCallback!=StandardScopeCallbacks.Transient ||
        !factories[0].Metadata.Has(DefaultFactoryMetadata) ||
        !factories[0].Metadata.Get<bool>(DefaultFactoryMetadata)) return false;
      foreach(var service in SharedSongServices)
      {
        var bindings=kernel.GetBindings(service).ToArray();
        if(bindings.Length!=1 || bindings[0].IsConditional ||
          (bindings[0].Target!=BindingTarget.Constant && bindings[0].ScopeCallback!=StandardScopeCallbacks.Singleton))
          return false;
      }
      return true;
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
      var windows=kernel.Get<IWindowManager>();
      var lyrics=kernel.Get<MusixMatchLyricsProvider>();
      // Preserve the default per-item factory lifetime and its constructor cache.
      return song=>new SongInPlayListViewModel(events,albums,artists,downloader,cloud,song,
        logger,storage,windows,new VPlayerViewModelsFactory(kernel),lyrics);
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
