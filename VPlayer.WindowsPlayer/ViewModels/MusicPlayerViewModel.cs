using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Logger;
using Microsoft.EntityFrameworkCore;
using Ninject;
using PCloudClient.Domain;
using Prism.Events;
using VCore;
using VCore.ItemsCollections.VirtualList;
using VCore.ItemsCollections.VirtualList.VirtualLists;
using VCore.Modularity.Events;
using VCore.Standard.Factories.ViewModels;
using VCore.Standard.Helpers;
using VPlayer.AudioStorage.AudioDatabase;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.InfoDownloader;
using VPlayer.AudioStorage.InfoDownloader.Clients.GIfs;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Events;
using VPlayer.Core.Modularity.Regions;
using VPlayer.Core.ViewModels;
using VPlayer.Core.ViewModels.Albums;
using VPlayer.Core.ViewModels.Artists;
using VPlayer.Core.ViewModels.TvShows;
using VPLayer.Domain.Contracts.CloudService.Providers;
using VPlayer.PCloud.ViewModels;
using VPlayer.Player.Views.WindowsPlayer;
using VPlayer.UPnP.ViewModels;
using VPlayer.UPnP.ViewModels.Player;
using VPlayer.UPnP.ViewModels.UPnP;
using VPlayer.WindowsPlayer.Players;
using VPlayer.WindowsPlayer.Views.WindowsPlayer;
using FileInfo = VCore.WPF.ViewModels.WindowsFiles;


namespace VPlayer.WindowsPlayer.ViewModels
{

  public class MusicPlayerViewModel : FilePlayableRegionViewModel<WindowsPlayerView, SoundItemInPlaylistViewModel, SoundItemFilePlaylist, PlaylistSoundItem, SoundItem>
  {
    #region Fields

    private readonly IVPlayerRegionProvider vPlayerRegionProvider;
    private readonly IViewModelsFactory viewModelsFactory;
    private readonly IEventAggregator eventAggregator;
    private readonly AudioInfoDownloader audioInfoDownloader;
    private readonly ICloudService cloudService;
    private readonly VLCPlayer vLcPlayer;
    private Dictionary<SongInPlayListViewModel, bool> playBookInCycle = new Dictionary<SongInPlayListViewModel, bool>();


    #endregion Fields

    #region Constructors

    public MusicPlayerViewModel(
      IVPlayerRegionProvider regionProvider,
      IViewModelsFactory viewModelsFactory,
      IEventAggregator eventAggregator,
      IKernel kernel,
      IStorageManager storageManager,
      AudioInfoDownloader audioInfoDownloader,
      UPnPManagerViewModel uPnPManagerViewModel,
      ICloudService cloudService,
      ILogger logger,
      VLCPlayer vLCPlayer) : base(regionProvider, kernel, logger, storageManager, eventAggregator, vLCPlayer)
    {
      this.vPlayerRegionProvider = regionProvider ?? throw new ArgumentNullException(nameof(regionProvider));
      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));
      this.eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
      this.audioInfoDownloader = audioInfoDownloader ?? throw new ArgumentNullException(nameof(audioInfoDownloader));
      this.cloudService = cloudService ?? throw new ArgumentNullException(nameof(cloudService));
      UPnPManagerViewModel = uPnPManagerViewModel ?? throw new ArgumentNullException(nameof(uPnPManagerViewModel));

      SelectedMediaRendererViewModel = uPnPManagerViewModel.Renderers.View.FirstOrDefault();



