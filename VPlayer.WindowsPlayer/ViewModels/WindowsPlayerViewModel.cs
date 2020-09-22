using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Logger;
using Ninject;
using Prism.Events;
using VCore;
using VCore.Annotations;
using VCore.Helpers;
using VCore.ItemsCollections;
using VCore.ItemsCollections.VirtualList;
using VCore.ItemsCollections.VirtualList.VirtualLists;
using VCore.Modularity.Events;
using VCore.ViewModels.Navigation;
using Vlc.DotNet.Core;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.InfoDownloader;
using VPlayer.AudioStorage.InfoDownloader.Clients.GIfs;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Events;
using VPlayer.Core.Interfaces.ViewModels;
using VPlayer.Core.Modularity.Regions;
using VPlayer.Core.ViewModels;
using VPlayer.Player.Views.WindowsPlayer;

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

namespace VPlayer.WindowsPlayer.ViewModels
{
  public class WindowsPlayerViewModel : PlayableRegionViewModel<WindowsPlayerView>, INavigationItem
  {
    #region Fields

    private readonly IVPlayerRegionProvider vPlayerRegionProvider;
    private readonly IEventAggregator eventAggregator;
    private readonly IStorageManager storageManager;
    private readonly AudioInfoDownloader audioInfoDownloader;
    private readonly ILogger logger;
    private readonly IAlbumsViewModel albumsViewModel;
    private int actualSongIndex = 0;
    private Dictionary<SongInPlayList, bool> playBookInCycle = new Dictionary<SongInPlayList, bool>();
    private HashSet<SongInPlayList> shuffleList = new HashSet<SongInPlayList>();

    #endregion Fields

    #region Constructors

