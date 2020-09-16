using Listener;
using Ninject;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using VCore;
using VCore.Helpers;
using VCore.ItemsCollections;
using VCore.ItemsCollections.VirtualList;
using VCore.ItemsCollections.VirtualList.VirtualLists;
using VCore.Modularity.Events;
using VCore.Modularity.Interfaces;
using VCore.Modularity.Navigation;
using VCore.Modularity.RegionProviders;
using VCore.ViewModels;
using VCore.ViewModels.Navigation;
using Vlc.DotNet.Core;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.InfoDownloader;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Events;
using VPlayer.Core.Modularity.Regions;
using VPlayer.Core.ViewModels;
using VPlayer.Core.ViewModels.Artists;
using VPlayer.Player.Views;
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

namespace VPlayer.Player.ViewModels
{
  public class WindowsPlayerViewModel : PlayableRegionViewModel<WindowsPlayerView>, INavigationItem
  {
    #region Fields

    private readonly IVPlayerRegionProvider regionProvider;
    private readonly IEventAggregator eventAggregator;
    private readonly IStorageManager storageManager;
    private readonly AudioInfoDownloader audioInfoDownloader;
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
      AudioInfoDownloader audioInfoDownloader) : base(regionProvider)
    {
      this.regionProvider = regionProvider ?? throw new ArgumentNullException(nameof(regionProvider));
      this.eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
      this.storageManager = storageManager ?? throw new ArgumentNullException(nameof(storageManager));
      this.audioInfoDownloader = audioInfoDownloader ?? throw new ArgumentNullException(nameof(audioInfoDownloader));
      Kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
      this.storageManager.ItemChanged.Where(x => x.Item.GetType() == typeof(Song)).Subscribe(SongChange);
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
        }
      }
    }
    #endregion

    public override bool IsPlaying { get; protected set; }

    public override bool CanPlay
    {
      get { return PlayList.Count != 0; }
    }

    public VlcMediaPlayer MediaPlayer { get; private set; }
    public RxObservableCollection<SongInPlayList> PlayList { get; set; } = new RxObservableCollection<SongInPlayList>();
    public VirtualList<SongInPlayList> VirtualizedPlayList { get; set; } 

    public override bool ContainsNestedRegions => false;
    public override string RegionName { get; protected set; } = RegionNames.WindowsPlayerContentRegion;
    public string Header => "Player";

    public TimeSpan TotalPlaylistDuration
    {
      get { return TimeSpan.FromSeconds(PlayList.Sum(x => x.Duration)); }
    }

    #region IsRepeate

    private bool isRepeate;
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
            UpdatePlaylist();
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
            UpdatePlaylist();
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
      regionProvider.ShowAlbumDetail(ActualSong.AlbumViewModel, RegionNames.WindowsPlayerContentRegion);
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
        UpdatePlaylist();
        RaisePropertyChanged(nameof(ActualSavedPlaylist));
      }
    }

    #endregion 

    #endregion Commands

    #region Methods

    #region Initialize

    public override void Initialize()
    {
      Task.Run(() =>
      {
        base.Initialize();

        PlayList.ItemRemoved.Subscribe(ItemsRemoved);
        PlayList.ItemAdded.Subscribe(ItemsAdded);

        //PlayList.CollectionChanged += PlayList_CollectionChanged;



        var currentAssembly = Assembly.GetEntryAssembly();
        var currentDirectory = new FileInfo(currentAssembly.Location).DirectoryName;
        var path = new DirectoryInfo(Path.Combine(currentDirectory, "libvlc", IntPtr.Size == 4 ? "win-x86" : "win-x64"));

        var libDirectory = new DirectoryInfo(path.FullName);
        MediaPlayer = new VlcMediaPlayer(libDirectory);



        MediaPlayer.EncounteredError += (sender, e) =>
        {
          Console.Error.Write("An error occurred");
          IsPlayFnished = true;
        };

        MediaPlayer.EndReached += (sender, e) => { Task.Run(() => PlayNextWithSong()); };

        MediaPlayer.TimeChanged += (sender, e) =>
        {
          ActualSong.ActualPosition = ((VlcMediaPlayer)sender).Position;
          ActualSavedPlaylist.LastSongElapsedTime = ((VlcMediaPlayer)sender).Position;
        };

        MediaPlayer.Paused += (sender, e) =>
        {
          ActualSong.IsPaused = true;
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

        eventAggregator.GetEvent<PlaySongsEvent>().Subscribe(PlaySongs);
        eventAggregator.GetEvent<PauseEvent>().Subscribe(Pause);
        eventAggregator.GetEvent<PlaySongsFromPlayListEvent>().Subscribe(PlaySongFromPlayList);
        eventAggregator.GetEvent<DeleteSongEvent>().Subscribe(DeleteSongs);
      });
    }

    #endregion Initialize

    private void SongChange(ItemChanged itemChanged)
    {
      if (itemChanged.Item is Song song)
      {
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
    }

    #region DeleteSongs

    private void DeleteSongs(DeleteEventArgs obj)
    {
      switch (obj.DeleteType)
      {
        case DeleteType.Database:
          throw new NotImplementedException();
          break;
        case DeleteType.SingleFromPlaylist:
          foreach (var songToDelete in obj.SongsToDelete)
          {
            var songInPlaylist = PlayList.SingleOrDefault(x => x.Model.Id == songToDelete.Id);

            if (songInPlaylist != null)
            {
              PlayList.Remove(songInPlaylist);
            }
          }

          SavePlaylist();

          break;
        case DeleteType.AlbumFromPlaylist:
          foreach (var songToDelete in obj.SongsToDelete)
          {
            var albumSongs = PlayList.Where(x => x.Model.Album.Id == songToDelete.Album.Id).ToList();

            foreach (var albumSong in albumSongs)
            {
              PlayList.Remove(albumSong);
            }
          }

          SavePlaylist();
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
      eventPattern.EventArgs.ArtistViewModel.IsInPlaylist = false;
      eventPattern.EventArgs.AlbumViewModel.IsInPlaylist = false;
    }


    private void ItemsAdded(EventPattern<SongInPlayList> eventPattern)
    {

    }

    #endregion ItemsRemoved

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


      UpdatePlaylist();

    }

    #endregion

    #region PlaySongs

    private void PlaySongs(PlaySongsEventData data)
    {
      IsActive = true;

      if (ActualSavedPlaylist.Id > 0)
        UpdatePlaylist();

      switch (data.PlaySongsAction)
      {
        case PlaySongsAction.Play:
          PlaySongs(data.Songs);
          SavePlaylist();

        
          break;
        case PlaySongsAction.Add:
          PlayList.AddRange(data.Songs);

          if (ActualSong == null)
          {
            SetActualSong(0);
          }

          VirtualizedPlayList = new VirtualList<SongInPlayList>(new ItemsGenerator<SongInPlayList>(PlayList));
          RaisePropertyChanged(nameof(CanPlay));
          SavePlaylist();
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
      IsShuffle = data.IsShufle;
      IsRepeate = data.IsRepeat;

      Task.Run(async () =>
      {
        foreach (var song in PlayList)
        {
          song.LoadLRCFromEnitityLyrics();
        }
      });

    }

    private void PlaySongs(IEnumerable<SongInPlayList> songs, bool savePlaylist = true, int songIndex = 0)
    {
      PlayList.Clear();
      PlayList.AddRange(songs);
      ReloadVirtulizedPlaylist();



      IsPlaying = true;

      PlayNext(songIndex);

      if (ActualSong != null)
        ActualSong.IsPlaying = false;

      if (savePlaylist)
      {
        SavePlaylist();
      }
    }

    #endregion PlaySongs

    private void ReloadVirtulizedPlaylist()
    {
      var generator = new ItemsGenerator<SongInPlayList>(PlayList);
      generator._repository.PageSize = 15;
      VirtualizedPlayList = new VirtualList<SongInPlayList>(generator);
    }

    #region Play

    public override void Play()
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

                  await ActualSong.TryToUpdateLyrics();
                }

              });
            }
          }

          MediaPlayer.Play();
          CheckCycle();

        }
      }
    }

    #endregion Play

    #region Pause

    public override void Pause()
    {
      MediaPlayer.Pause();
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
          }
        }
        else if (PlayList.Count > actualSongIndex)
        {
          if (ActualSong != null)
          {
            ActualSong.IsPlaying = false;
            ActualSong.IsPaused = false;
          }


          SetActualSong(actualSongIndex);

          if (IsPlaying || forcePlay)
            Play();
          else if (!IsPlaying && songIndex != null)
            Play();
          else
            ActualSong.IsPaused = true;
        }
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
        ActualSong = PlayList[index];
        ActualSong.IsPlaying = true;

        if (ActualSavedPlaylist != null)
        {
          ActualSavedPlaylist.LastSongIndex = PlayList.IndexOf(ActualSong);
          UpdatePlaylist();
        }

        shuffleList.Add(ActualSong);
      }
      catch (Exception ex)
      {

        throw;
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

    public bool SavePlaylist(bool isUserCreated = false)
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

      var playlistName = string.Join(", ", artists.Select(x => x.Key).ToArray()) + " " + DateTime.Now.ToShortDateString();

      var songIds = PlayList.Select(x => x.Model.Id);

      var hash = songIds.GetSequenceHashCode();


      var entityPlayList = new Playlist()
      {
        IsReapting = IsRepeate,
        IsShuffle = IsShuffle,
        Name = playlistName,
        SongsInPlaylitsHashCode = hash,
        SongCount = songs.Count,
        PlaylistSongs = songs,
        LastSongElapsedTime = ActualSavedPlaylist.LastSongElapsedTime,
        LastSongIndex = ActualSavedPlaylist.LastSongIndex,
        IsUserCreated = isUserCreated,
        LastPlayed = DateTime.Now
      };

      var success = storageManager.StoreData(entityPlayList, out var entityPlaylist);

      entityPlayList.Update(entityPlaylist);

      ActualSavedPlaylist = entityPlayList;

      return success;
    }

    #endregion

    #region UpdatePlaylist

    private void UpdatePlaylist()
    {
      storageManager.UpdateData(ActualSavedPlaylist);
    }

    #endregion

    #region Dispose


    public override void Dispose()
    {
      if (ActualSavedPlaylist != null)
        UpdatePlaylist();

      base.Dispose();
    }

    #endregion

    #endregion
  }
}