      vLcPlayer = vLCPlayer ?? throw new ArgumentNullException(nameof(vLCPlayer));
    }

    #endregion Constructors

    #region Properties

    #region RandomGifUrl

    private string randomGifUrl;

    public string RandomGifUrl
    {
      get { return randomGifUrl; }
      set
      {
        if (value != randomGifUrl)
        {
          randomGifUrl = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region GifTag

    private string gifTag = "random";

    public string GifTag
    {
      get { return gifTag; }
      set
      {
        if (value != gifTag)
        {
          gifTag = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region UseGif

    private bool useGif;
    public bool UseGif
    {
      get { return useGif; }
      set
      {
        if (value != useGif)
        {
          useGif = value;
          RaisePropertyChanged();
        }
      }
    }
    #endregion

    public override bool ContainsNestedRegions => true;
    public override string RegionName { get; protected set; } = RegionNames.WindowsPlayerContentRegion;
    public override string Header => "Music player";
    public int Cycle { get; set; }

    public UPnPManagerViewModel UPnPManagerViewModel { get; }

    #region MediaRendererViewModel

    private MediaRendererViewModel selectedMediaRendererViewModel;

    public MediaRendererViewModel SelectedMediaRendererViewModel
    {
      get { return selectedMediaRendererViewModel; }
      set
      {
        if (value != selectedMediaRendererViewModel)
        {
          selectedMediaRendererViewModel = value;
          playPauseUPnP?.RaiseCanExecuteChanged();
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region IsUPnPPlaying

    private bool isUPnPPlaying;

    public bool IsUPnPPlaying
    {
      get { return isUPnPPlaying; }
      set
      {
        if (value != isUPnPPlaying)
        {
          isUPnPPlaying = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #endregion Properties

    #region Commands

    #region AlbumDetail

    private ActionCommand albumDetail;

    public ICommand AlbumDetail
    {
      get
      {
        if (albumDetail == null)
        {
          albumDetail = new ActionCommand(OnAlbumDetail, CanExecuteAlbumDetail);
        }

        return albumDetail;
      }
    }

    public void OnAlbumDetail()
    {
      if (ActualItem is SongInPlayListViewModel songInPlay)
      {
        vPlayerRegionProvider.ShowAlbumDetail(songInPlay.AlbumViewModel);
      }

    }

    private bool CanExecuteAlbumDetail()
    {
      if (ActualItem is SongInPlayListViewModel songInPlay)
      {
        return songInPlay?.AlbumViewModel != null;
      }

      return false;
    }

    #endregion 

    #region NextGif

    private ActionCommand nextGif;

    public ICommand NextGif
    {
      get
      {
        if (nextGif == null)
        {
          nextGif = new ActionCommand(OnNextGif);
        }

        return nextGif;
      }
    }

    public async void OnNextGif()
    {
      GiphyClient giphyClient = new GiphyClient();

      var gif = await giphyClient.GetRandomGif(GifTag);

      if (gif != null)
      {
        RandomGifUrl = gif.Url.Replace("&", "&amp;");
      }
      else
      {
        logger.Log(MessageType.Error, "GIF IS NULL");
      }
    }

    #endregion

    #region PlayPauseUPnP

    private ActionCommand playPauseUPnP;

    public ICommand PlayPauseUPnP
    {
      get
      {
        if (playPauseUPnP == null)
        {
          playPauseUPnP = new ActionCommand(OnPlayPauseUPnP, CanExecutePlayPauseUPnP);
        }

        return playPauseUPnP;
      }
    }

    private bool CanExecutePlayPauseUPnP()
    {
      return SelectedMediaRendererViewModel != null;
    }

    private bool isUPnp;
    private MediaRendererPlayer mediaRendererPlayer;
    public async void OnPlayPauseUPnP()
    {
      try
      {
        if (!isUPnp)
        {
          BufferingSubject.OnNext(true);

          MediaPlayer.Stop();

          if (mediaRendererPlayer == null)
          {
            mediaRendererPlayer = new MediaRendererPlayer(SelectedMediaRendererViewModel.Model);

            MediaPlayer = mediaRendererPlayer;

            await HookToPlayerEvents();
          }
          else
            MediaPlayer = mediaRendererPlayer;

          await SetMedia(ActualItem.Model);

          if (IsPlaying)
          {
            MediaPlayer.Play();
          }

          isUPnp = true;
        }
        else
        {
          mediaRendererPlayer?.Stop();

          await SetVlcPlayer();
        }
      }
      catch (Exception ex)
      {
        MessageBox.Show(ex.ToString());

        await SetVlcPlayer();
      }
    }

    #endregion

    #endregion

    #region Methods

    #region Initialize

    public override void Initialize()
    {
      base.Initialize();

      IsPlaying = false;

      storageManager.SubscribeToItemChange<Song>(OnSongChange).DisposeWith(this);
      storageManager.SubscribeToItemChange<Album>(OnAlbumChange).DisposeWith(this);

      eventAggregator.GetEvent<ItemUpdatedEvent<AlbumViewModel>>().Subscribe(OnAlbumUpdated).DisposeWith(this);

      eventAggregator.GetEvent<RemoveFromPlaylistEvent<SongInPlayListViewModel>>().Subscribe(RemoveFromPlaystSongs).DisposeWith(this);
      eventAggregator.GetEvent<PlaySongsFromPlayListEvent<SongInPlayListViewModel>>().Subscribe(PlayItemFromPlayList).DisposeWith(this);
      eventAggregator.GetEvent<PlayItemsEvent<SoundItem, SongInPlayListViewModel>>().Subscribe(PlaySongItems).DisposeWith(this);

      PlayList.ItemRemoved.Subscribe(ItemsRemoved).DisposeWith(this);
      PlayList.ItemAdded.Subscribe(ItemsAdded).DisposeWith(this);
    }

    #endregion

    #region PlaySongItems

    private void PlaySongItems(PlayItemsEventData<SongInPlayListViewModel> tvShowEpisodeInPlaylistViewModel)
    {
      var data = new PlayItemsEventData<SoundItemInPlaylistViewModel>(tvShowEpisodeInPlaylistViewModel.Items, tvShowEpisodeInPlaylistViewModel.EventAction, tvShowEpisodeInPlaylistViewModel.Model);

      PlayItemsFromEvent(data);
    }

    #endregion

    #region RemoveFromPlaystSongs

    private void RemoveFromPlaystSongs(RemoveFromPlaylistEventArgs<SongInPlayListViewModel> obj)
    {
      var data = new RemoveFromPlaylistEventArgs<SoundItemInPlaylistViewModel>()
      {
        DeleteType = obj.DeleteType,
        ItemsToRemove = obj.ItemsToRemove.Select(x => (SoundItemInPlaylistViewModel)x).ToList()
      };

      RemoveItemsFromPlaylist(data);
    }

    #endregion

    #region OnActivation

    public override void OnActivation(bool firstActivation)
    {
      base.OnActivation(firstActivation);

      if (firstActivation)
      {
        regionProvider.RegisterView<SongPlayerView, MusicPlayerViewModel>(RegionNames.PlayerContentRegion, this, false, out var guid, RegionManager);
      }
    }

    #endregion

    #region OnActualItemChanged

    protected override void OnActualItemChanged()
    {
      base.OnActualItemChanged();

      Application.Current.Dispatcher.Invoke(() =>
      {
        albumDetail?.RaiseCanExecuteChanged();
      });
    }

    #endregion

    #region OnNewItemPlay

    CancellationTokenSource downloadingLyricsTask;


    public override void OnNewItemPlay()
    {
      base.OnNewItemPlay();

      DownloadLyrics();
    }

    #endregion

    #region DownloadSongInfo

    private Task DownloadSongInfo(SoundItemInPlaylistViewModel viewmodel, CancellationToken cancellationToken)
    {
      if (viewmodel == null)
      {
        return Task.CompletedTask;
      }

      return Task.Run(async () =>
      {
        try
        {
          if (!(viewmodel is SongInPlayListViewModel))
          {
            var fileInfo = viewmodel?.Model?.FileInfo;

            Application.Current.Dispatcher.Invoke(() =>
            {
              viewmodel.IsDownloading = true;
            });

            if (fileInfo != null)
            {
              cancellationToken.ThrowIfCancellationRequested();

              var stats = (await cloudService.GetFileStats(long.Parse(fileInfo.Indentificator)))?.metadata;


              if (stats != null)
              {
                fileInfo.Artist = stats.artist;
                fileInfo.Album = stats.album;

                if (!string.IsNullOrEmpty(stats.title))
                {
                  fileInfo.Name = stats.title;
                }

                viewmodel.Model.Name = fileInfo.Name;

                cancellationToken.ThrowIfCancellationRequested();

                var result = await storageManager.UpdateEntityAsync(fileInfo);

                Artist artist = null;
                Album album = null;

                if (!string.IsNullOrEmpty(stats.artist))
                {
                  var normalizedName = VPlayerStorageManager.GetNormalizedName(stats.artist);

                  cancellationToken.ThrowIfCancellationRequested();

                  artist = storageManager.GetRepository<Artist>().Include(x => x.Albums).FirstOrDefault(x => x.NormalizedName == normalizedName);

                  if (artist == null)
                  {
                    cancellationToken.ThrowIfCancellationRequested();

                    artist = await audioInfoDownloader.UpdateArtist(fileInfo.Artist);
                  }
                }

                if (artist != null)
                {
                  var normalizedName = VPlayerStorageManager.GetNormalizedName(fileInfo.Album);

                  cancellationToken.ThrowIfCancellationRequested();

                  album = artist.Albums?.SingleOrDefault(x => x.NormalizedName == normalizedName);

                  if (album == null)
                  {
                    album = new Album()
                    {
                      Artist = artist,
                      Name = fileInfo.Album
                    };

                    cancellationToken.ThrowIfCancellationRequested();

                    album = await audioInfoDownloader.UpdateAlbum(album);

                    if (album != null)
                    {
                      var id = album.MusicBrainzId;
                      var storedAlbum = artist.Albums?.SingleOrDefault(x => x.MusicBrainzId == id);

                      if (storedAlbum != null)
                      {
                        album = storedAlbum;
                      }
                    }
                  }
                }

                if (artist != null)
                {
                  if (album == null)
                  {
                    album = new Album()
                    {
                      Artist = artist
                    };
                  }

                  var song = new Song()
                  {
                    Album = album,
                    SoundItem = viewmodel.Model,
                    Name = fileInfo.Name
                  };

                  var vm = viewModelsFactory.Create<SongInPlayListViewModel>(song);

                  var index = PlayList.IndexOf(viewmodel);

                  cancellationToken.ThrowIfCancellationRequested();

                  if (index >= 0)
                  {
                    await Application.Current.Dispatcher.InvokeAsync(async () =>
                      {
                        try
                        {
                          PlayList.Remove(viewmodel);
                          PlayList.Insert(index, vm);

                          vm.Initialize();

                          vm.ActualPosition = viewmodel.ActualPosition;
                          vm.IsFavorite = viewmodel.IsFavorite;
                          vm.IsPlaying = viewmodel.IsPlaying;
                          vm.IsSelected = viewmodel.IsSelected;
                          vm.Duration = viewmodel.Duration;

                          if (ActualItem == viewmodel)
                          {
                            ActualItem = vm;
                          }

                          if (vm.AlbumViewModel == null && vm.SongModel.Album != null)
                          {
                            vm.AlbumViewModel = viewModelsFactory.Create<AlbumViewModel>(vm.SongModel.Album);
                          }

                          if (vm.ArtistViewModel == null && vm.SongModel.Album?.Artist != null)
                          {
                            vm.ArtistViewModel = viewModelsFactory.Create<ArtistViewModel>(vm.SongModel.Album.Artist);
                          }

                          ReloadVirtulizedPlaylist();
                          await vm.TryToRefreshUpdateLyrics();
                        }

                        catch (TaskCanceledException)
                        {
                        }
                        catch (Exception ex)
                        {
                          logger.Log(ex);
                        }
                      });
                  }
                }
              }
            }
          }
        }
        catch (TaskCanceledException)
        {
        }


        //var artists = await audioInfoDownloader.UpdateArtist(stats.);
      });
    }

    #endregion

    #region DownloadLyrics

    private Task DownloadLyrics()
    {
      downloadingLyricsTask?.Cancel();
      downloadingLyricsTask?.Dispose();

      downloadingLyricsTask = new CancellationTokenSource();

      var cancellationToken = downloadingLyricsTask.Token;

      return Task.Run(async () =>
      {
        try
        {
          if (ActualItem is SongInPlayListViewModel songInPlay)
          {
            bool wasLyricsNull = songInPlay.LRCLyrics == null;

            if (string.IsNullOrEmpty(songInPlay.LRCLyrics))
            {
              cancellationToken.ThrowIfCancellationRequested();

              await songInPlay.TryToRefreshUpdateLyrics();
            }

            if (songInPlay.LRCLyrics != null && wasLyricsNull)
            {
              cancellationToken.ThrowIfCancellationRequested();

              await storageManager.UpdateEntityAsync(ActualItem.Model);
            }

            var validItemsToUpdate = PlayList.OfType<SongInPlayListViewModel>().ToList();

            var itemsAfter = validItemsToUpdate.Skip(actualItemIndex).Where(x => x.LyricsObject == null);
            var itemsBefore = validItemsToUpdate.Take(actualItemIndex).Where(x => x.LyricsObject == null);

            await DownloadLyrics(itemsAfter, cancellationToken);

            await DownloadLyrics(itemsBefore, cancellationToken);
          }
        }
        catch (OperationCanceledException)
        {
        }

      }, cancellationToken);
    }

    private async Task DownloadLyrics(IEnumerable<SongInPlayListViewModel> songInPlayListViewModels, CancellationToken cancellationToken)
    {
      foreach (var item in songInPlayListViewModels)
      {
        cancellationToken.ThrowIfCancellationRequested();

        var updated = await item.TryToRefreshUpdateLyrics();

        if (updated)
        {
          cancellationToken.ThrowIfCancellationRequested();

          await storageManager.UpdateEntityAsync(item.Model);
        }
      }
    }

    #endregion

    #region OnSetActualItem

    public override void OnSetActualItem(SoundItemInPlaylistViewModel itemViewModel, bool isPlaying)
    {
      if (itemViewModel is SongInPlayListViewModel viewModel)
      {
        if (viewModel.AlbumViewModel != null)
          viewModel.AlbumViewModel.IsPlaying = isPlaying;

        if (viewModel.AlbumViewModel != null)
          viewModel.ArtistViewModel.IsPlaying = isPlaying;
      }

    }

    #endregion

    #region GetNewPlaylistItemViewModel

    protected override PlaylistSoundItem GetNewPlaylistItemViewModel(SoundItemInPlaylistViewModel song, int index)
    {
      var playlistVideoItem = new PlaylistSoundItem();

      if (song.Model.Id != 0)
      {
        playlistVideoItem.IdReferencedItem = song.Model.Id;
      }
      else
      {
        return null;
      }


      playlistVideoItem.OrderInPlaylist = (index + 1);


      return playlistVideoItem;
    }

    #endregion

    #region OnAlbumChange

    private void OnAlbumChange(ItemChanged<Album> change)
    {
      var album = change.Item;

      var songsInPlaylist = PlayList.OfType<SongInPlayListViewModel>().Where(x => x.AlbumViewModel != null && x.AlbumViewModel.ModelId == album.Id);

      foreach (var song in songsInPlaylist)
      {
        song.UpdateAlbumViewModel(album);
      }

    }

    #endregion

    #region OnSongChange

    private void OnSongChange(ItemChanged<Song> itemChanged)
    {
      var song = itemChanged.Item;

      var playlistSong = PlayList.OfType<SongInPlayListViewModel>().SingleOrDefault(x => x.Model.Id == song.Id);

      if (playlistSong != null)
      {
        playlistSong.Update(song);

        if (ActualItem != null && ActualItem.Model.Id == song.Id && ActualItem is SongInPlayListViewModel inPlayListViewModel)
        {
          inPlayListViewModel.Update(song);
        }
      }
    }

    #endregion

    #region OnRemoveItemsFromPlaylist

    protected override void OnRemoveItemsFromPlaylist(DeleteType deleteType, RemoveFromPlaylistEventArgs<SoundItemInPlaylistViewModel> args)
    {
      var albumId = args.ItemsToRemove.OfType<SongInPlayListViewModel>().FirstOrDefault(x => x.AlbumViewModel != null)?.AlbumViewModel.ModelId;

      if (albumId != null)
      {
        var albumSongs = PlayList.OfType<SongInPlayListViewModel>().Where(x => x.SongModel.Album.Id == albumId).ToList();

        foreach (var albumSong in albumSongs)
        {
          PlayList.Remove(albumSong);
        }

        StorePlaylist(editSaved: true);
      }
    }

    #endregion

    #region OnPlayEvent

    private CancellationTokenSource downloadingSongTask;
    protected override void OnPlayEvent(PlayItemsEventData<SoundItemInPlaylistViewModel> data)
    {
      base.OnPlayEvent(data);

      Task.Run(async () =>
      {
        downloadingSongTask?.Cancel();
        downloadingSongTask = new CancellationTokenSource();

        try
        {
          var items = data.Items.ToList();

          foreach (var item in items)
          {
            await DownloadSongInfo(item, downloadingSongTask.Token);
          }

         

         
        }
        catch (Exception ex)
        {
          logger.Log(ex);
        }
      });
    }

    #endregion

    #region ItemsRemoved

    protected override void ItemsRemoved(EventPattern<SoundItemInPlaylistViewModel> eventPattern)
    {
      if (eventPattern.EventArgs is SongInPlayListViewModel songInPlayListViewModel)
      {
        var anyAlbum = PlayList.OfType<SongInPlayListViewModel>().Any(x => x.AlbumViewModel.ModelId == songInPlayListViewModel.AlbumViewModel.ModelId);

        if (!anyAlbum)
        {
          songInPlayListViewModel.AlbumViewModel.IsInPlaylist = false;
        }


        var anyArtist = PlayList.OfType<SongInPlayListViewModel>().Any(x => x.ArtistViewModel.ModelId == songInPlayListViewModel.ArtistViewModel.ModelId);

        if (!anyArtist)
        {
          songInPlayListViewModel.ArtistViewModel.IsInPlaylist = false;
        }

        shuffleList.Remove(eventPattern.EventArgs);
      }

    }

    #endregion

    #region ItemsAdded

    private void ItemsAdded(EventPattern<SoundItemInPlaylistViewModel> eventPattern)
    {

    }

    #endregion

    #region CheckCycle

    private void CheckCycle()
    {
      if (playBookInCycle.Count > 0 && playBookInCycle.All(x => x.Value))
      {
        Cycle++;

        foreach (var item in playBookInCycle)
        {
          playBookInCycle[item.Key] = false;
        }
      }
    }

    #endregion

    #region GetNewPlaylistModel

    protected override SoundItemFilePlaylist GetNewPlaylistModel(List<PlaylistSoundItem> playlistModels, bool isUserCreated)
    {
      var artists = PlayList.OfType<SongInPlayListViewModel>().GroupBy(x => x.ArtistViewModel.Name).ToList();

      string playlistName = null;

      if (artists.Count > 0)
      {
        playlistName = string.Join(", ", artists.Select(x => x.Key).ToArray());
      }
      else
      {
        playlistName = "NEJAKY PLAYLIST";
      }


      var entityPlayList = new SoundItemFilePlaylist()
      {
        IsReapting = IsRepeate,
        IsShuffle = IsShuffle,
        Name = playlistName,
        ItemCount = playlistModels?.Count,
        PlaylistItems = playlistModels,
        LastItemElapsedTime = ActualSavedPlaylist.LastItemElapsedTime,
        LastItemIndex = ActualSavedPlaylist.LastItemIndex,
        IsUserCreated = isUserCreated,
        LastPlayed = DateTime.Now
      };

      return entityPlayList;
    }

    #endregion

    #region FilterByActualSearch

    protected override void FilterByActualSearch(string predictate)
    {
      if (!string.IsNullOrEmpty(predictate))
      {
        var items = PlayList.Where(x => IsInFind(x.Name, predictate)).ToList();

        var other = PlayList.OfType<SongInPlayListViewModel>().Where(x => IsInFind(x.AlbumViewModel.Name, predictate) ||
                                                                          IsInFind(x.ArtistViewModel.Name, predictate));

        var notIn = other.Where(x => !items.Contains(x));

        items.AddRange(notIn);

        var generator = new ItemsGenerator<SoundItemInPlaylistViewModel>(items, 15);

        VirtualizedPlayList = new VirtualList<SoundItemInPlaylistViewModel>(generator);

      }
      else
      {
        ReloadVirtulizedPlaylist();
      }
    }



    #endregion

    #region OnAlbumUpdated

    private void OnAlbumUpdated(ItemUpdatedEventArgs<AlbumViewModel> itemUpdatedEventArgs)
    {
      var songsInAlbum = PlayList.OfType<SongInPlayListViewModel>().Where(x => x.AlbumViewModel.ModelId == itemUpdatedEventArgs.Model.ModelId);

      foreach (var songInAlbum in songsInAlbum)
      {
        if (songInAlbum.AlbumViewModel != itemUpdatedEventArgs.Model)
          songInAlbum.AlbumViewModel = itemUpdatedEventArgs.Model;
        else
          songInAlbum.UpdateAlbumViewModel(itemUpdatedEventArgs.Model.Model);
      }
    }

    #endregion

    #region SetVlcPlayer

    private async Task SetVlcPlayer()
    {
      MediaPlayer = vLcPlayer;

      await SetMedia(ActualItem.Model);

      if (IsPlaying)
      {
        MediaPlayer.Play();
      }

      isUPnp = false;
    }

    #endregion

    #region OnDeactived

    public override void OnDeactived()
    {
      base.OnDeactived();

      downloadingLyricsTask?.Cancel();
      downloadingLyricsTask?.Dispose();
      downloadingLyricsTask = null;

      downloadingSongTask?.Cancel();
      downloadingSongTask?.Dispose();
      downloadingSongTask = null;
    }

    #endregion

    #region Dispose

    public override void Dispose()
    {
      base.Dispose();

      downloadingLyricsTask?.Cancel();
      downloadingLyricsTask?.Dispose();

      downloadingSongTask?.Cancel();
      downloadingSongTask?.Dispose();
    }

    #endregion

    #endregion
  }
}