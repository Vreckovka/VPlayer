using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Logger;
using Ninject;
using Prism.Events;
using VCore;
using VCore.Helpers;
using VCore.ItemsCollections;
using VCore.ItemsCollections.VirtualList;
using VCore.ItemsCollections.VirtualList.VirtualLists;
using VCore.Modularity.Events;
using VCore.Standard.Helpers;
using VCore.ViewModels.Navigation;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.InfoDownloader;
using VPlayer.AudioStorage.InfoDownloader.Clients.GIfs;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Events;
using VPlayer.Core.Interfaces.ViewModels;
using VPlayer.Core.Modularity.Regions;
using VPlayer.Core.ViewModels;
using VPlayer.Core.ViewModels.Albums;
using VPlayer.Core.ViewModels.TvShows;
using VPlayer.Player.Views.WindowsPlayer;
using VPlayer.WindowsPlayer.Providers;
using VPlayer.WindowsPlayer.Views.WindowsPlayer;

//TODO: Cykli ked prejdes cely play list tak ze si ho cely vypocujes (meni sa farba podla cyklu)
//TODO: Hash playlistov, ked zavries appku tak ti vyhodi posledny playlist
//TODO: Nacitanie zo suboru
//TODO: Ak je neidentifkovana skladba, pridanie interpreta zo zoznamu, alebo vytvorit noveho
//TODO: Nastavit si hlavnu zlozku a ked spustis z inej, moznost presunut
//TODO: Playlist hore pri menu, quick ze prides a uvidis napriklad 5 poslednych hore v rade , ako carusel (5/5)
//TODO: Playlist nech sa automaticky nevytvara ak je niekolko pesniciek (nastavenie pre uzivatela aky pocet sa ma ukladat!) (3/5)
//TODO: Ulozit playlist s casom a nastaveniami (opakovanie, shuffle)
//TODO: 2 Druhy playlistov, uzivatelsky(upravitelny) a generovany (readonly)
//TODO: Hore prenutie medzi windows a browser playermi , zmizne bocne menu
//TODO: Pridat loading indikator, mozno aj co prave robi
//TODO: Hviezdicky pocet 

namespace VPlayer.WindowsPlayer.ViewModels
{

  public class MusicPlayerViewModel : PlayableRegionViewModel<WindowsPlayerView, SongInPlayListViewModel, SongsPlaylist, PlaylistSong, Song>
  {
    #region Fields

    private readonly IVPlayerRegionProvider vPlayerRegionProvider;
    private readonly IEventAggregator eventAggregator;
    private readonly AudioInfoDownloader audioInfoDownloader;
    private int actualSongIndex = 0;
    private Dictionary<SongInPlayListViewModel, bool> playBookInCycle = new Dictionary<SongInPlayListViewModel, bool>();

    #endregion Fields

    #region Constructors

