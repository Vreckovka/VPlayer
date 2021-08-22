using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Logger;
using Ninject;
using Prism.Events;
using VCore;
using VCore.ItemsCollections.VirtualList;
using VCore.ItemsCollections.VirtualList.VirtualLists;
using VCore.Modularity.Events;
using VCore.Standard.Helpers;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.InfoDownloader;
using VPlayer.AudioStorage.InfoDownloader.Clients.GIfs;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Events;
using VPlayer.Core.Modularity.Regions;
using VPlayer.Core.ViewModels;
using VPlayer.Core.ViewModels.Albums;
using VPlayer.Core.ViewModels.TvShows;
using VPlayer.Player.Views.WindowsPlayer;
using VPlayer.UPnP.ViewModels;
using VPlayer.UPnP.ViewModels.Player;
using VPlayer.UPnP.ViewModels.UPnP;
using VPlayer.WindowsPlayer.Players;
using VPlayer.WindowsPlayer.Views.WindowsPlayer;


namespace VPlayer.WindowsPlayer.ViewModels
{

  public class MusicPlayerViewModel : FilePlayableRegionViewModel<WindowsPlayerView, SoundItemInPlaylistViewModel, SoundItemFilePlaylist, PlaylistSoundItem, SoundItem>
  {
    #region Fields

    private readonly IVPlayerRegionProvider vPlayerRegionProvider;
    private readonly IEventAggregator eventAggregator;
    private readonly AudioInfoDownloader audioInfoDownloader;
    private readonly VLCPlayer vLcPlayer;
    private Dictionary<SongInPlayListViewModel, bool> playBookInCycle = new Dictionary<SongInPlayListViewModel, bool>();


    #endregion Fields

    #region Constructors

    public MusicPlayerViewModel(
      IVPlayerRegionProvider regionProvider,
      IEventAggregator eventAggregator,
      IKernel kernel,
      IStorageManager storageManager,
      AudioInfoDownloader audioInfoDownloader,
      UPnPManagerViewModel uPnPManagerViewModel,
      ILogger logger,
      VLCPlayer vLCPlayer) : base(regionProvider, kernel, logger, storageManager, eventAggregator, vLCPlayer)
    {
      this.vPlayerRegionProvider = regionProvider ?? throw new ArgumentNullException(nameof(regionProvider));
      this.eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
      this.audioInfoDownloader = audioInfoDownloader ?? throw new ArgumentNullException(nameof(audioInfoDownloader));
      UPnPManagerViewModel = uPnPManagerViewModel ?? throw new ArgumentNullException(nameof(uPnPManagerViewModel));

      SelectedMediaRendererViewModel = UPnPManagerViewModel.Renderers.View.FirstOrDefault();



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
      if(ActualItem is SongInPlayListViewModel songInPlay)
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
      eventAggregator.GetEvent<PlayItemsEvent<SoundItem, SongInPlayListViewModel>>().Subscribe(PlayTvShowItems).DisposeWith(this);

      PlayList.ItemRemoved.Subscribe(ItemsRemoved).DisposeWith(this);
      PlayList.ItemAdded.Subscribe(ItemsAdded).DisposeWith(this);
    }

    #endregion

    #region PlayTvShowItems

    private void PlayTvShowItems(PlayItemsEventData<SongInPlayListViewModel> tvShowEpisodeInPlaylistViewModel)
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

      albumDetail?.RaiseCanExecuteChanged();
    }

    #endregion

    #region OnNewItemPlay

    CancellationTokenSource downloadingLyricsTask;
    public override void OnNewItemPlay()
    {
      base.OnNewItemPlay();

      downloadingLyricsTask?.Cancel();
      downloadingLyricsTask?.Dispose();

      downloadingLyricsTask = new CancellationTokenSource();

      var cancellationToken = downloadingLyricsTask.Token;

      Task.Run(async () =>
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

    #endregion

    #region DownloadLyrics

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
      if(itemViewModel  is SongInPlayListViewModel viewModel)
      {
        viewModel.AlbumViewModel.IsPlaying = isPlaying;
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

    #region ItemsRemoved

    protected override void ItemsRemoved(EventPattern<SoundItemInPlaylistViewModel> eventPattern)
    {
      if(eventPattern.EventArgs is SongInPlayListViewModel songInPlayListViewModel)
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
    }

    #endregion

    #region Dispose

    public override void Dispose()
    {
      base.Dispose();

      downloadingLyricsTask?.Cancel();
      downloadingLyricsTask?.Dispose();
    }

    #endregion

    #endregion
  }
}