using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using System.Windows;
using Logger;
using Microsoft.EntityFrameworkCore;
using Prism.Events;
using VCore.Standard.Factories.ViewModels;
using VCore.WPF.Interfaces.Managers;
using VCore.WPF.ViewModels.WindowsFiles;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Events;
using VPlayer.Core.Modularity.Regions;
using VPlayer.Core.ViewModels;
using VPlayer.Core.ViewModels.SoundItems;
using VPlayer.Core.ViewModels.TvShows;
using VPLayer.Domain;
using VPlayer.Library.ViewModels;

namespace VPlayer.Home.ViewModels
{
  public class SongsPlaylistViewModel : FilePlaylistViewModel<SoundItemInPlaylistViewModel, SoundItemFilePlaylist, PlaylistSoundItem>
  {
    #region Fields

    private readonly IViewModelsFactory viewModelsFactory;
    private readonly SongPlaylistsViewModel songPlaylistsViewModel;
    private readonly IVPlayerCloudService vPlayerCloudService;
    private readonly IStorageManager storageManager;
    private readonly ILogger logger;

    #endregion

    #region Constructors

    public SongsPlaylistViewModel(
      SoundItemFilePlaylist model,
      IEventAggregator eventAggregator,
      IViewModelsFactory viewModelsFactory,
      SongPlaylistsViewModel songPlaylistsViewModel,
      IVPlayerCloudService vPlayerCloudService,
      IStorageManager storageManager,
      ILogger logger,
      IWindowManager windowManager) : base(model, eventAggregator, storageManager, windowManager)
    {
      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));
      this.songPlaylistsViewModel = songPlaylistsViewModel ?? throw new ArgumentNullException(nameof(songPlaylistsViewModel));
      this.vPlayerCloudService = vPlayerCloudService ?? throw new ArgumentNullException(nameof(vPlayerCloudService));
      this.storageManager = storageManager ?? throw new ArgumentNullException(nameof(storageManager));
      this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #endregion

    protected override void OnDetail()
    {
      throw new NotImplementedException();
    }

    #region GetItemsToPlay

    private SerialDisposable serialDisposable = new SerialDisposable();
    public override async Task<IEnumerable<SoundItemInPlaylistViewModel>> GetItemsToPlay()
    {
      var playlist = storageManager.GetRepository<SoundItemFilePlaylist>()
        .Include(x => x.PlaylistItems)
        .ThenInclude(x => x.ReferencedItem)
        .ThenInclude(x => x.FileInfo)
        .SingleOrDefault(x => x.Id == Model.Id);

      if (playlist != null)
      {
        Model = playlist;

        var playlistItems = playlist.PlaylistItems.OrderBy(x => x.OrderInPlaylist).ToList();

        return playlistItems.Select(x => viewModelsFactory.Create<SoundItemInPlaylistViewModel>(x.ReferencedItem));
      }

      return null;
    }

    #endregion

    public override void PublishPlayEvent(IEnumerable<SoundItemInPlaylistViewModel> viewModels, EventAction eventAction)
    {
      var e = new PlayItemsEventData<SoundItemInPlaylistViewModel>(viewModels, eventAction, IsShuffle, IsRepeating, Model.LastItemElapsedTime, Model);

      eventAggregator.GetEvent<PlayItemsEvent<SoundItem, SoundItemInPlaylistViewModel>>().Publish(e);
    }

    public override void PublishAddToPlaylistEvent(IEnumerable<SoundItemInPlaylistViewModel> viewModels)
    {
      var e = new PlayItemsEventData<SoundItemInPlaylistViewModel>(viewModels, EventAction.Add, this);

      eventAggregator.GetEvent<PlayItemsEvent<SoundItem, SoundItemInPlaylistViewModel>>().Publish(e);
    }
    
    public override void Dispose()
    {
      base.Dispose();

      serialDisposable?.Dispose();
    }
  }

  public class SoundItemWithPlaylistItem
  {
    public SoundItemInPlaylistViewModel SoundItemInPlaylist { get; set; }
    public PlaylistSoundItem PlaylistSoundItem { get; set; }
  }
}