    public MusicPlayerViewModel(
      IVPlayerRegionProvider regionProvider,
      IEventAggregator eventAggregator,
      IKernel kernel,
      IStorageManager storageManager,
      AudioInfoDownloader audioInfoDownloader,
      ILogger logger,
      IVlcProvider vlcProvider) : base(regionProvider, kernel, logger, storageManager, eventAggregator, vlcProvider)
    {
      this.vPlayerRegionProvider = regionProvider ?? throw new ArgumentNullException(nameof(regionProvider));
      this.eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
      this.audioInfoDownloader = audioInfoDownloader ?? throw new ArgumentNullException(nameof(audioInfoDownloader));
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
    public override string Header => "Music Player";
    public int Cycle { get; set; }

    #endregion Properties

    #region Commands

    #region NextSong

    private ActionCommand<SongInPlayListViewModel> nextSong;

    public ICommand NextSong
    {
      get
      {
        if (nextSong == null)
        {
          nextSong = new ActionCommand<SongInPlayListViewModel>(OnNextSong);
        }

        return nextSong;
      }
    }

    public void OnNextSong(SongInPlayListViewModel songInPlayListViewModel)
    {
      PlayNextWithItem(songInPlayListViewModel);
    }

    #endregion 

    #region AlbumDetail

    private ActionCommand albumDetail;

    public ICommand AlbumDetail
    {
      get
      {
        if (albumDetail == null)
        {
          albumDetail = new ActionCommand(OnAlbumDetail);
        }

        return albumDetail;
      }
    }

    public void OnAlbumDetail()
    {
      vPlayerRegionProvider.ShowAlbumDetail(ActualItem.AlbumViewModel);
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
      

      PlayList.ItemRemoved.Subscribe(ItemsRemoved).DisposeWith(this);
      PlayList.ItemAdded.Subscribe(ItemsAdded).DisposeWith(this);
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

    #region OnNewItemPlay

    public override void OnNewItemPlay()
    {
      Task.Run(async () =>
      {
        if (string.IsNullOrEmpty(ActualItem.Lyrics) && !string.IsNullOrEmpty(ActualItem.ArtistViewModel?.Name))
        {
          await audioInfoDownloader.UpdateSongLyricsAsync(ActualItem.ArtistViewModel.Name, ActualItem.Name, ActualItem.Model);
        }

        if (string.IsNullOrEmpty(ActualItem.LRCLyrics))
        {
          await ActualItem.TryToRefreshUpdateLyrics();
        }

      });
    }

    #endregion

    #region OnSetActualItem

    public override void OnSetActualItem(SongInPlayListViewModel itemViewModel, bool isPlaying)
    {
      itemViewModel.AlbumViewModel.IsPlaying = isPlaying;
      itemViewModel.ArtistViewModel.IsPlaying = isPlaying;
    }

    #endregion

    #region GetNewPlaylistItemViewModel

    protected override PlaylistSong GetNewPlaylistItemViewModel(SongInPlayListViewModel song, int index)
    {
      return new PlaylistSong()
      {
        IdSong = song.Model.Id,
        OrderInPlaylist = (index + 1)
      };
    }

    #endregion

    #region OnAlbumChange

    private void OnAlbumChange(ItemChanged<Album> change)
    {
      var album = change.Item;

      var songsInPlaylist = PlayList.Where(x => x.AlbumViewModel != null && x.AlbumViewModel.ModelId == album.Id);

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

      var playlistSong = PlayList.SingleOrDefault(x => x.Model.Id == song.Id);

      if (playlistSong != null)
      {
        playlistSong.Update(song);

        if (ActualItem != null && ActualItem.Model.Id == song.Id)
        {
          ActualItem.Update(song);
        }
      }
    }

    #endregion

    #region OnRemoveItemsFromPlaylist

    protected override void OnRemoveItemsFromPlaylist(DeleteType deleteType, RemoveFromPlaylistEventArgs<SongInPlayListViewModel> args)
    {
      var albumId = args.ItemsToRemove.FirstOrDefault(x => x.AlbumViewModel != null)?.AlbumViewModel.ModelId;

      if (albumId != null)
      {
        var albumSongs = PlayList.Where(x => x.Model.Album.Id == albumId).ToList();

        foreach (var albumSong in albumSongs)
        {
          PlayList.Remove(albumSong);
        }

        StorePlaylist(editSaved: true);
      }
    }

    #endregion

    #region ItemsRemoved

    protected override void ItemsRemoved(EventPattern<SongInPlayListViewModel> eventPattern)
    {
      var anyAlbum = PlayList.Any(x => x.AlbumViewModel.ModelId == eventPattern.EventArgs.AlbumViewModel.ModelId);

      if (!anyAlbum)
      {
        eventPattern.EventArgs.AlbumViewModel.IsInPlaylist = false;
      }


      var anyArtist = PlayList.Any(x => x.ArtistViewModel.ModelId == eventPattern.EventArgs.ArtistViewModel.ModelId);

      if (!anyArtist)
      {
        eventPattern.EventArgs.ArtistViewModel.IsInPlaylist = false;
      }

      shuffleList.Remove(eventPattern.EventArgs);

    }

    #endregion

    #region ItemsAdded

    private void ItemsAdded(EventPattern<SongInPlayListViewModel> eventPattern)
    {

    }

    #endregion

    #region OnPlayEvent

    protected override void OnPlayEvent()
    {
      base.OnPlayEvent();


      Task.Run(async () =>
      {
        foreach (var song in PlayList)
        {
          song.LoadLRCFromEnitityLyrics();
        }
      });
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

    protected override SongsPlaylist GetNewPlaylistModel(List<PlaylistSong> playlistModels, bool isUserCreated)
    {
      var artists = PlayList.GroupBy(x => x.ArtistViewModel.Name);

      var playlistName = string.Join(", ", artists.Select(x => x.Key).ToArray());

      var entityPlayList = new SongsPlaylist()
      {
        IsReapting = IsRepeate,
        IsShuffle = IsShuffle,
        Name = playlistName,
        ItemCount = playlistModels.Count,
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
        var items = PlayList.Where(x =>
          IsInFind(x.Name, predictate) ||
          IsInFind(x.AlbumViewModel.Name, predictate) ||
          IsInFind(x.ArtistViewModel.Name, predictate));

        var generator = new ItemsGenerator<SongInPlayListViewModel>(items, 15);

        VirtualizedPlayList = new VirtualList<SongInPlayListViewModel>(generator);

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
      var songsInAlbum = PlayList.Where(x => x.AlbumViewModel.ModelId == itemUpdatedEventArgs.Model.ModelId);

      foreach (var songInAlbum in songsInAlbum)
      {
        if (songInAlbum.AlbumViewModel != itemUpdatedEventArgs.Model)
          songInAlbum.AlbumViewModel = itemUpdatedEventArgs.Model;
        else
          songInAlbum.UpdateAlbumViewModel(itemUpdatedEventArgs.Model.Model);
      }
    }

    #endregion

    #endregion
  }
}