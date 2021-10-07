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
      ILogger logger) : base(model, eventAggregator, storageManager)
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
        var playlistItems = playlist.PlaylistItems.OrderBy(x => x.OrderInPlaylist).ToList();

        var songsItems = storageManager.GetRepository<Song>()
          .Where(x => playlistItems.Select(y => y.IdReferencedItem).Contains(x.ItemModel.Id))
          .Include(x => x.Album)
          .ThenInclude(x => x.Artist)
          .Include(x => x.ItemModel)
          .ThenInclude(x => x.FileInfo)
          .ToList();

        List<FileInfo> sources = new List<FileInfo>();

        if (playlist.PlaylistType == PlaylistType.Cloud || playlistItems.Select(x => x.ReferencedItem.Source).Any(y => y.Contains("http")))
        {
          var fileInfos = playlistItems.Select(x => x.ReferencedItem.FileInfo).ToList();

          var itemSourcesProcess = vPlayerCloudService.GetItemSources(fileInfos);


          Application.Current.Dispatcher.Invoke(() =>
          {
            songPlaylistsViewModel.LoadingStatus.TotalProcessCount = itemSourcesProcess.InternalProcessesCount;
          });

          serialDisposable.Disposable = itemSourcesProcess.OnInternalProcessedCountChanged.Subscribe(x =>
          {
            Application.Current.Dispatcher.Invoke(() =>
            {
              songPlaylistsViewModel.LoadingStatus.ProcessedCount = x;
            });
          });

          sources = (await itemSourcesProcess.Process)?.ToList();
        }

        if (songsItems.Count > 0)
        {
          var grouppedSongs = songsItems.Select(x => new
          {
            Song = x,
            PlaylistItem = playlistItems.Single(y => y.IdReferencedItem == x.ItemModel.Id)
          });

          var songs = grouppedSongs.OrderBy(x => x.PlaylistItem.OrderInPlaylist)
            .Select(x => viewModelsFactory.Create<SongInPlayListViewModel>(x.Song)).ToList();

          if (sources != null)
          {
            foreach (var song in songs)
            {
              var source =
                sources.SingleOrDefault(x => x.Indentificator == song.SongModel.ItemModel.FileInfo.Indentificator);

              if (source != null)
              {
                song.SongModel.ItemModel.FileInfo.Source = source.Source;
              }
            }
          }
         

          return songs;
        }
        else
        {
          return playlistItems.Select(x => viewModelsFactory.Create<SoundItemInPlaylistViewModel>(x.ReferencedItem));
        }
      }

      return null;
    }

    #endregion

    public override void PublishPlayEvent(IEnumerable<SoundItemInPlaylistViewModel> viewModels, EventAction eventAction)
    {
      var e = new PlayItemsEventData<SoundItemInPlaylistViewModel>(viewModels, eventAction, this);

      eventAggregator.GetEvent<PlayItemsEvent<SoundItem, SoundItemInPlaylistViewModel>>().Publish(e);
    }

    public override void PublishAddToPlaylistEvent(IEnumerable<SoundItemInPlaylistViewModel> viewModels)
    {
      var e = new PlayItemsEventData<SoundItemInPlaylistViewModel>(viewModels, EventAction.Add, this);

      eventAggregator.GetEvent<PlayItemsEvent<SoundItem, SoundItemInPlaylistViewModel>>().Publish(e);
    }


    #region OnPlay

    protected override void OnPlay(EventAction action)
    {
      Task.Run(async () =>
      {
        try
        {
          Application.Current.Dispatcher.Invoke(() =>
          {
            songPlaylistsViewModel.LoadingStatus.IsLoading = true;
          });

          var data = (await GetItemsToPlay()).ToList();

          var e = new PlayItemsEventData<SoundItemInPlaylistViewModel>(data, action, IsShuffle, IsRepeating, Model.LastItemElapsedTime, Model);
          eventAggregator.GetEvent<PlayItemsEvent<SoundItem, SoundItemInPlaylistViewModel>>().Publish(e);
        }
        finally
        {
          Application.Current.Dispatcher.Invoke(() =>
          {
            songPlaylistsViewModel.LoadingStatus.IsLoading = false;
          });
        }
      });
    }

    #endregion


    public override void Dispose()
    {
      base.Dispose();

      serialDisposable?.Dispose();
    }
  }
}