    public WindowsPlayerViewModel(
      IVPlayerRegionProvider regionProvider,
      IEventAggregator eventAggregator,
      IKernel kernel,
      IStorageManager storageManager,
      AudioInfoDownloader audioInfoDownloader,
      ILogger logger,
      IAlbumsViewModel albumsViewModel) : base(regionProvider)
    {
      this.vPlayerRegionProvider = regionProvider ?? throw new ArgumentNullException(nameof(regionProvider));
      this.eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
      this.storageManager = storageManager ?? throw new ArgumentNullException(nameof(storageManager));
      this.audioInfoDownloader = audioInfoDownloader ?? throw new ArgumentNullException(nameof(audioInfoDownloader));
      this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
      this.albumsViewModel = albumsViewModel ?? throw new ArgumentNullException(nameof(albumsViewModel));

      Kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));


    }

    #endregion Constructors

    #region Properties

    #region ActualSong

    private SongInPlayList actualSong;
    public SongInPlayList ActualSong
    {
      get { return actualSong; }
      private set
      {
        if (value != actualSong)
        {
          actualSong = value;
          actualSongSubject.OnNext(PlayList.IndexOf(actualSong));
          RaisePropertyChanged();

          if (actualSong != null && string.IsNullOrEmpty(actualSong.ImagePath))
          {
            UseGif = true;
          }
        }
      }
    }

    #endregion

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


    public override bool CanPlay
    {
      get { return PlayList.Count != 0; }
    }

    public VlcMediaPlayer MediaPlayer { get; private set; }
    public RxObservableCollection<SongInPlayList> PlayList { get; } = new RxObservableCollection<SongInPlayList>();

    #region VirtualizedPlayList

    private VirtualList<SongInPlayList> virtualizedPlayList;
    public VirtualList<SongInPlayList> VirtualizedPlayList
    {
      get { return virtualizedPlayList; }
      private set
      {
        if (value != virtualizedPlayList)
        {
          virtualizedPlayList = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    public override bool ContainsNestedRegions => false;
    public override string RegionName { get; protected set; } = RegionNames.WindowsPlayerContentRegion;
    public string Header => "Player";

    public TimeSpan TotalPlaylistDuration
    {
      get { return TimeSpan.FromSeconds(PlayList.Sum(x => x.Duration)); }
    }

    #region IsRepeate

    private bool isRepeate = true;
    public bool IsRepeate
    {
      get { return isRepeate; }
      set
      {
        if (value != isRepeate)
        {
          isRepeate = value;

          if (ActualSavedPlaylist.IsReapting != value)
          {
            ActualSavedPlaylist.IsReapting = value;
            UpdateActualSavedPlaylistPlaylist();
          }

          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region IsShuffle

    private bool isShuffle;
    public bool IsShuffle
    {
      get { return isShuffle; }
      set
      {
        if (value != isShuffle)
        {
          isShuffle = value;

          if (ActualSavedPlaylist.IsShuffle != value)
          {
            ActualSavedPlaylist.IsShuffle = value;
            UpdateActualSavedPlaylistPlaylist();
          }

          RaisePropertyChanged();
        }
      }
    }

    #endregion

    public IKernel Kernel { get; set; }
    public int Cycle { get; set; }
    public Playlist ActualSavedPlaylist { get; set; } = new Playlist() { Id = -1 };
    public bool IsPlayFnished { get; private set; }

    #region ActualSearch

    private ReplaySubject<string> actualSearchSubject;
    private string actualSearch;
    public string ActualSearch
    {
      get { return actualSearch; }
      set
      {
        if (value != actualSearch)
        {
          actualSearch = value;

          actualSearchSubject.OnNext(actualSearch);

          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region ActualSongChanged

    private ReplaySubject<int> actualSongSubject = new ReplaySubject<int>(1);

    public IObservable<int> ActualSongChanged
    {
      get { return actualSongSubject.AsObservable(); }
    }

    #endregion

    #endregion Properties

    #region Commands

    #region NextSong

    private ActionCommand<SongInPlayList> nextSong;

    public ICommand NextSong
    {
      get
      {
        if (nextSong == null)
        {
          nextSong = new ActionCommand<SongInPlayList>(OnNextSong);
        }

        return nextSong;
      }
    }

    public void OnNextSong(
      SongInPlayList songInPlayList)
    {
      PlayNextWithSong(songInPlayList);
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
      vPlayerRegionProvider.ShowAlbumDetail(ActualSong.AlbumViewModel, RegionNames.WindowsPlayerContentRegion);
    }

    #endregion 

    #region SavePlaylist

    private ActionCommand savePlaylistCommand;

    public ICommand SavePlaylistCommandCommand
    {
      get
      {
        if (savePlaylistCommand == null)
        {
          savePlaylistCommand = new ActionCommand(OnSavePlaylist);
        }

        return savePlaylistCommand;
      }
    }

    public void OnSavePlaylist()
    {
      if (!SavePlaylist(true))
      {
        ActualSavedPlaylist.IsUserCreated = true;
        UpdateActualSavedPlaylistPlaylist();
        RaisePropertyChanged(nameof(ActualSavedPlaylist));
      }
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
      RandomGifUrl = (await giphyClient.GetRandomGif(GifTag)).Url.Replace("&","&amp;");
    }

    #endregion

    #endregion Commands

    #region Methods

    #region Initialize

    public override void Initialize()
    {
      Task.Run(async () =>
      {
        base.Initialize();

        OnNextGif();

        actualSearchSubject = new ReplaySubject<string>(1);
        actualSearchSubject.DisposeWith(this);


        storageManager.SubscribeToItemChange<Song>(OnSongChange).DisposeWith(this);
        storageManager.SubscribeToItemChange<Album>(OnAlbumChange).DisposeWith(this);

        PlayList.ItemRemoved.Subscribe(ItemsRemoved).DisposeWith(this);
        PlayList.ItemAdded.Subscribe(ItemsAdded).DisposeWith(this);
        PlayList.DisposeWith(this);

        actualSearchSubject.Throttle(TimeSpan.FromMilliseconds(150)).Subscribe(FilterByActualSearch).DisposeWith(this);

        //PlayList.CollectionChanged += PlayList_CollectionChanged;

        var currentAssembly = Assembly.GetEntryAssembly();

        if (currentAssembly != null)
        {
          var currentDirectory = new FileInfo(currentAssembly.Location).DirectoryName;
          var path = new DirectoryInfo(Path.Combine(currentDirectory, "libvlc", IntPtr.Size == 4 ? "win-x86" : "win-x64"));

          var libDirectory = new DirectoryInfo(path.FullName);

          MediaPlayer = new VlcMediaPlayer(libDirectory);
          MediaPlayer.DisposeWith(this);
        }

        if (MediaPlayer == null)
        {
          logger.Log(Logger.MessageType.Error, "VLC was not initlized!");
          return;
        }

        MediaPlayer.EncounteredError += (sender, e) =>
        {
          Console.Error.Write("An error occurred");
          IsPlayFnished = true;
        };

        MediaPlayer.EndReached += (sender, e) => { Task.Run(() => PlayNextWithSong()); };

        MediaPlayer.TimeChanged += (sender, e) =>
        {
          if (ActualSong != null)
          {
            ActualSong.ActualPosition = ((VlcMediaPlayer)sender).Position;
            ActualSavedPlaylist.LastSongElapsedTime = ((VlcMediaPlayer)sender).Position;
          }
        };

        MediaPlayer.Paused += (sender, e) =>
        {
          if (ActualSong != null)
          {
            ActualSong.IsPaused = true;
          }

          IsPlaying = false;
          IsPlayingSubject.OnNext(IsPlaying);
        };

        MediaPlayer.Stopped += (sender, e) =>
        {
          if (IsPlayFnished)
          {
            ActualSong.IsPlaying = false;
            ActualSong.IsPaused = false;
            ActualSong = null;
            actualSongIndex = -1;
            IsPlaying = false;
            IsPlayingSubject.OnNext(IsPlaying);
          }
        };

        MediaPlayer.Playing += (sender, e) =>
        {
          ActualSong.IsPlaying = true;
          ActualSong.IsPaused = false;
          IsPlaying = true;
          IsPlayingSubject.OnNext(IsPlaying);
        };

        eventAggregator.GetEvent<PlaySongsEvent>().Subscribe(PlaySongs).DisposeWith(this);
        eventAggregator.GetEvent<PauseEvent>().Subscribe(Pause).DisposeWith(this);
        eventAggregator.GetEvent<PlaySongsFromPlayListEvent>().Subscribe(PlaySongFromPlayList).DisposeWith(this);
        eventAggregator.GetEvent<RemoveFromPlaylistEvent>().Subscribe(DeleteSongs).DisposeWith(this);
      });
    }

    #endregion Initialize

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

        if (ActualSong != null && ActualSong.Model.Id == song.Id)
        {
          ActualSong.Update(song);
        }
      }
    }

    #endregion

    #region DeleteSongs

    private void DeleteSongs(RemoveFromPlaylistEventArgs obj)
    {
      switch (obj.DeleteType)
      {
        case DeleteType.SingleFromPlaylist:
          foreach (var songToDelete in obj.SongsToDelete)
          {
            var songInPlaylist = PlayList.SingleOrDefault(x => x == songToDelete);

            if (songInPlaylist != null)
            {
              PlayList.Remove(songInPlaylist);
            }
          }

          SavePlaylist(editSaved: true);

          break;
        case DeleteType.AlbumFromPlaylist:

          var albumId = obj.SongsToDelete.FirstOrDefault(x => x.AlbumViewModel != null)?.AlbumViewModel.ModelId;

          if (albumId != null)
          {
            var albumSongs = PlayList.Where(x => x.Model.Album.Id == albumId).ToList();

            foreach (var albumSong in albumSongs)
            {
              PlayList.Remove(albumSong);
            }

            SavePlaylist(editSaved: true);
          }
          break;
        default:
          throw new ArgumentOutOfRangeException();
      }

      ReloadVirtulizedPlaylist();
    }

    #endregion

    #region ItemsRemoved

    private void ItemsRemoved(EventPattern<SongInPlayList> eventPattern)
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

      if (ActualSong == eventPattern.EventArgs)
      {
        PlayNext(actualSongIndex);
      }

    }

    #endregion ItemsRemoved

    #region ItemsAdded

    private void ItemsAdded(EventPattern<SongInPlayList> eventPattern)
    {

    }

    #endregion

    #region PlaySongFromPlayList

    private void PlaySongFromPlayList(SongInPlayList songInPlayList)
    {
      if (songInPlayList == ActualSong)
      {
        if (!ActualSong.IsPaused)
          Pause();
        else
          Play();
      }
      else
      {
        PlayNextWithSong(songInPlayList);
      }
    }

    #endregion PlaySongFromPlayList

    #region PlayPlaylist

    private void PlayPlaylist(PlaySongsEventData data, int? lastSongIndex = null)
    {
      ActualSavedPlaylist = data.GetModel<Playlist>();

      ActualSavedPlaylist.LastPlayed = DateTime.Now;

      if (lastSongIndex == null)
      {
        PlaySongs(data.Songs, false);
      }
      else
      {
        PlaySongs(data.Songs, false, lastSongIndex.Value);

        if (data.SetPostion.HasValue)
          MediaPlayer.Position = data.SetPostion.Value;
      }


      UpdateActualSavedPlaylistPlaylist();

    }

    #endregion

    #region PlaySongs

    private void PlaySongs(PlaySongsEventData data)
    {
      if (ActualSavedPlaylist.Id > 0)
        UpdateActualSavedPlaylistPlaylist();

      switch (data.PlaySongsAction)
      {
        case PlaySongsAction.Play:
          PlaySongs(data.Songs);

          break;
        case PlaySongsAction.Add:
          PlayList.AddRange(data.Songs);

          if (ActualSong == null)
          {
            SetActualSong(0);
          }

          ReloadVirtulizedPlaylist();
          RaisePropertyChanged(nameof(CanPlay));
          SavePlaylist(editSaved: true);
          break;
        case PlaySongsAction.PlayFromPlaylist:
          PlayPlaylist(data);

          break;
        case PlaySongsAction.PlayFromPlaylistLast:
          PlayPlaylist(data, data.GetModel<Playlist>().LastSongIndex);
          break;
        default:
          throw new ArgumentOutOfRangeException();
      }

      RaisePropertyChanged(nameof(CanPlay));

      if (data.IsShufle.HasValue)
        IsShuffle = data.IsShufle.Value;

      if (data.IsRepeat.HasValue)
        IsRepeate = data.IsRepeat.Value;

      Task.Run(async () =>
      {
        foreach (var song in PlayList)
        {
          song.LoadLRCFromEnitityLyrics();
        }
      });

    }

    private void PlaySongs(IEnumerable<SongInPlayList> songs, bool savePlaylist = true, int songIndex = 0, bool editSaved = false)
    {
      IsActive = true;

      PlayList.Clear();
      PlayList.AddRange(songs);
      ReloadVirtulizedPlaylist();

      IsPlaying = true;

      PlayNext(songIndex);

      if (ActualSong != null)
        ActualSong.IsPlaying = false;

      if (savePlaylist)
      {
        SavePlaylist(editSaved: editSaved);
      }
    }

    #endregion PlaySongs

    #region ReloadVirtulizedPlaylist

    private void ReloadVirtulizedPlaylist()
    {
      var generator = new ItemsGenerator<SongInPlayList>(PlayList, 15);
      VirtualizedPlayList = new VirtualList<SongInPlayList>(generator);
    }

    #endregion

    #region Play

    public override void Play()
    {
      Task.Run(() =>
      {
        if (IsPlayFnished)
        {
          PlayNext(0, true);
        }
        else
        {
          if (ActualSong != null)
          {
            if (IsPlaying)
            {
              var media = MediaPlayer.GetMedia();
              if (media == null || media.NowPlaying != ActualSong.Model.DiskLocation)
              {
                //file:///D:/Hudba/2Pac Discography [2007]/--- Other albums ---/1998 - Greatest Hits/2Pac - 102 - 2 Of Amerikaz Most Wanted.mp3
                var location = new Uri(ActualSong.Model.DiskLocation);

                MediaPlayer.SetMedia(location);

                Task.Run(async () =>
                {
                  if (string.IsNullOrEmpty(ActualSong.Lyrics) && !string.IsNullOrEmpty(ActualSong.ArtistViewModel?.Name))
                  {
                    await audioInfoDownloader.UpdateSongLyricsAsync(ActualSong.ArtistViewModel.Name, ActualSong.Name, ActualSong.Model);
                  }

                  if (string.IsNullOrEmpty(ActualSong.LRCLyrics))
                  {
                    await ActualSong.TryToRefreshUpdateLyrics();
                  }

                });
              }
            }

            MediaPlayer.Play();
            CheckCycle();

          }
        }
      });
    }

    #endregion Play

    #region Pause

    public override void Pause()
    {
      Task.Run(() => { MediaPlayer.Pause(); });
    }

    #endregion Pause

    #region PlayPrevious

    public override void PlayPrevious()
    {
      actualSongIndex--;

      if (actualSongIndex < 0)
      {
        actualSongIndex = 0;
      }

      PlayNext(actualSongIndex);
    }

    #endregion

    #region Stop

    public override void Stop()
    {
      MediaPlayer.Stop();
    }

    #endregion

    #region PlayNext

    public override void PlayNext(int? songIndex = null, bool forcePlay = false)
    {
      Application.Current?.Dispatcher?.Invoke(() =>
      {
        if (!string.IsNullOrEmpty(actualSearch))
        {
          ActualSearch = null;
        }

        IsPlayFnished = false;

        if (IsShuffle && songIndex == null)
        {
          var random = new Random();
          var result = PlayList.Where(p => shuffleList.All(p2 => p2 != p)).ToList();

          actualSongIndex = random.Next(0, result.Count);
        }
        else if (songIndex == null)
        {
          actualSongIndex++;
        }
        else
        {
          actualSongIndex = songIndex.Value;
        }

        if (actualSongIndex == PlayList.Count)
        {
          if (IsRepeate)
            actualSongIndex = 0;
          else
          {
            IsPlayFnished = true;
            Stop();
            return;
          }
        }

        SetActualSong(actualSongIndex);

        if (IsPlaying || forcePlay)
          Play();
        else if (!IsPlaying && songIndex != null)
          Play();
        else if (ActualSong != null)
          ActualSong.IsPaused = true;

        UpdateActualSavedPlaylistPlaylist();
      });
    }

    public void PlayNextWithSong(SongInPlayList nextSong = null)
    {
      if (nextSong == null)
      {
        PlayNext();
      }
      else
      {
        var item = PlayList.SingleOrDefault(x => x.Model.Id == nextSong.Model.Id);

        if (ActualSong != item && item != null)
        {
          PlayNext(PlayList.IndexOf(item));
        }
      }
    }

    #endregion PlayNext

    #region SetActualSong

    private void SetActualSong(int index)
    {
      try
      {
        if (actualSongIndex < PlayList.Count && actualSongIndex >= 0)
        {
          if (ActualSong != null)
          {
            ActualSong.IsPlaying = false;
            ActualSong.IsPaused = false;
            ActualSong.AlbumViewModel.IsPlaying = false;
            ActualSong.ArtistViewModel.IsPlaying = false;
          }

          ActualSong = PlayList[index];
          ActualSong.IsPlaying = true;

          ActualSong.AlbumViewModel.IsPlaying = true;
          ActualSong.ArtistViewModel.IsPlaying = true;

          if (ActualSavedPlaylist != null)
          {
            ActualSavedPlaylist.LastSongIndex = PlayList.IndexOf(ActualSong);
            ActualSavedPlaylist.LastSongElapsedTime = 0;
            UpdateActualSavedPlaylistPlaylist();
          }

          shuffleList.Add(ActualSong);
        }
        else
        {
          if (ActualSong != null)
          {
            ActualSong.IsPlaying = false;
            ActualSong.IsPaused = false;
            ActualSong.AlbumViewModel.IsPlaying = false;
            ActualSong.ArtistViewModel.IsPlaying = false;
          }

          ActualSong = null;
        }
      }
      catch (Exception ex)
      {
        logger.Log(ex);
      }
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

    #region PlayList_CollectionChanged

    private void PlayList_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {

      RaisePropertyChanged(nameof(TotalPlaylistDuration));

      switch (e.Action)
      {
        case NotifyCollectionChangedAction.Add:

          foreach (var item in e.NewItems)
          {
            playBookInCycle.Add((SongInPlayList)item, false);
          }

          break;
        case NotifyCollectionChangedAction.Remove:
          foreach (var item in e.OldItems)
          {
            playBookInCycle.Remove((SongInPlayList)item);
          }
          break;
        case NotifyCollectionChangedAction.Replace:

          foreach (var item in e.OldItems)
          {
            playBookInCycle.Remove((SongInPlayList)item);
          }

          foreach (var item in e.NewItems)
          {
            playBookInCycle.Add((SongInPlayList)item, false);
          }

          break;
        case NotifyCollectionChangedAction.Reset:
          playBookInCycle.Clear();
          break;
        default:
          throw new ArgumentOutOfRangeException();
      }


    }

    #endregion

    #region SavePlaylist

    public bool SavePlaylist(bool isUserCreated = false, bool editSaved = false)
    {
      var songs = new List<PlaylistSong>();

      for (int i = 0; i < PlayList.Count; i++)
      {
        var song = PlayList[i];

        songs.Add(new PlaylistSong()
        {
          IdSong = song.Model.Id,
          OrderInPlaylist = (i + 1)
        });
      }

      var artists = PlayList.GroupBy(x => x.ArtistViewModel.Name);

      var playlistName = string.Join(", ", artists.Select(x => x.Key).ToArray());

      var songIds = PlayList.Select(x => x.Model.Id);

      var hashCode = songIds.GetSequenceHashCode();

      var entityPlayList = new Playlist()
      {
        IsReapting = IsRepeate,
        IsShuffle = IsShuffle,
        Name = playlistName,
        SongsInPlaylitsHashCode = hashCode,
        SongCount = songs.Count,
        PlaylistSongs = songs,
        LastSongElapsedTime = ActualSavedPlaylist.LastSongElapsedTime,
        LastSongIndex = ActualSavedPlaylist.LastSongIndex,
        IsUserCreated = isUserCreated,
        LastPlayed = DateTime.Now
      };

      bool success = false;

      if (editSaved && ActualSavedPlaylist.IsUserCreated && hashCode != ActualSavedPlaylist.SongsInPlaylitsHashCode)
      {
        ActualSavedPlaylist.SongsInPlaylitsHashCode = hashCode;
        ActualSavedPlaylist.PlaylistSongs = songs;
        ActualSavedPlaylist.SongCount = songs.Count;

        UpdateActualSavedPlaylistPlaylist();
      }
      else
      {
        success = storageManager.StoreData(entityPlayList, out var entityPlaylist);

        entityPlayList.Update(entityPlaylist);

        ActualSavedPlaylist = entityPlayList;

        if (!success)
        {
          ActualSavedPlaylist.LastPlayed = DateTime.Now;
          UpdateActualSavedPlaylistPlaylist();
        }
      }

      return success;
    }

    #endregion

    #region UpdateActualSavedPlaylistPlaylist

    private void UpdateActualSavedPlaylistPlaylist()
    {
      storageManager.UpdateData(ActualSavedPlaylist);
    }

    #endregion

    #region FilterByActualSearch

    private void FilterByActualSearch(string predictate)
    {
      if (!string.IsNullOrEmpty(predictate))
      {
        var items = PlayList.Where(x =>
          IsInFind(x.Name, predictate) ||
          IsInFind(x.AlbumViewModel.Name, predictate) ||
          IsInFind(x.ArtistViewModel.Name, predictate));

        var generator = new ItemsGenerator<SongInPlayList>(items, 15);

        VirtualizedPlayList = new VirtualList<SongInPlayList>(generator);

      }
      else
      {
        ReloadVirtulizedPlaylist();
      }
    }

    #endregion

    #region IsInFind

    private bool IsInFind(string original, string phrase)
    {
      return original.ToLower().Contains(phrase) || original.Similarity(phrase) > 0.8;
    }

    #endregion

    #region Dispose

    public override void Dispose()
    {
      if (ActualSavedPlaylist != null)
        UpdateActualSavedPlaylistPlaylist();


      base.Dispose();
    }

    #endregion

    #endregion
  }
}