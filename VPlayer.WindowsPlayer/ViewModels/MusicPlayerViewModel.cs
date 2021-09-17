using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
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
using VCore.WPF.Managers;
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
using VPLayer.Domain;
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
    private readonly IVPlayerCloudService cloudService;
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
      IVPlayerCloudService cloudService,
      ILogger logger,
      IWindowManager windowManager,
      VLCPlayer vLCPlayer) : base(regionProvider, kernel, logger, storageManager, eventAggregator, windowManager, vLCPlayer)
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

      storageManager.ObserveOnItemChange<Song>().ObserveOn(Application.Current.Dispatcher).Subscribe(OnSongChange).DisposeWith(this);
      storageManager.ObserveOnItemChange<Album>().ObserveOn(Application.Current.Dispatcher).Subscribe(OnAlbumChange).DisposeWith(this);
      storageManager.ObserveOnItemChange<SoundItem>().ObserveOn(Application.Current.Dispatcher).Subscribe(OnSoundItemUpdated).DisposeWith(this);



      eventAggregator.GetEvent<ItemUpdatedEvent<AlbumViewModel>>().Subscribe(OnAlbumUpdated).DisposeWith(this);
      eventAggregator.GetEvent<RemoveFromPlaylistEvent<SongInPlayListViewModel>>().Subscribe(RemoveFromPlaystSongs).DisposeWith(this);
      eventAggregator.GetEvent<PlaySongsFromPlayListEvent<SongInPlayListViewModel>>().Subscribe(PlayItemFromPlayList).DisposeWith(this);
      eventAggregator.GetEvent<PlayItemsEvent<SoundItem, SongInPlayListViewModel>>().Subscribe(PlaySongItems).DisposeWith(this);

      PlayList.ItemRemoved.Subscribe(ItemsRemoved).DisposeWith(this);
      PlayList.ItemAdded.Subscribe(ItemsAdded).DisposeWith(this);
    }

    #endregion

    private async void OnSoundItemUpdated(IItemChanged<SoundItem> soundItemChanged)
    {
      if (soundItemChanged.Changed == Changed.Updated)
      {
        var inPlaylist = PlayList.SingleOrDefault(x => x.Model.Id == soundItemChanged.Item.Id);

        if (inPlaylist != null)
        {
          var lastIsAutomaticLyricsFindEnabled = inPlaylist.Model.IsAutomaticLyricsFindEnabled;

          inPlaylist.Model.Update(soundItemChanged.Item);

          inPlaylist.RaiseNotifyPropertyChanged(nameof(SoundItemInPlaylistViewModel.Model));

          if (inPlaylist is SongInPlayListViewModel song && lastIsAutomaticLyricsFindEnabled != song.Model.IsAutomaticLyricsFindEnabled)
          {
            song.RaiseNotifyPropertyChanged(nameof(SongInPlayListViewModel.IsAutomaticLyricsDownloadDisabled));

            await song.TryToRefreshUpdateLyrics();
          }
        }
      }
    }

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

    private Artist downloadingArtist;
    private Album downloadingAlbum;

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
          if (viewmodel is SongInPlayListViewModel songInPlayListViewModel &&
              songInPlayListViewModel.AlbumViewModel == null &&
              songInPlayListViewModel.ArtistViewModel == null)
          {
            var fileInfo = viewmodel?.Model?.FileInfo;

            Application.Current.Dispatcher.Invoke(() => { viewmodel.IsDownloading = true; });

            if (fileInfo != null)
            {
              cancellationToken.ThrowIfCancellationRequested();

              string artistName = viewmodel?.Model?.FileInfo.Artist;
              string albumName = viewmodel?.Model?.FileInfo.Album;


              if (artistName == null || albumName == null)
              {
                if (long.TryParse(fileInfo.Indentificator, out var id))
                {
                  var stats = (await cloudService.GetFileStats(id));

                  var data = stats?.metadata;

                  if (data != null)
                  {
                    fileInfo.Artist = AudioInfoDownloader.GetClearName(data.artist);
                    fileInfo.Album = AudioInfoDownloader.GetClearName(data.album);

                    artistName = fileInfo.Artist;
                    albumName = fileInfo.Album;


                    if (!string.IsNullOrEmpty(data.title))
                    {
                      fileInfo.Name = AudioInfoDownloader.GetClearName(data.title);
                    }
                  }
                }
                else
                {
                  var info = await Task.Run(() => audioInfoDownloader.GetAudioInfoByWindowsAsync(fileInfo.Source));

                  artistName = AudioInfoDownloader.GetClearName(info?.Artist);
                  albumName = AudioInfoDownloader.GetClearName(info?.Album);

                  if (!string.IsNullOrEmpty(info?.Title))
                  {
                    fileInfo.Name = AudioInfoDownloader.GetClearName(info.Title);
                  }
                }
              }

              viewmodel.Model.FileInfo.Name = fileInfo.Name;

              cancellationToken.ThrowIfCancellationRequested();

              var result = await storageManager.UpdateEntityAsync(fileInfo);

              if (!string.IsNullOrEmpty(artistName))
              {
                if (downloadingArtist == null || VPlayerStorageManager.GetNormalizedName(artistName)?.Similarity(VPlayerStorageManager.GetNormalizedName(downloadingArtist.Name), true) < 0.9)
                  downloadingArtist = await GetArist(artistName, cancellationToken);
              }
              else
              {
                downloadingArtist = null;
              }


              if (!string.IsNullOrEmpty(albumName))
              {
                var normalizedAlbumName = AudioInfoDownloader.GetClearName(VPlayerStorageManager.GetNormalizedName(albumName));

                if (downloadingAlbum == null || normalizedAlbumName?.Similarity(VPlayerStorageManager.GetNormalizedName(downloadingAlbum.Name), true, true) < 0.9)
                  downloadingAlbum = await GetAlbum(downloadingArtist, albumName, cancellationToken);
              }
              else
              {
                downloadingAlbum = null;
              }

              if (downloadingArtist != null)
              {
                if (downloadingAlbum == null)
                {
                  downloadingAlbum = new Album()
                  {
                    Artist = downloadingArtist,
                    Name = albumName
                  };
                }
              }
              else
              {
                downloadingArtist = new Artist()
                {
                  Name = artistName
                };

                if (downloadingAlbum == null)
                {
                  downloadingAlbum = new Album()
                  {
                    Artist = downloadingArtist,
                    Name = albumName
                  };
                }
              }

              Application.Current.Dispatcher.InvokeAsync(async () =>
              {
                try
                {
                  songInPlayListViewModel.SongModel.Album = downloadingAlbum;

                  songInPlayListViewModel.Initialize();


                  if (songInPlayListViewModel.AlbumViewModel == null && songInPlayListViewModel.SongModel.Album != null)
                  {
                    songInPlayListViewModel.AlbumViewModel = viewModelsFactory.Create<AlbumViewModel>(songInPlayListViewModel.SongModel.Album);
                    songInPlayListViewModel.AlbumViewModel.Model = downloadingAlbum;
                  }

                  if (songInPlayListViewModel.ArtistViewModel == null && songInPlayListViewModel.SongModel.Album?.Artist != null)
                  {
                    songInPlayListViewModel.ArtistViewModel = viewModelsFactory.Create<ArtistViewModel>(songInPlayListViewModel.SongModel.Album.Artist);
                    songInPlayListViewModel.ArtistViewModel.Model = downloadingArtist;
                  }

                  cancellationToken.ThrowIfCancellationRequested();

                  Task.Run(async () =>
                  {
                    var resultLyrics = await songInPlayListViewModel.TryToRefreshUpdateLyrics();

                    if (resultLyrics && viewmodel.Model != null)
                    {
                      await storageManager.UpdateEntityAsync(viewmodel.Model);
                    }
                  });

                  songInPlayListViewModel.SongModel.Album.Songs.Add(songInPlayListViewModel.SongModel);

                  //ActualItem.ImagePath
                  songInPlayListViewModel.RaiseNotifyPropertyChanged(nameof(SoundItemFilePlaylist.Name));
                  songInPlayListViewModel.RaiseNotifyPropertyChanged(nameof(SongInPlayListViewModel.AlbumViewModel));
                  songInPlayListViewModel.RaiseNotifyPropertyChanged(nameof(SongInPlayListViewModel.ArtistViewModel));
                  albumDetail?.RaiseCanExecuteChanged();

                  if (songInPlayListViewModel == ActualItem)
                  {
                    RaisePropertyChanged(nameof(ActualItem));
                    songInPlayListViewModel.RaiseNotifyPropertyChanged(nameof(SongInPlayListViewModel.ImagePath));
                  }
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
        catch (TaskCanceledException)
        {
        }
        catch (Exception ex)
        {
          logger.Log(ex);
        }
        finally
        {
          Application.Current.Dispatcher.Invoke(() =>
          {
            viewmodel.IsDownloading = false;
          });
        }
      });
    }

    #endregion

    #region GetArist

    private async Task<Artist> GetArist(string artistName, CancellationToken cancellationToken)
    {
      Artist artist = null;

      if (!string.IsNullOrEmpty(artistName))
      {
        var normalizedName = VPlayerStorageManager.GetNormalizedName(artistName);

        cancellationToken.ThrowIfCancellationRequested();

        artist = storageManager.GetRepository<Artist>().Include(x => x.Albums).FirstOrDefault(x => x.NormalizedName == normalizedName);

        if (artist == null)
        {
          cancellationToken.ThrowIfCancellationRequested();

          artist = await audioInfoDownloader.UpdateArtist(artistName);
        }
      }

      return artist;
    }

    #endregion

    #region GetAlbum

    private async Task<Album> GetAlbum(Artist artist, string albumName, CancellationToken cancellationToken)
    {
      Album album = null;

      if (artist != null)
      {
        {
          var normalizedName = VPlayerStorageManager.GetNormalizedName(albumName);

          cancellationToken.ThrowIfCancellationRequested();

          album = artist.Albums?.SingleOrDefault(x => x.NormalizedName == normalizedName);

          if (album == null)
          {
            album = new Album()
            {
              Artist = artist,
              Name = albumName
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
      }

      return album;
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

            var validItemsToUpdate = PlayList.OfType<SongInPlayListViewModel>()
              .Where(x => x.ArtistViewModel != null && x.AlbumViewModel != null).ToList();

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

        if (viewModel.ArtistViewModel != null)
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

    private void OnAlbumChange(IItemChanged<Album> change)
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

    private void OnSongChange(IItemChanged<Song> itemChanged)
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
      if (deleteType == DeleteType.AlbumFromPlaylist)
      {
        var album = args.ItemsToRemove.OfType<SongInPlayListViewModel>().FirstOrDefault(x => x.AlbumViewModel != null)?.AlbumViewModel;

        if (album != null && !string.IsNullOrEmpty(album.Name))
        {
          var albumSongs = PlayList.OfType<SongInPlayListViewModel>().Where(x => x.SongModel?.Album == album.Model).ToList();

          foreach (var albumSong in albumSongs)
          {
            PlayList.Remove(albumSong);
          }
        }
        else
        {
          foreach (var item in args.ItemsToRemove)
          {
            PlayList.Remove(item);
          }
        }
      }
      else
      {
        foreach (var item in args.ItemsToRemove)
        {
          PlayList.Remove(item);
        }
      }


    }

    #endregion

    #region BeforePlayEvent

    protected override async Task BeforePlayEvent(PlayItemsEventData<SoundItemInPlaylistViewModel> data)
    {
      var songs = data.Items.OfType<SongInPlayListViewModel>().ToList();

      var soundItems = data.Items.Where(x => !(x is SongInPlayListViewModel)).ToList();

      var newList = new List<SongInPlayListViewModel>();

      if (songs.Count > 0)
      {
        foreach (var item in data.Items)
        {
          var songItem = songs.SingleOrDefault(x => x.Model.Id == item.Model.Id);
          var soundItem = soundItems.SingleOrDefault(x => x.Model.Id == item.Model.Id);

          if (songItem != null)
          {
            newList.Add(songItem);
          }
          else if (soundItem != null)
          {
            newList.Add(CreateSongViewModel(soundItem));
          }
        }
      }
      else
      {
        newList.AddRange(soundItems.Select(x => CreateSongViewModel(x)));
      }

      data.Items = newList;

      Application.Current?.Dispatcher?.Invoke(() =>
      {
        if (PlayList.Count > 0)
        {
          var allArtists = PlayList.OfType<SongInPlayListViewModel>().Where(x => x.ArtistViewModel != null).GroupBy(x => x.ArtistViewModel);

          foreach (var artist in allArtists.Select(x => x.Key))
          {
            artist.IsInPlaylist = false;
          }

          var allAlbums = PlayList.OfType<SongInPlayListViewModel>().Where(x => x.AlbumViewModel != null).GroupBy(x => x.AlbumViewModel);

          foreach (var album in allAlbums.Select(x => x.Key))
          {
            album.IsInPlaylist = false;
          }
        }
      });


    }

    #endregion


    protected override void OnPlayPlaylist(PlayItemsEventData<SoundItemInPlaylistViewModel> data)
    {
      base.OnPlayPlaylist(data);

      if (ActualSavedPlaylist.PlaylistType != PlaylistType.Cloud && data.Items.Select(x => x.Model.Source).Any(y => y.Contains("http")))
      {
        ActualSavedPlaylist.PlaylistType = PlaylistType.Cloud;
      }
    }

    #region OnPlayEvent

    private CancellationTokenSource downloadingSongTask;

    protected override void OnPlayEvent(PlayItemsEventData<SoundItemInPlaylistViewModel> data)
    {
      base.OnPlayEvent(data);

      Application.Current?.Dispatcher?.Invoke(() =>
      {
        if (PlayList.Count > 0)
        {
          var allArtists = PlayList.OfType<SongInPlayListViewModel>().Where(x => x.ArtistViewModel != null).GroupBy(x => x.ArtistViewModel);

          foreach (var artist in allArtists.Select(x => x.Key))
          {
            artist.IsInPlaylist = true;
          }

          var allAlbums = PlayList.OfType<SongInPlayListViewModel>().Where(x => x.AlbumViewModel != null).GroupBy(x => x.AlbumViewModel);

          foreach (var album in allAlbums.Select(x => x.Key))
          {
            album.IsInPlaylist = true;
          }
        }
      });

      DownloadSongInfos(PlayList);
    }

    #endregion

    #region CreateSongViewModel

    private SongInPlayListViewModel CreateSongViewModel(SoundItemInPlaylistViewModel viewmodel)
    {
      var song = new Song()
      {
        ItemModel = viewmodel.Model,
      };

      var vm = viewModelsFactory.Create<SongInPlayListViewModel>(song);

      vm.ActualPosition = viewmodel.ActualPosition;
      vm.IsFavorite = viewmodel.IsFavorite;
      vm.IsPlaying = viewmodel.IsPlaying;
      vm.IsSelected = viewmodel.IsSelected;
      vm.Duration = viewmodel.Duration;

      return vm;

    }

    #endregion

    #region DownloadSongInfos

    private Task DownloadSongInfos(IEnumerable<SoundItemInPlaylistViewModel> soundItemInPlaylistViewModel)
    {
      return Task.Run(async () =>
      {
        downloadingSongTask?.Cancel();
        downloadingSongTask = new CancellationTokenSource();
        downloadingArtist = null;
        downloadingAlbum = null;

        try
        {
          var items = soundItemInPlaylistViewModel.ToList();

          foreach (var item in items)
          {
            if (downloadingSongTask == null)
            {
              return;
            }

            downloadingSongTask.Token.ThrowIfCancellationRequested();

            await DownloadSongInfo(item, downloadingSongTask.Token);
          }

          TrySetNewPlaylistName();
        }
        catch (OperationCanceledException)
        {
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
        var anyAlbum = PlayList.OfType<SongInPlayListViewModel>()
          .Where(x => x.AlbumViewModel != null)
          .Any(x => x.AlbumViewModel.ModelId == songInPlayListViewModel.AlbumViewModel.ModelId);

        if (!anyAlbum && songInPlayListViewModel.AlbumViewModel != null)
        {
          songInPlayListViewModel.AlbumViewModel.IsInPlaylist = false;
        }

        var anyArtist = PlayList.OfType<SongInPlayListViewModel>()
          .Where(x => x.ArtistViewModel != null)
          .Any(x => x.ArtistViewModel.ModelId == songInPlayListViewModel.ArtistViewModel.ModelId);

        if (!anyArtist && songInPlayListViewModel.ArtistViewModel != null)
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

    #region TrySetNewPlaylistName

    private void TrySetNewPlaylistName()
    {
      if (ActualSavedPlaylist != null && string.IsNullOrEmpty(ActualSavedPlaylist.Name))
      {
        ActualSavedPlaylist.Name = GetNewPlaylistName();

        Application.Current?.Dispatcher?.Invoke(() =>
        {
          OnSavePlaylist();
        });
      }
    }

    #endregion

    #region GetNewPlaylistName

    private string GetNewPlaylistName()
    {
      var actaulPlaylist = PlayList?.ToList();

      if (actaulPlaylist == null)
      {
        return null;
      }

      var artists = actaulPlaylist.OfType<SongInPlayListViewModel>()
        .Where(x => !string.IsNullOrEmpty(x?.ArtistViewModel?.Name))
        .GroupBy(x => x.ArtistViewModel.Name).ToList();

      string playlistName = "";

      if (artists.Count > 0)
      {
        playlistName = string.Join(", ", artists.Select(x => x.Key).ToArray());

        var albums = actaulPlaylist.OfType<SongInPlayListViewModel>()
          .Where(x => !string.IsNullOrEmpty(x?.AlbumViewModel?.Name))
          .GroupBy(x => x.AlbumViewModel.Name).ToList();

        if (albums.Count == 1)
        {
          playlistName += $" - {albums[0].Key}";
        }
      }
      else
      {
        var splits = actaulPlaylist.Where(x => x?.Model?.Source != null)
          .Select(x => x.Model.Source.Split("\\")).ToList();

        if (splits.Count > 0)
        {
          for (int i = 0; i < splits.Max(x => x.Length); i++)
          {
            var source = splits.Where(x => x.Length > i).GroupBy(x => x[i]).ToList();

            var keys = source.Select(x => x.Key).Distinct().ToList();
            var separator = "\\";

            if (keys.Count == 1)
            {
              if (i > 0)
              {
                playlistName += separator;
              }

              playlistName += keys[0];
            }
            else if (i == 0)
            {
              playlistName = null;
            }
          }
        }
        else
        {
          playlistName = null;
        }
      }

      return playlistName;
    }

    #endregion

    #region BeforeClearPlaylist

    protected override void BeforeClearPlaylist()
    {
      base.BeforeClearPlaylist();

      PlayList.OfType<SongInPlayListViewModel>().Where(x => x.AlbumViewModel != null).Select(x => x.AlbumViewModel).Distinct()
         .ForEach(x =>
         {
           x.IsPlaying = false;
           x.IsInPlaylist = false;
         });

      PlayList.OfType<SongInPlayListViewModel>().Where(x => x.ArtistViewModel != null).Select(x => x.ArtistViewModel).Distinct()
        .ForEach(x =>
        {
          x.IsPlaying = false;
          x.IsInPlaylist = false;
        });
    }

    #endregion

    #region GetNewPlaylistModel

    protected override SoundItemFilePlaylist GetNewPlaylistModel(List<PlaylistSoundItem> playlistModels, bool isUserCreated)
    {
      var playlistName = GetNewPlaylistName();

      var entityPlayList = new SoundItemFilePlaylist()
      {
        IsReapting = IsRepeate,
        IsShuffle = IsShuffle,
        Name = string.IsNullOrEmpty(playlistName) ? "" : playlistName,
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
        RequestReloadVirtulizedPlaylist();
      }
    }



    #endregion

    #region OnAlbumUpdated

    private void OnAlbumUpdated(ItemUpdatedEventArgs<AlbumViewModel> itemUpdatedEventArgs)
    {
      if (itemUpdatedEventArgs.Model != null)
      {
        var songsInAlbum = PlayList.OfType<SongInPlayListViewModel>().Where(x => x.AlbumViewModel != null)
          .Where(x => x.AlbumViewModel.ModelId == itemUpdatedEventArgs.Model.ModelId);

        foreach (var songInAlbum in songsInAlbum)
        {
          if (songInAlbum.AlbumViewModel != itemUpdatedEventArgs.Model)
            songInAlbum.AlbumViewModel = itemUpdatedEventArgs.Model;
          else
            songInAlbum.UpdateAlbumViewModel(itemUpdatedEventArgs.Model.Model);
        }
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

      //downloadingLyricsTask?.Cancel();
      //downloadingLyricsTask?.Dispose();
      //downloadingLyricsTask = null;

      //downloadingSongTask?.Cancel();
      //downloadingSongTask?.Dispose();
      //downloadingSongTask = null;
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